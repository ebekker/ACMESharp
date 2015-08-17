using Jose;
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

        #endregion -- Fields --

        #region -- Properties --

        public Uri RootUrl
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
            var foo = new System.Collections.Hashtable
            {
                ["foo1"] = "bar",
            };

            var requ = WebRequest.Create(new Uri(RootUrl, "/"));

            // TODO:  according to ACME 5.5 we *should* be able to issue a HEAD
            // request to get an initial replay-nonce, but this is not working,
            // so we do a GET against the root URL to get that initial nonce
            //requ.Method = "HEAD";
            requ.Method = "GET";

            var resp = requ.GetResponse();

            var nonceHeader = resp.Headers.AllKeys.FirstOrDefault(x =>
                    x.Equals("Replay-nonce", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(nonceHeader))
            {
                throw new AcmeException("Missing initial replay-nonce header");
            }

            NextNonce = resp.Headers[nonceHeader];
            if (string.IsNullOrEmpty(NextNonce))
            {
                throw new AcmeException("Missing initial replay-nonce header value");
            }

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

        public IDictionary<string, string> GetDirectory()
        {
            AssertInit();

            var resp = Web.DownloadString("/acme/directory");

            //var requ = WebRequest.Create(RootUrl);
            //requ.ContentType = "application/json";

            //var resp = requ.GetResponse();

            return null;
        }

        public void Register(string[] contacts)
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

        #endregion -- Methods --
    }
}
