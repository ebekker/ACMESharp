using ACMESharp.ACME;
using ACMESharp.Providers.AWS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Providers.IIS
{
    [TestClass]
    public class IisProviderTests
    {
        private static Config.IisHandlerParams _handlerParams = new Config.IisHandlerParams
        {
            WebSiteRef = "Default Web Site",
        };

        public static IisChallengeHandlerProvider GetProvider()
        {
            return new IisChallengeHandlerProvider();
        }

        public static IisChallengeHandler GetHandler(Challenge c)
        {
            return (IisChallengeHandler)GetProvider().GetHandler(c, _handlerParams);
        }

        [TestMethod]
        public void TestParameterDescription()
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

            Assert.IsTrue(p.IsSupported(TestCommon.HTTP_CHALLENGE));
            Assert.IsFalse(p.IsSupported(TestCommon.DNS_CHALLENGE));
            Assert.IsFalse(p.IsSupported(TestCommon.TLS_SNI_CHALLENGE));
            Assert.IsFalse(p.IsSupported(TestCommon.FAKE_CHALLENGE));
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void TestRequiredParams()
        {
            var p = GetProvider();
            var c = TestCommon.HTTP_CHALLENGE;
            var h = p.GetHandler(c, new Dictionary<string, object>());
        }

        [TestMethod]
        public void TestHandleCreateAndCleanUpFiles()
        {
            var r = new Random();
            var bn = new byte[10];
            var bv = new byte[10];
            r.NextBytes(bn);
            r.NextBytes(bv);
            var rn = BitConverter.ToString(bn);
            var rv = BitConverter.ToString(bv);

            var c = new HttpChallenge(AcmeProtocol.CHALLENGE_TYPE_HTTP, new HttpChallengeAnswer())
            {
                Token = "FOOBAR",
                FileUrl = $"http://foobar.acmetesting.zyborg.io/utest/{rn}",
                FilePath = $"utest/{rn}",
                FileContent = rv,
            };

            var awsParams = new AwsCommonParams();
            awsParams.InitParams(_handlerParams);

            var p = GetProvider();
            using (var h = p.GetHandler(c, _handlerParams))
            {
                var sites = IisHelper.ListDistinctHttpWebSites();
                Assert.IsNotNull(sites);
                var site = sites.First(x => x.SiteName == _handlerParams.WebSiteRef);
                Assert.IsNotNull(site);

                var fullPath = Environment.ExpandEnvironmentVariables(
                        Path.Combine(site.SiteRoot, c.FilePath));

                // Assert test file does not exist
                Assert.IsFalse(File.Exists(fullPath));

                // Create the record...
                h.Handle(c);

                // ...and assert it does exist
                Assert.IsTrue(File.Exists(fullPath));
                Assert.AreEqual(c.FileContent, File.ReadAllText(fullPath));

                // Clean up the record...
                h.CleanUp(c);

                // ...and assert it does not exist once more
                Assert.IsFalse(File.Exists(fullPath));
            }
        }
    }
}
