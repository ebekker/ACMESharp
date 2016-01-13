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
    public class AwsS3ProviderTests
    {
        private static Config.AwsS3HandlerParams _handlerParams;

        [ClassInitialize]
        public static void Init(TestContext tctx)
        {
            using (var fs = new FileStream("Config\\AwsS3HandlerParams.json", FileMode.Open))
            {
                _handlerParams = JsonHelper.Load<Config.AwsS3HandlerParams>(fs);
            }
        }

        public static AwsS3ChallengeHandlerProvider GetProvider()
        {
            return new AwsS3ChallengeHandlerProvider();
        }

        public static AwsS3ChallengeHandler GetHandler(Challenge c)
        {
            return (AwsS3ChallengeHandler)GetProvider().GetHandler(c, _handlerParams);
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
        public void TestHandlerLifetime()
        {
            var p = GetProvider();
            var c = TestCommon.HTTP_CHALLENGE;
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
            var c = TestCommon.HTTP_CHALLENGE;
            var h = p.GetHandler(c, _handlerParams);

            h.Dispose();
            h.Handle(null);
        }

        [TestMethod]
        public void TestHandlerUploadAndCleanUpObject()
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
                FilePath = $"/utest/{rn}",
                FileContent = rv,
            };

            var awsParams = new AwsCommonParams();
            awsParams.InitParams(_handlerParams);

            var p = GetProvider();
            using (var h = p.GetHandler(c, _handlerParams))
            {
                // Assert test file does not exist
                var s3Obj = AwsS3ChallengeHandler.GetFile(awsParams,
                        _handlerParams.BucketName, c.FilePath);
                
                // Assert test record does *not* exist
                Assert.IsNull(s3Obj);

                // Create the record...
                h.Handle(c);

                // ...and assert it does exist
                s3Obj = AwsS3ChallengeHandler.GetFile(awsParams,
                        _handlerParams.BucketName, c.FilePath);

                Assert.IsNotNull(s3Obj);
                using (var sr = new StreamReader(s3Obj.ResponseStream))
                {
                    var v = sr.ReadToEnd();
                    Assert.AreEqual(c.FileContent, v);
                }

                // Clean up the record...
                h.CleanUp(c);

                // ...and assert it does not exist once more
                s3Obj = AwsS3ChallengeHandler.GetFile(awsParams,
                        _handlerParams.BucketName, c.FilePath);

                Assert.IsNull(s3Obj);
            }
        }
    }
}
