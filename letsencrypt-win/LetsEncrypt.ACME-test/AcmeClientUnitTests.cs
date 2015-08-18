using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LetsEncrypt.ACME.JOSE;

namespace LetsEncrypt.ACME
{
    [TestClass]
    public class AcmeClientUnitTests
    {
        Uri _rootUrl = new Uri("http://acme2.aws3.ezshield.ws:4000/");

        [TestMethod]
        public void TestInit()
        {
            var client = new AcmeClient();
            client.RootUrl = _rootUrl;

            client.Init();
        }

        [TestMethod]
        public void TestGetDirectory()
        {
            var client = new AcmeClient();
            client.RootUrl = _rootUrl;

            var acmeDir = client.GetDirectory();
        }


        [TestMethod]
        public void TestRegister()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var client = new AcmeClient())
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Init();

                    client.Register(new string[] {
                        "mailto:letsencrypt@mailinator.com",
                        "tel:+14109361212",
                    });
                }
            }
        }
    }
}
