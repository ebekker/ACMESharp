using ACMESharp.ACME;
using ACMESharp.Providers.CloudFlare;
using ACMESharp.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ACMESharp.Providers
{
    [TestClass]
    public class CloudFlareTests
    {
        public static readonly IReadOnlyDictionary<string, object> EMPTY_PARAMS =
                new Dictionary<string, object>()
        {
            ["DomainName"] = "",
            ["AuthKey"] = "",
            ["EmailAddress"] = "",
        };

        private static IReadOnlyDictionary<string, object> _handlerParams = EMPTY_PARAMS;

        private static IReadOnlyDictionary<string, object> GetParams()
        {
            return _handlerParams;
        }

        [ClassInitialize]
        public static void Init(TestContext tctx)
        {
            var file = new FileInfo("Config\\CloudFlareHandlerParams.json");
            if (file.Exists)
            {
                using (var fs = new FileStream(file.FullName, FileMode.Open))
                {
                    _handlerParams = JsonHelper.Load<Dictionary<string, object>>(fs);
                }
            }
        }

        public static CloudFlareChallengeHandlerProvider GetProvider()
        {
            return new CloudFlareChallengeHandlerProvider();
        }

        public static CloudFlareChallengeHandler GetHandler(Challenge challenge)
        {
            return (CloudFlareChallengeHandler)GetProvider().GetHandler(challenge, null);
        }

        public static CloudFlareHelper GetHelper()
        {
            var p = GetParams();
            var h = new CloudFlareHelper(
                    (string)p["AuthKey"],
                    (string)p["EmailAddress"],
                    (string)p["DomainName"]
                );
            return h;
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

        [TestMethod]
        public void TestAddDnsRecord()
        {
            var h = GetHelper();
            var rrName = "acmesharp-test." + GetParams()["DomainName"];
            var rrValue = "testrr-" + DateTime.Now.ToString("yyyyMMddHHmmss #1");

            h.AddOrUpdateDnsRecord(rrName, rrValue);
        }

        [TestMethod]
        public void TestUpdateDnsRecord()
        {
            var h = GetHelper();
            var rrName = "acmesharp-test." + GetParams()["DomainName"];
            var rrValue = "testrr-" + DateTime.Now.ToString("yyyyMMddHHmmss #2");

            h.AddOrUpdateDnsRecord(rrName, rrValue);
        }

        [TestMethod]
        public void TestDeleteDnsRecord()
        {
            var h = GetHelper();
            var rrName = "acmesharp-test." + GetParams()["DomainName"];

            h.DeleteDnsRecord(rrName);
        }
    }
}
