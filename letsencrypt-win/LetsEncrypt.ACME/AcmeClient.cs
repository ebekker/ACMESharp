using Jose;
using LetsEncrypt.ACME.HTTP;
using LetsEncrypt.ACME.JOSE;
using LetsEncrypt.ACME.JSON;
using LetsEncrypt.ACME.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace LetsEncrypt.ACME
{
    /// <summary>
    /// The ACME client encapsulates all the protocol rules to interact
    /// with an ACME client as specified by the ACME specficication.
    /// </summary>
    public class AcmeClient : IDisposable
    {
        #region -- Constants --

        /// <summary>
        /// The relation name for the "Terms of Service" related link header.
        /// </summary>
        /// <remarks>
        /// Link headers can be returned as part of a registration:
        ///   HTTP/1.1 201 Created
        ///   Content-Type: application/json
        ///   Location: https://example.com/acme/reg/asdf
        ///   Link: <https://example.com/acme/new-authz>;rel="next"
        ///   Link: <https://example.com/acme/recover-reg>;rel="recover"
        ///   Link: <https://example.com/acme/terms>;rel="terms-of-service"
        ///
        /// The "terms-of-service" URI should be included in the "agreement" field
        /// in a subsequent registration update
        /// </remarks>
        public const string TOS_LINK_REL = "terms-of-service";

        #endregion -- Constants --

        #region -- Fields --

        WebClient _Web;
        JsonSerializerSettings _jsonSettings = new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
            ContractResolver = new AcmeJsonContractResolver(),
        };

        #endregion -- Fields --

        #region -- Constructors --

        public AcmeClient(Uri rootUrl = null, AcmeServerDirectory dir = null,
                ISigner signer = null, AcmeRegistration reg = null)
        {
            RootUrl = rootUrl;
            Directory = dir;
            Signer = signer;
            Registration = reg;
        }

        #endregion -- Constructors --

        #region -- Properties --

        public Uri RootUrl
        { get; set; }

        public AcmeServerDirectory Directory
        { get; set; }

        public ISigner Signer
        { get; set; }

        public AcmeRegistration Registration
        { get; set; }

        public bool Initialized
        { get; private set; }

        private WebClient Web
        {
            get
            {
                if (_Web == null)
                {
                    _Web = new WebClient();
                    _Web.BaseAddress = RootUrl.ToString();
                    _Web.Encoding = Encoding.UTF8;
                    _Web.Headers["content-type"] = "application/json";
                }
                return _Web;
            }
        }

        public string NextNonce
        { get; private set; }

        #endregion -- Properties --

        #region -- Methods --
        
        public void Init()
        {
            if (RootUrl == null)
                throw new InvalidOperationException("Missing ACME server root URL (RootUrl)");

            if (Signer == null)
                throw new InvalidOperationException("Missing request message signer (Signer)");

            if (Directory == null)
                Directory = new AcmeServerDirectory();

            var requ = WebRequest.Create(new Uri(RootUrl, "/"));

            // TODO:  according to ACME 5.5 we *should* be able to issue a HEAD
            // request to get an initial replay-nonce, but this is not working,
            // so we do a GET against the root URL to get that initial nonce
            //requ.Method = "HEAD";
            requ.Method = "GET";

            var resp = requ.GetResponse();
            ExtractNonce(resp);

            Initialized = true;
        }

        public void Dispose()
        {
            if (Web != null)
                Web.Dispose();

            Initialized = false;
        }

        protected void AssertInit()
        {
            if (!Initialized)
                throw new InvalidOperationException("Client is not initialized");
        }

        protected void AssertRegistration()
        {
            if (Registration == null)
                throw new InvalidOperationException("Client is missing registration info");
        }

        public AcmeServerDirectory GetDirectory(bool saveRelative = false)
        {
            AssertInit();

            var requ = WebRequest.Create(new Uri(RootUrl,
                    Directory[AcmeServerDirectory.RES_DIRECTORY]));
            requ.Method = "GET";

            using (var resp = (HttpWebResponse)requ.GetResponse())
            {
                using (var s = new StreamReader(resp.GetResponseStream()))
                {
                    var resMap = JObject.Parse(s.ReadToEnd());

                    foreach (var kv in resMap)
                    {
                        if (kv.Value.Type == JTokenType.String)
                        {
                            var urlValue = (kv.Value as JValue).Value as string;

                            if (saveRelative)
                                urlValue = new Uri(urlValue).PathAndQuery;

                            Directory[kv.Key] = urlValue;
                        }
                    }
                }
            }

            return Directory;
        }

        public AcmeRegistration Register(string[] contacts)
        {
            AssertInit();

            var requMsg = new NewRegRequest
            {
                Contact = contacts,
            };

            var resp = PostRequest(new Uri(RootUrl,
                    Directory[AcmeServerDirectory.RES_NEW_REG]), requMsg);

            // HTTP 409 (Conflict) response for a previously registered pub key
            //    Location:  still had the regUri
            if (resp.IsError)
            {
                if (resp.StatusCode == HttpStatusCode.Conflict)
                    throw new AcmeWebException(resp.Error as WebException,
                            "Conflict due to previously registered public key", resp);
                else if (resp.IsError)
                    throw new AcmeWebException(resp.Error as WebException,
                            "Unexpected error", resp);
            }

            var regUri = resp.Headers["Location"];
            if (string.IsNullOrEmpty(regUri))
                throw new AcmeException("server did not provide a registration URI in the response");


            var newReg = new AcmeRegistration
            {
                PublicKey = Signer.ExportJwk(),
                RegistrationUri = regUri,
                Contacts = respMsg.Contact,
                Links = resp.Links,
                /// Extracts the "Terms of Service" related link header if there is one and
                /// returns the URI associated with it.  Otherwise returns <c>null</c>.
                TosLinkUri = resp.Links[TOS_LINK_REL].FirstOrDefault(),
                AuthorizationsUri = respMsg.Authorizations,
                CertificatesUri = respMsg.Certificates,
                TosAgreementUri = respMsg.Agreement,
            };

            Registration = newReg;

            return Registration;
        }

        public AcmeRegistration UpdateRegistration(bool useRootUrl = false, bool agreeToTos = false, string[] contacts = null)
        {
            AssertInit();
            AssertRegistration();

            var requMsg = new UpdateRegRequest();

            if (contacts != null)
                requMsg.Contact = contacts;

            if (agreeToTos && !string.IsNullOrWhiteSpace(Registration.TosLinkUri))
                requMsg.Agreement = Registration.TosLinkUri;

            // Compute the URL to submit the request to, either exactly as
            // provided in the Registration object or relative to the Root URL
            var requUri = new Uri(Registration.RegistrationUri);
            if (useRootUrl)
                requUri = new Uri(RootUrl, requUri.PathAndQuery);

            var resp = PostRequest(requUri, requMsg);

            if (resp.IsError)
            {
                throw new AcmeWebException(resp.Error as WebException,
                        "Unexpected error", resp);
            }

            var respMsg = JsonConvert.DeserializeObject<RegResponse>(resp.Content);

            var updReg = new AcmeRegistration
            {
                PublicKey = Signer.ExportJwk(),
                RegistrationUri = Registration.RegistrationUri,
                Contacts = respMsg.Contact,
                Links = resp.Links,
                /// Extracts the "Terms of Service" related link header if there is one and
                /// returns the URI associated with it.  Otherwise returns <c>null</c>.
                TosLinkUri = resp.Links[TOS_LINK_REL].FirstOrDefault(),
                AuthorizationsUri = respMsg.Authorizations,
                CertificatesUri = respMsg.Certificates,
                TosAgreementUri = respMsg.Agreement,
            };

            Registration = updReg;

            return Registration;
        }

        public AuthorizationState AuthorizeIdentifier(string dnsIdentifier)
        {
            AssertInit();
            AssertRegistration();

            var requMsg = new NewAuthzRequest
            {
                Identifier = new IdentifierPart
                {
                    Type = "dns",
                    Value = dnsIdentifier
                }
            };

            var resp = PostRequest(new Uri(RootUrl,
                    Directory[AcmeServerDirectory.RES_NEW_AUTHZ]), requMsg);

            if (resp.IsError)
            {
                throw new AcmeWebException(resp.Error as WebException,
                        "Unexpected error", resp);
            }

            var respMsg = JsonConvert.DeserializeObject<NewAuthzResponse>(resp.Content);

            var authzState = new AuthorizationState
            {
                Identifier = respMsg.Identifier.Value,
                Status = respMsg.Status,
                Combinations = respMsg.Combinations,

                // Simple copy/conversion from one form to another
                Challenges = respMsg.Challenges.Select(x => new AuthorizeChallenge
                {
                    Type = x.Type,
                    Status = x.Status,
                    Uri = x.Uri,
                    Token = x.Token,
                    Tls = x.Tls,
                }),
            };

            return authzState;
        }

        public void RefreshAuthorizeChallenge(AuthorizationState authzState, string type, bool useRootUrl = false)
        {
            AssertInit();
            AssertRegistration();

            var c = authzState.Challenges.FirstOrDefault(x => x.Type == type);
            if (c == null)
                throw new ArgumentOutOfRangeException("no challenge found matching requested type");

            var requUri = new Uri(c.Uri);
            if (useRootUrl)
                requUri = new Uri(RootUrl, requUri.PathAndQuery);

            var requ = WebRequest.Create(requUri);
            using (var resp = requ.GetResponse())
            {
                using (var s = new StreamReader(resp.GetResponseStream()))
                {
                    var cp = JsonConvert.DeserializeObject<ChallengePart>(s.ReadToEnd());

                    c.Type = cp.Type;
                    c.Uri = cp.Uri;
                    c.Token = cp.Token;
                    c.Status = cp.Status;
                    c.Tls = cp.Tls;
                }
            }
        }

        public void GenerateAuthorizeChallengeAnswer(AuthorizationState authzState, string type)
        {
            AssertInit();
            AssertRegistration();

            var c = authzState.Challenges.FirstOrDefault(x => x.Type == type);
            if (c == null)
                throw new ArgumentOutOfRangeException("no challenge found matching requested type");

            switch (type)
            {
                case "dns":
                    c.ChallengeAnswer = c.GenerateDnsChallengeAnswer(authzState.Identifier, Signer);
                    c.ChallengeAnswerMessage = new AnswerDnsChallengeRequest
                    {
                        ClientPublicKey = Signer.ExportJwk(),
                        Validation = new
                        {
                            header = new { alg = Signer.JwsAlg },
                            payload = JwsHelper.Base64UrlEncode(JsonConvert.SerializeObject(new
                            {
                                type = "dns",
                                token = c.Token
                            })),
                            signature = c.ChallengeAnswer.Value
                        }
                    };
                    break;

                case "simpleHttp":
                    var tls = c.Tls.GetValueOrDefault(true);
                    c.ChallengeAnswer = c.GenerateHttpChallengeAnswer(authzState.Identifier, Signer, tls);
                    c.ChallengeAnswerMessage = new AnswerHttpChallengeRequest
                    {
                        Tls = tls
                    };
                    break;

                default:
                    throw new ArgumentException("unsupported challenge type", nameof(type));
            }
        }

        public void SubmitAuthorizeChallengeAnswer(AuthorizationState authzState, string type, bool useRootUrl = false)
        {
            AssertInit();
            AssertRegistration();

            var c = authzState.Challenges.FirstOrDefault(x => x.Type == type);
            if (c == null)
                throw new ArgumentOutOfRangeException("no challenge found matching requested type");

            if (c.ChallengeAnswer.Key == null || c.ChallengeAnswer.Value == null || c.ChallengeAnswerMessage == null)
                throw new InvalidOperationException("challenge answer has not been generated");

            var requUri = new Uri(c.Uri);
            if (useRootUrl)
                requUri = new Uri(RootUrl, requUri.PathAndQuery);

            var resp = PostRequest(requUri, c.ChallengeAnswerMessage);

            if (resp.IsError)
            {
                throw new AcmeWebException(resp.Error as WebException,
                        "Unexpected error", resp);
            }
        }

        /// <summary>
        /// Submits an ACME protocol request via an HTTP POST with the necessary semantics
        /// and protocol details.  The result is a simplified and canonicalized response
        /// object capturing the error state, HTTP response headers and content of the
        /// response body.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private AcmeHttpResponse PostRequest(Uri uri, object message)
        {
            var acmeSigned = ComputeAcmeSigned(message, Signer);
            var acmeBytes = Encoding.ASCII.GetBytes(acmeSigned);

            var requ = WebRequest.Create(uri);
            requ.Method = "POST";
            requ.ContentType = "application/json";
            requ.ContentLength = acmeBytes.Length;
            using (var s = requ.GetRequestStream())
            {
                s.Write(acmeBytes, 0, acmeBytes.Length);
            }
            
            try
            {
                using (var resp = (HttpWebResponse)requ.GetResponse())
                {
                    ExtractNonce(resp);
                    return new AcmeHttpResponse(resp);
                }
            }
            catch (WebException ex)
            {
                using (var resp = (HttpWebResponse)ex.Response)
                {
                    var acmeResp = new AcmeHttpResponse(resp)
                    {
                        IsError = true,
                        Error = ex,
                    };

                    if (ProblemDetailResponse.CONTENT_TYPE == resp.ContentType
                            && !string.IsNullOrEmpty(acmeResp.Content))
                    {
                        acmeResp.ProblemDetail = JsonConvert.DeserializeObject<ProblemDetailResponse>(
                                acmeResp.Content);
                    }

                    return acmeResp;
                }
            }
        }

        /// <summary>
        /// Computes the JWS-signed ACME request body for the given message object
        /// and signer instance.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="signer"></param>
        /// <returns></returns>
        private string ComputeAcmeSigned(object message, ISigner signer)
        {
            var protectedHeader = new
            {
                nonce = NextNonce
            };
            var unprotectedHeader = new
            {
                alg = Signer.JwsAlg,
                jwk = Signer.ExportJwk()
            };

            var payload = string.Empty;
            if (message is JObject)
                payload = ((JObject)message).ToString(Formatting.None);
            else
                payload = JsonConvert.SerializeObject(message, Formatting.None);

            var acmeSigned = JwsHelper.SignFlatJson(Signer.Sign, payload,
                    protectedHeader, unprotectedHeader);

            return acmeSigned;
        }

        /// <summary>
        /// Extracts the next ACME protocol nonce from the argument Web response
        /// and remembers it for the next protocol request.
        /// </summary>
        /// <param name="resp"></param>
        private void ExtractNonce(WebResponse resp)
        {
            var nonceHeader = resp.Headers.AllKeys.FirstOrDefault(x =>
                    x.Equals("Replay-nonce", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(nonceHeader))
                throw new AcmeException("Missing initial replay-nonce header");

            NextNonce = resp.Headers[nonceHeader];
            if (string.IsNullOrEmpty(NextNonce))
                throw new AcmeException("Missing initial replay-nonce header value");
        }

        #endregion -- Methods --

        #region -- Nested Types --

        public class AcmeHttpResponse
        {
            public AcmeHttpResponse(HttpWebResponse resp)
            {
                StatusCode = resp.StatusCode;
                Headers = resp.Headers;
                using (var s = new StreamReader(resp.GetResponseStream()))
                Links = new LinkCollection(Headers.GetValues("Link"));
                {
                    Content = s.ReadToEnd();
                }
            }

            public HttpStatusCode StatusCode
            { get; set; }

            public WebHeaderCollection Headers
            { get; set; }
            
            public string Content

            public LinkCollection Links
            { get; set; }

            public bool IsError
            { get; set; }

            public Exception Error
            { get; set; }

            public ProblemDetailResponse ProblemDetail
            { get; set; }
        }

        public class AcmeWebException : AcmeException
        {
            public AcmeWebException(WebException innerException, string message = null,
                    AcmeHttpResponse response = null) : base(message, innerException)
            {
                Response = response;
            }

            protected AcmeWebException(SerializationInfo info, StreamingContext context) : base(info, context)
            { }

            public WebException WebException
            {
                get { return InnerException as WebException; }
            }

            public AcmeHttpResponse Response
            { get; private set; }
        }


        #endregion -- Nested Types --
    }
}
