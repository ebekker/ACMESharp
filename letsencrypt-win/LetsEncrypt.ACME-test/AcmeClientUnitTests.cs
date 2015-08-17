using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            var client = new AcmeClient();
            client.RootUrl = _rootUrl;
            client.Register(new string[] {
                "mailto:letsencrypt@mailinator.com",
                "tel:+14109361212",
            });
        }
    }
}
