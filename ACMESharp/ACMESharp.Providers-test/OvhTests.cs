using ACMESharp.ACME;
using ACMESharp.Providers.OVH;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Providers
{
   public class OvhTests
    {
        private Dictionary<string, object> GetParams()
        {
            return new Dictionary<string, object>()
            {
                {"Endpoint", "" },
                {"ApplicationKey", "" },
                {"ApplicationSecret", "" },
                {"ConsumerKey", "" }
            };
        }
        
        public static OvhChallengeHandlerProvider GetProvider()
        {
            return new OvhChallengeHandlerProvider();
        }

        public static OvhChallengeHandlerProvider GetHandler(Challenge challenge)
        {
            return (OvhChallengeHandlerProvider) GetProvider().GetHandler(challenge, null);
        }


        [TestMethod]
        public void TestParameterDescriptions()
        {
            var p = GetProvider();
            var dp = p.DescribeParameters();

            Assert.IsNotNull(dp);
            Assert.IsTrue(dp.Any());
        }

        [TestMethod]
        public void TestSupportedChallenges()
        {
            var p = GetProvider();

            Assert.IsTrue(p.IsSupported(TestCommon.DNS_CHALLENGE));
            Assert.IsFalse(p.IsSupported(TestCommon.HTTP_CHALLENGE));
            Assert.IsFalse(p.IsSupported(TestCommon.TLS_SNI_CHALLENGE));
            Assert.IsFalse(p.IsSupported(TestCommon.FAKE_CHALLENGE));
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void TestRequiredParams()
        {
            var p = GetProvider();
            var c = TestCommon.DNS_CHALLENGE;
            var h = p.GetHandler(c, new Dictionary<string, object>());
        }

        [TestMethod]
        public void TestHandlerLifetime()
        {
            var p = GetProvider();
            var c = TestCommon.DNS_CHALLENGE;
            var h = p.GetHandler(c, GetParams());

            Assert.IsNotNull(h);
            Assert.IsFalse(h.IsDisposed);
            h.Dispose();
            Assert.IsTrue(h.IsDisposed);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestHandlerDisposedAccess()
        {
            var p = GetProvider();
            var c = TestCommon.DNS_CHALLENGE;
            var h = p.GetHandler(c, GetParams());

            h.Dispose();
            h.Handle(null);
        }

    }
}
