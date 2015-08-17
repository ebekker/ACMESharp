using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using LetsEncrypt.ACME.JOSE;
using System.Net;
using System.Linq;
using System.Text;

namespace LetsEncrypt.ACME
{
    [TestClass]
    public class AcmeUnitTest1
    {
        Uri _rootUrl = new Uri("http://acme2.aws3.ezshield.ws:4000/");

        [TestMethod]
        public void TestNewReg()
        {
            var requ = WebRequest.Create(_rootUrl);
            var resp = requ.GetResponse();
            Assert.IsNotNull(resp);

            var nonceKey = resp.Headers.AllKeys.FirstOrDefault(
                    x => x.Equals("Replay-nonce", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(string.IsNullOrEmpty(nonceKey));
            var nonceValue = resp.Headers[nonceKey];

            var newReg = new
            {
                resource = "new-reg",
                contact = new string[]
                {
                    "mailto:cert-admin@example.com",
                    "tel:+12025551212"
                },
            };
            var newRegSer = JsonConvert.SerializeObject(newReg);

            var algSigner = new RS256Signer();
            algSigner.Init();

            var unprotectedHeader = new
            {
                alg = "RS256",
                jwk = algSigner.ExportJwk()
            };
            var protectedHeader = new
            {
                nonce = nonceValue,
            };

            var acmeJson = JwsHelper.SignFlatJson(algSigner.Sign, newRegSer,
                    protectedHeader, unprotectedHeader);
            var acmeJsonBytes = Encoding.ASCII.GetBytes(acmeJson);

            requ = WebRequest.Create(new Uri(_rootUrl, "/acme/new-reg"));
            requ.Method = "POST";
            requ.ContentType = "application/json";
            requ.ContentLength = acmeJsonBytes.Length;
            using (var s = requ.GetRequestStream())
            {
                s.Write(acmeJsonBytes, 0, acmeJsonBytes.Length);
            }
            resp = requ.GetResponse();
        }
    }
}
