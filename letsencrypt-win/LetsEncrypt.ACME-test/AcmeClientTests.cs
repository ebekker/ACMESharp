using LetsEncrypt.ACME.JOSE;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME
{
    [TestFixture]
    public class AcmeClientTests
    {
        Uri _rootUrl = new Uri("http://acme2.aws3.ezshield.ws:4000/");

        [Test]
        [Category("skipCI")]
        public void Test0010_Init()
        {
            using (var signer = new RS256Signer())
            {
                using (var client = new AcmeClient(_rootUrl, signer: signer))
                {
                    client.Init();

                    Assert.IsNotNull(client.Directory);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(client.NextNonce));
                }
            }
        }

        [Test]
        [Category("skipCI")]
        public void Test0020_GetDirectory()
        {
            var boulderUrlBase = "http://localhost:4000";
            var boulderResMap = new Dictionary<string, string>
            {
                ["new-authz"] = "http://localhost:4000/acme/new-authz",
                ["new-cert"] = "http://localhost:4000/acme/new-cert",
                ["new-reg"] = "http://localhost:4000/acme/new-reg",
                ["revoke-cert"] = "http://localhost:4000/acme/revoke-cert",
            };

            using (var signer = new RS256Signer())
            {
                using (var client = new AcmeClient(_rootUrl, signer: signer))
                {
                    client.Init();

                    // Test absolute URI paths
                    var acmeDir = client.GetDirectory(false);
                    foreach (var ent in boulderResMap)
                    {
                        Assert.IsTrue(acmeDir.Contains(ent.Key));
                        Assert.AreEqual(ent.Value, acmeDir[ent.Key]);
                    }

                    // Test relative URI paths
                    acmeDir = client.GetDirectory(true);
                    foreach (var ent in boulderResMap)
                    {
                        var relUrl = ent.Value.Replace(boulderUrlBase, "");
                        Assert.IsTrue(acmeDir.Contains(ent.Key));
                        Assert.AreEqual(relUrl, acmeDir[ent.Key]);
                    }
                }
            }
        }

    }
}
