using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Net;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using ACMESharp.ACME;
using ACMESharp.JOSE;
using ACMESharp.Messages;
using ACMESharp.Util;

namespace ACMESharp
{
    /// <summary>
    /// Set of unit tests that test the ACME protocol and interoperability
    /// with the Boulder CA.  We can use these tests to experiment wth Boulder
    /// and channel these lessons into the actual ACME client and supporting
    /// components.
    /// </summary>
    [TestClass]
    public class AcmeUnitTests
    {
        Uri _rootUrl = new Uri("http://acme2.aws3.ezshield.ws:4000/");

        // TODO:  eval if we still need this test since we're going
        //        against the LE STAGE endpoint on a regular basis

        [TestMethod]
        [TestCategory("skipCI")]
        [Ignore] // *Normally*, we skip this test because it depends on a local Boulder setup to accessible
        public void TestNewRegRequest()
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
                    // Tel contact method is no longer supported by Boulder
                    //"tel:+12025551212"
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

        [TestMethod]
        public void TestEcdhKeys()
        {
            // To make sure keys are exportable:
            //    http://stackoverflow.com/questions/20505325/how-to-export-private-key-for-ecdiffiehellmancng/20505976#20505976

            var ecdhKeyParams = new CngKeyCreationParameters
            {
                KeyUsage = CngKeyUsages.AllUsages,
                ExportPolicy = CngExportPolicies.AllowPlaintextExport
            };
            var ecdhKey = CngKey.Create(CngAlgorithm.ECDiffieHellmanP256, null, ecdhKeyParams);
            var ecdh = new ECDiffieHellmanCng(ecdhKey);
            ecdh.KeySize = 256;


            //Export the keys
            var privateKey = ecdh.Key.Export(CngKeyBlobFormat.EccPrivateBlob);

            // This returns:
            //   [ { MinSize = 256; MaxSize = 384; SkipSize = 128 }
            //     { MinSize = 521; MaxSize = 521; SkipSize = 0   } ]
            var keySizes = ecdh.LegalKeySizes;
            // Example of this:
            //      <ECDHKeyValue xmlns="http://www.w3.org/2001/04/xmldsig-more#">
            //        <DomainParameters>
            //          <NamedCurve URN="urn:oid:1.3.132.0.35" />
            //        </DomainParameters>
            //        <PublicKey>
            //          <X Value="6338036285454860977775086861655185721418051140960904673987863656163882965225521398319125216217757952736756437624751684728661860413862054254572205453827782795" xsi:type="PrimeFieldElemType" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" />
            //          <Y Value="2429739115523607678822648112222739155064474393176967830414279652115290771735466025346855521196073509912224542851147234378090051353981358078708633637907317343" xsi:type="PrimeFieldElemType" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" />
            //        </PublicKey>
            //      </ECDHKeyValue>
            var pubKeyXml = ecdh.PublicKey.ToXmlString();
        }

        [TestMethod]
        public void TestChallengAnswerRequest()
        {
            var ansr = new DnsChallengeAnswer
            {
                KeyAuthorization = "TestKeyAuthz",
            };
            var requ = ChallengeAnswerRequest.CreateRequest(ansr);
            var json = JsonConvert.SerializeObject(requ, Formatting.None);
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            Assert.AreEqual(requ.Resource, dict[nameof(requ.Resource)]);

            Assert.AreEqual(ansr.KeyAuthorization, dict[nameof(ansr.KeyAuthorization)]);
        }
    }
}
