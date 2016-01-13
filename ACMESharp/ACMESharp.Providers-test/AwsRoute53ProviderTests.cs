using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using ACMESharp.ACME;
using ACMESharp.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACMESharp.Providers.AWS
{
    [TestClass]
    public class AwsRoute53ProviderTests
    {
        private static Config.AwsR53HandlerParams _handlerParams;

        [ClassInitialize]
        public static void Init(TestContext tctx)
        {
            using (var fs = new FileStream("Config\\AwsR53HandlerParams.json", FileMode.Open))
            {
                _handlerParams = JsonHelper.Load<Config.AwsR53HandlerParams>(fs);
            }
        }

        public static AwsRoute53ChallengeHandlerProvider GetProvider()
        {
            return new AwsRoute53ChallengeHandlerProvider();
        }

        public static AwsRoute53ChallengeHandler GetHandler(Challenge c)
        {
            return (AwsRoute53ChallengeHandler)GetProvider().GetHandler(c, _handlerParams);
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
            var h = p.GetHandler(c, _handlerParams);

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
            var h = p.GetHandler(c, _handlerParams);

            h.Dispose();
            h.Handle(null);
        }

        [TestMethod]
        public void TestHandlerDefineAndCleanUpResourceRecord()
        {
            var r = new Random();
            var bn = new byte[4];
            var bv = new byte[10];
            r.NextBytes(bn);
            r.NextBytes(bv);
            var rn = BitConverter.ToString(bn);
            var rv = BitConverter.ToString(bv);

            var c = new DnsChallenge(AcmeProtocol.CHALLENGE_TYPE_DNS, new DnsChallengeAnswer())
            {
                Token = "FOOBAR",
                RecordName = $"{rn}.{_handlerParams.DefaultDomain}",
                RecordValue = rv,
            };

            var r53 = new Route53Helper
            {
                HostedZoneId = _handlerParams.HostedZoneId,
            };
            r53.CommonParams.InitParams(_handlerParams);

            var p = GetProvider();
            using (var h = p.GetHandler(c, _handlerParams))
            {
                // Assert test record does *not* exist
                var rr = r53.GetRecords(c.RecordName);
                var rrFirst = rr.ResourceRecordSets.FirstOrDefault(x =>
                    x.Name.ToLower().StartsWith(c.RecordName.ToLower()))?.Name;

                Assert.IsNull(rrFirst);

                // Create the record...
                h.Handle(c);

                // ...and assert it does exist
                rr = r53.GetRecords(c.RecordName);
                rrFirst = rr.ResourceRecordSets.FirstOrDefault(x =>
                    x.Name.ToLower().StartsWith(c.RecordName.ToLower()))?.Name;

                Assert.IsNotNull(rrFirst);
                StringAssert.StartsWith(rrFirst.ToLower(), c.RecordName.ToLower());

                // Clean up the record...
                h.CleanUp(c);

                // ...and assert it does not exist once more
                rr = r53.GetRecords(c.RecordName);
                rrFirst = rr.ResourceRecordSets.FirstOrDefault(x =>
                    x.Name.ToLower().StartsWith(c.RecordName.ToLower()))?.Name;

                Assert.IsNull(rrFirst);
            }
        }
    }
}
