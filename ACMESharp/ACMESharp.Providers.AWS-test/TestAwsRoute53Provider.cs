using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using ACMESharp.ACME;
using ACMESharp.POSH.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACMESharp.Providers.AWS
{
    [TestClass]
    public class TestAwsRoute53Provider
    {
        private static config.DnsConfig _dnsConfig;

        [ClassInitialize]
        public static void Init(TestContext tctx)
        {
            using (var fs = new FileStream("config\\dnsConfig.json", FileMode.Open))
            {
                _dnsConfig = JsonHelper.Load<config.DnsConfig>(fs);
            }
        }

        public static AwsRoute53ChallengeHandlerProvider GetProvider()
        {
            return new AwsRoute53ChallengeHandlerProvider();
        }

        public static AwsRoute53ChallengeHandler GetHandler(Challenge c)
        {
            return (AwsRoute53ChallengeHandler)GetProvider().GetHandler(c, _dnsConfig);
        }

        [TestMethod]
        public void TestParameterDescriptions()
        {
            var p = GetProvider();
            var dp = p.DescribeParameters();

            Assert.IsNotNull(dp);
            Assert.IsTrue(dp.Count() > 0);
        }

        [TestMethod]
        public void TestSupportedChallenges()
        {
            var p = GetProvider();

            Assert.IsTrue(p.IsSupported(new DnsChallenge()));
            Assert.IsFalse(p.IsSupported(new HttpChallenge()));
            Assert.IsFalse(p.IsSupported(new TlsSniChallenge()));
            Assert.IsFalse(p.IsSupported(new FakeChallenge()));
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void TestRequiredParams()
        {
            var p = GetProvider();
            var c = new DnsChallenge();
            var h = p.GetHandler(c, new Dictionary<string, object>());
        }

        [TestMethod]
        public void TestHandlerLifetime()
        {
            var p = GetProvider();
            var c = new DnsChallenge();
            var h = p.GetHandler(c, _dnsConfig);

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
            var c = new DnsChallenge();
            var h = p.GetHandler(c, _dnsConfig);

            h.Dispose();
            h.Handle(null);
        }

        class FakeChallenge : Challenge
        { }
    }
}
