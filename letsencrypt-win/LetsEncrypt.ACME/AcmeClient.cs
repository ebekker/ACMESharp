using Jose;
using LetsEncrypt.ACME.JOSE;
using LetsEncrypt.ACME.JSON;
using LetsEncrypt.ACME.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LetsEncrypt.ACME
{
    /// <summary>
    /// The ACME client encapsulates all the protocol rules to interact
    /// with an ACME client as specified by the ACME specficication.
    /// </summary>
    public class AcmeClient : IDisposable
    {
        #region -- Fields --

        WebClient _Web;
        JsonSerializerSettings _jsonSettings = new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
            ContractResolver = new AcmeJsonContractResolver(),
        };


        #endregion -- Fields --

        #region -- Properties --

        public Uri RootUrl
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

        public IDictionary<string, string> GetDirectory()
        {
            AssertInit();

            var resp = Web.DownloadString("/acme/directory");

            //var requ = WebRequest.Create(RootUrl);
            //requ.ContentType = "application/json";

            //var resp = requ.GetResponse();

            return null;
        }

        public void RegisterXXX(string[] contacts)
        {
            AssertInit();

            var requMesg = new NewRegRequest
            {
                Contact = contacts,
            };

            //var certBytes = Certificate.CreateSelfSignCertificatePfx(null, DateTime.Now.AddMinutes(-10), DateTime.Now.AddYears(1));
            //var cert = new X509Certificate2(certBytes);
            //var privateKey = cert.PrivateKey as RSACryptoServiceProvider;
            //var privateKeyBlob = privateKey.ExportCspBlob(true);
            //var privateKey2 = new RSACryptoServiceProvider(new CspParameters(24));
            //privateKey2.ImportCspBlob(privateKeyBlob);

            // With help from:
            //    https://github.com/dvsekhvalnov/jose-jwt/blob/master/UnitTests/jwt-2048.p12
            //    https://github.com/dvsekhvalnov/jose-jwt/blob/master/UnitTests/TestSuite.cs
            var cert = new X509Certificate2(@"C:\prj\letsencrypt\solutions\letsencrypt-win\letsencrypt-win\jwt-2048.p12",
                    "1", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);
            var privateKey = cert.PrivateKey as RSACryptoServiceProvider;
            var privateKey2 = new RSACryptoServiceProvider();
            privateKey2.ImportParameters(privateKey.ExportParameters(true));

            var jsonSettings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                ContractResolver = new AcmeJsonContractResolver(),
            };

            var requBody = JsonConvert.SerializeObject(requMesg, jsonSettings);
            var requWrap = Jose.JWT.Encode(requBody, privateKey2, JwsAlgorithm.RS256);

            var respBody = Web.UploadString("/acme/new-reg", "GET", requBody);
        }

        public AcmeRegistration Register(string[] contacts)
        {
            AssertInit();

            var message = new NewRegRequest
            {
                Contact = contacts,
            };

            var resp = PostRequest(new Uri(RootUrl, "/acme/new-reg"), message);
            var regUri = resp.Headers["Location"];
            if (string.IsNullOrEmpty(regUri))
                throw new AcmeException("server did not provide a registration URI in the response");

            // HTTP 409 (Conflict) response for a previously registered pub key
            //    Location:  still had the regUri
            if (resp.IsError && resp.StatusCode == HttpStatusCode.Conflict)
                throw new AcmeException("Conflict due to previously registered public key");

            // TODO:  Link headers can be returned:
            //   HTTP/1.1 201 Created
            //   Content-Type: application/json
            //   Location: https://example.com/acme/reg/asdf
            //   Link: <https://example.com/acme/new-authz>;rel="next"
            //   Link: <https://example.com/acme/recover-reg>;rel="recover"
            //   Link: <https://example.com/acme/terms>;rel="terms-of-service"
            //
            // The "terms-of-service" URI should be included in the "agreement" field
            // in a subsequent registration update


            Registration = new AcmeRegistration
            {
                PublicKey = Signer.ExportJwk(),
                Contacts = contacts,
                RegistrationUri = regUri,
            };

            return Registration;
        }

        public void AuthorizeDns(string dnsIdentifier)
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

            var resp = PostRequest(new Uri(RootUrl, "/acme/new-authz"), requMsg);
            var respMsg = JsonConvert.DeserializeObject<NewAuthzResponse>(resp.Content);
        }

        private PostResponse PostRequest(Uri uri, object message)
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
                    return new PostResponse(resp);
                }
            }
            catch (WebException ex)
            {
                using (var resp = (HttpWebResponse)ex.Response)
                {
                    return new PostResponse(resp)
                    {
                        IsError = true,
                    };
                }
            }
        }

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
            var payload = JsonConvert.SerializeObject(message);
            var acmeSigned = JwsHelper.SignFlatJson(Signer.Sign, payload,
                    protectedHeader, unprotectedHeader);

            return acmeSigned;
        }

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

        protected class PostResponse
        {
            public PostResponse(HttpWebResponse resp)
            {
                StatusCode = resp.StatusCode;
                Headers = resp.Headers;
                using (var s = new StreamReader(resp.GetResponseStream()))
                {
                    Content = s.ReadToEnd();
                }
            }

            public bool IsError
            { get; set; }

            public HttpStatusCode StatusCode
            { get; set; }

            public WebHeaderCollection Headers
            { get; set; }
            
            public string Content
            { get; set; }


        }

        #endregion -- Nested Types --
    }
}
