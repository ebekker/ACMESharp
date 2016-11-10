using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.ACME;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACMESharp
{
    [TestClass]
    public class ChallengeHandlerTests
    {
        private static readonly Challenge DNS_CHALLENGE = new DnsChallenge(
                AcmeProtocol.CHALLENGE_TYPE_DNS,
                new DnsChallengeAnswer())
        {
            Token = "FOOBAR",
            RecordName = "acmetest.example.com",
            RecordValue = "0123456789+123456789+123456789+123456789+123456789"
        };

        private static readonly Challenge HTTP_CHALLENGE = new HttpChallenge(
                AcmeProtocol.CHALLENGE_TYPE_HTTP,
                new HttpChallengeAnswer())
        {
            Token = "FOOBAR",
            FileUrl = "http://acmetest.example.com/utest/ABCDEF",
            FilePath = "/utest/ABCDEF",
            FileContent = "0123456789+123456789+123456789+123456789+123456789",
        };


        [TestMethod]
        public void TestBuiltInProvidersExist()
        {
            var provsEnum = ChallengeHandlerExtManager.GetProviderInfos();
            Assert.IsNotNull(provsEnum);

            var provsArr = provsEnum.ToArray();
            Assert.IsTrue(provsArr.Length > 0);
        }

        [TestMethod]
        public void TestProviderInfo()
        {
            var prov = ChallengeHandlerExtManager.GetProviderInfos().First();

            Assert.AreEqual("manual", prov.Name);
            Assert.AreEqual(prov.Info.SupportedTypes,
                ChallengeTypeKind.DNS | ChallengeTypeKind.HTTP);
        }

        [TestMethod]
        public void TestProviderParams()
        {
            var prov = ChallengeHandlerExtManager.GetProvider("manual");
            Assert.IsNotNull(prov);

            var paramsEnum = prov.DescribeParameters();
            Assert.IsNotNull(paramsEnum);

            var paramsArr = paramsEnum.ToArray();
            Assert.IsTrue(paramsArr.Length > 0);
        }

        [TestMethod]
        public void TestIsSupportedDnsChallenge()
        {
            var prov = ChallengeHandlerExtManager.GetProvider("manual");
            Assert.IsNotNull(prov);
            Assert.IsTrue(prov.IsSupported(DNS_CHALLENGE));
        }

        [TestMethod]
        public void TestIsSupportedHttpChallenge()
        {
            var prov = ChallengeHandlerExtManager.GetProvider("manual");
            Assert.IsNotNull(prov);
            Assert.IsTrue(prov.IsSupported(HTTP_CHALLENGE));
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void TestHandlerDisposal()
        {
            var prov = ChallengeHandlerExtManager.GetProvider("manual");
            Assert.IsNotNull(prov);
            var h = prov.GetHandler(DNS_CHALLENGE, null);

            Assert.IsFalse(h.IsDisposed);
            h.Dispose();
            Assert.IsTrue(h.IsDisposed);
            h.Handle(DNS_CHALLENGE);
        }

        [TestMethod]
        public void TestHandleDnsChallenge()
        {
            var prov = ChallengeHandlerExtManager.GetProvider("manual");
            Assert.IsNotNull(prov);
            var h = prov.GetHandler(DNS_CHALLENGE, new Dictionary<string, object>
            {
                { "WriteOutPath", "DBG" }
            });

            h.Handle(DNS_CHALLENGE);
            h.Dispose();
        }

        [TestMethod]
        public void TestHandleHttpChallenge()
        {
            var prov = ChallengeHandlerExtManager.GetProvider("manual");
            Assert.IsNotNull(prov);
            var h = prov.GetHandler(HTTP_CHALLENGE, new Dictionary<string, object>
            {
                { "WriteOutPath", "DBG" }
            });

            h.Handle(HTTP_CHALLENGE);
            h.Dispose();
        }

        [TestMethod]
        public void TestCleanUpDnsChallenge()
        {
            var prov = ChallengeHandlerExtManager.GetProvider("manual");
            Assert.IsNotNull(prov);
            var h = prov.GetHandler(DNS_CHALLENGE, new Dictionary<string, object>
            {
                { "WriteOutPath", "DBG" }
            });

            h.CleanUp(DNS_CHALLENGE);
            h.Dispose();
        }

        [TestMethod]
        public void TestCleanUpHttpChallenge()
        {
            var prov = ChallengeHandlerExtManager.GetProvider("manual");
            Assert.IsNotNull(prov);
            var h = prov.GetHandler(HTTP_CHALLENGE, new Dictionary<string, object>
            {
                { "WriteOutPath", "DBG" }
            });

            h.CleanUp(HTTP_CHALLENGE);
            h.Dispose();
        }
    }
}