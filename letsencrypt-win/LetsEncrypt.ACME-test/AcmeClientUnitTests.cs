using LetsEncrypt.ACME.DNS;
using LetsEncrypt.ACME.JOSE;
using LetsEncrypt.ACME.PKI;
using LetsEncrypt.ACME.WebServer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace LetsEncrypt.ACME
{
    [TestClass]
    public class AcmeClientUnitTests
    {
        public const string DEFAULT_BASE_LOCAL_STORE = "..\\lostore";
        public const string WEB_PROXY_CONFIG = "testProxyConfig.json";

        // Running against a local (private) instance of Boulder CA
        //Uri _rootUrl = new Uri("http://acme2.aws3.ezshield.ws:4000/");
        //string _dirUrlBase = "http://localhost:4000/";

        // Running against the STAGE (public) instance of Boulder CA
        Uri _rootUrl = new Uri("https://acme-staging.api.letsencrypt.org/");
        string _dirUrlBase = "https://acme-staging.api.letsencrypt.org/";

        public const string TEST_CN1 = "acme-win.acmetesting.zyborg.io";
        public const string TEST_EM1 = "mailto:letsencrypt@mailinator.com";
        public const string TEST_PH1 = "tel:+14109361212";
        public const string TEST_EM2 = "mailto:letsencrypt+update@mailinator.com";

        private static WebProxyConfig _wpConfig;
        private static IWebProxy _proxy;

        private static string _baseLocalStore = DEFAULT_BASE_LOCAL_STORE;

        private static string _testRegister_AcmeSignerFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestRegister.acmeSigner";
        private static string _testRegister_AcmeRegFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestRegister.acmeReg";
        private static string _testRegisterUpdate_AcmeRegFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestRegisterUpdate.acmeReg";

        private static string _testAuthz_AcmeAuthzFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestAuthz.acmeAuthz";
        private static string _testAuthzRefresh_AcmeAuthzFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestAuthz-Refresh.acmeAuthz";
        private static string _testAuthzChallengeDnsRefresh_AcmeAuthzFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestAuthz-DnsChallengeRefreshed.acmeAuthz";
        private static string _testAuthzChallengeLegacyHttpRefresh_AcmeAuthzFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestAuthz-LegacyHttpChallengeRefreshed.acmeAuthz";
        private static string _testAuthzChallengeHttpRefresh_AcmeAuthzFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestAuthz-HttpChallengeRefreshed.acmeAuthz";
        private static string _testAuthzChallengeAnswers_AcmeAuthzFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestAuthz-ChallengeAnswers.acmeAuthz";
        private static string _testAuthzChallengeDnsHandled_AcmeAuthzFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestAuthz-ChallengeAnswersHandleDns.acmeAuthz";
        private static string _testAuthzChallengeDnsAnswered_AcmeAuthzFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestAuthz-DnsChallengeAnswered.acmeAuthz";
        private static string _testAuthzChallengeLegacyHttpHandled_AcmeAuthzFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestAuthz-ChallengeAnswersHandleLegacyHttp.acmeAuthz";
        private static string _testAuthzChallengeHttpHandled_AcmeAuthzFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestAuthz-ChallengeAnswersHandleHttp.acmeAuthz";
        private static string _testAuthzChallengeLegacyHttpAnswered_AcmeAuthzFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestAuthz-LegacyHttpChallengeAnswered.acmeAuthz";
        private static string _testAuthzChallengeHttpAnswered_AcmeAuthzFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestAuthz-HttpChallengeAnswered.acmeAuthz";

        private static string _testGenCsr_RsaKeysFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestGenCsr-rsaKeys.txt";
        private static string _testGenCsr_CsrDetailsFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestGenCsr-csrDetails.txt";
        private static string _testGenCsr_CsrFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestGenCsr-csr.txt";

        private static string _testCertRequ_AcmeCertRequFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestCertRequ.acmeCertRequ";
        private static string _testCertRequRefreshed_AcmeCertRequFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestCertRequ-Refreshed.acmeCertRequ";
        private static string _testCertRequRefreshed_CerFile = $"{DEFAULT_BASE_LOCAL_STORE}\\TestCertRequ-Refreshed.cer";

        [ClassInitialize]
        public static void OneTimeSetup(TestContext tctx)
        {
            if (!Directory.Exists(DEFAULT_BASE_LOCAL_STORE))
                Directory.CreateDirectory(DEFAULT_BASE_LOCAL_STORE);

            //_baseLocalStore = $"{DEFAULT_BASE_LOCAL_STORE}-{DateTime.Now.ToString("yyMMdd-HHmmss")}";
            //if (!Directory.Exists(_baseLocalStore))
            //    Directory.CreateDirectory(_baseLocalStore);

            if (File.Exists(WEB_PROXY_CONFIG))
            {
                _wpConfig = WebProxyConfig.Load(WEB_PROXY_CONFIG);
                if (_wpConfig != null && _wpConfig.UseProxy)
                {
                    _proxy = new WebProxy(_wpConfig.HostName, _wpConfig.HostPort);
                    if (_wpConfig.AcceptAllServerCerts)
                    {
                        System.Net.ServicePointManager.ServerCertificateValidationCallback =
                                (a, b, c, d) =>
                                {
                                    return true;
                                };
                    }
                }
            }
        }

        private static AcmeClient BuildClient(Uri rootUrl = null, ISigner signer = null, string testTagHeader = null)
        {
            var c = new AcmeClient(rootUrl, signer: signer);

            if (_proxy != null)
                c.Proxy = _proxy;
            if (testTagHeader != null)
                c.BeforeGetResponseAction = x =>
                {
                    x.Headers.Add("X-ACME-TestTag", testTagHeader);
                };

            return c;
        }

        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0010_Init()
        {
            using (var signer = new RS256Signer())
            {
                using (var client = BuildClient(_rootUrl, signer: signer, testTagHeader: nameof(Test0010_Init)))
                {
                    client.Init();

                    Assert.IsNotNull(client.Directory);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(client.NextNonce));
                }
            }
        }

        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0020_GetDirectory()
        {
            var boulderResMap = new Dictionary<string, string>
            {
                ["new-authz"]   /**/ = $"{_dirUrlBase}acme/new-authz",
                ["new-cert"]    /**/ = $"{_dirUrlBase}acme/new-cert",
                ["new-reg"]     /**/ = $"{_dirUrlBase}acme/new-reg",
                ["revoke-cert"] /**/ = $"{_dirUrlBase}acme/revoke-cert",
            };

            using (var signer = new RS256Signer())
            {
                using (var client = BuildClient(_rootUrl, signer: signer, testTagHeader: nameof(Test0020_GetDirectory)))
                {
                    client.Init();

                    // Test absolute URI paths
                    var acmeDirAbs = client.GetDirectory(false);
                    foreach (var ent in boulderResMap)
                    {
                        Assert.IsTrue(acmeDirAbs.Contains(ent.Key));
                        Assert.AreEqual(ent.Value, acmeDirAbs[ent.Key]);
                    }

                    // Test relative URI paths
                    var acmeDirRel = client.GetDirectory(true);
                    foreach (var ent in boulderResMap)
                    {
                        var relUrl = ent.Value.Replace(_dirUrlBase, "/");
                        Assert.IsTrue(acmeDirRel.Contains(ent.Key));
                        Assert.AreEqual(relUrl, acmeDirRel[ent.Key]);
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0030_Register()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                if (!_wpConfig.UseNewSigner)
                {
                    // Re-use existing Signer config from stable local store
                    using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
                    {
                        signer.Load(fs);
                    }
                }
                _testRegister_AcmeSignerFile = $"{_baseLocalStore}\\TestRegister.acmeSigner";
                using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Create))
                {
                    signer.Save(fs);
                }

                using (var client = BuildClient(testTagHeader: nameof(Test0030_Register)))
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Init();

                    client.GetDirectory(true);

                    client.Register(new string[] { TEST_EM1, TEST_PH1, });

                    Assert.IsNotNull(client.Registration);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(client.Registration.RegistrationUri));

                    _testRegister_AcmeRegFile = $"{_baseLocalStore}\\TestRegister.acmeReg";
                    using (var fs = new FileStream(_testRegister_AcmeRegFile, FileMode.Create))
                    {
                        client.Registration.Save(fs);
                    }
                }
            }
        }

        /// <summary>
        /// An <i>empty update</i> does not request any registration data elements be
        /// updated and should simply return the current state of the target registration
        /// (<see cref="https://letsencrypt.github.io/acme-spec/#rfc.section.6.3">ACME
        /// spec 6.3</see>).
        /// </summary>
        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0040_RegisterEmptyUpdate()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream(_testRegister_AcmeRegFile, FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = BuildClient(testTagHeader: nameof(Test0040_RegisterEmptyUpdate)))
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    // Do a simple update with no data changes requested
                    client.UpdateRegistration(true);

                    Assert.IsNotNull(client.Registration);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(client.Registration.RegistrationUri));

                    _testRegisterUpdate_AcmeRegFile = $"{_baseLocalStore}\\TestRegisterEmptyUpdate.acmeReg";
                    using (var fs = new FileStream(_testRegisterUpdate_AcmeRegFile, FileMode.Create))
                    {
                        client.Registration.Save(fs);
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0050_RegisterUpdateTosAgreement()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream(_testRegister_AcmeRegFile, FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = BuildClient(testTagHeader: nameof(Test0050_RegisterUpdateTosAgreement)))
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    client.UpdateRegistration(true, true);

                    Assert.IsNotNull(client.Registration);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(client.Registration.RegistrationUri));

                    _testRegisterUpdate_AcmeRegFile = $"{_baseLocalStore}\\TestRegisterUpdate.acmeReg";
                    using (var fs = new FileStream(_testRegisterUpdate_AcmeRegFile, FileMode.Create))
                    {
                        client.Registration.Save(fs);
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0060_RegisterUpdateContacts()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream(_testRegister_AcmeRegFile, FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = BuildClient(testTagHeader: nameof(Test0060_RegisterUpdateContacts)))
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    client.UpdateRegistration(true, contacts: new string[] { TEST_EM2, });

                    Assert.IsNotNull(client.Registration);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(client.Registration.RegistrationUri));

                    _testRegisterUpdate_AcmeRegFile = $"{_baseLocalStore}\\TestRegisterUpdate.acmeReg";
                    using (var fs = new FileStream(_testRegisterUpdate_AcmeRegFile, FileMode.Create))
                    {
                        client.Registration.Save(fs);
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0070_RegisterDuplicate()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
                {
                    signer.Load(fs);
                }

                using (var client = BuildClient(testTagHeader: nameof(Test0070_RegisterDuplicate)))
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Init();

                    client.GetDirectory(true);

                    try
                    {
                        client.Register(new string[]
                                {
                                    "mailto:letsencrypt+dup@mailinator.com",
                                    "tel:+14105551212",
                                });
                        Assert.Fail("WebException expected");
                    }
                    catch (AcmeClient.AcmeWebException ex)
                    {
                        Assert.IsNotNull(ex.WebException);
                        Assert.IsNotNull(ex.Response);
                        Assert.AreEqual(HttpStatusCode.Conflict, ex.Response.StatusCode);
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0080_AuthorizeDnsBlacklisted()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream(_testRegister_AcmeRegFile, FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = BuildClient(testTagHeader: nameof(Test0080_AuthorizeDnsBlacklisted)))
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    try
                    {
                        client.AuthorizeIdentifier("acme-win-test.example.com");
                    }
                    catch (AcmeClient.AcmeWebException ex)
                    {
                        Assert.IsNotNull(ex.WebException);
                        Assert.IsNotNull(ex.Response);
                        Assert.IsNotNull(ex.Response.ProblemDetail);
                        Assert.AreEqual(HttpStatusCode.Forbidden, ex.Response.StatusCode);
                        Assert.AreEqual("urn:acme:error:unauthorized", ex.Response.ProblemDetail.Type);
                        StringAssert.Contains(ex.Response.ProblemDetail.Detail, "blacklist");
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0090_AuthorizeIdentifier()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream(_testRegister_AcmeRegFile, FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = BuildClient(testTagHeader: nameof(Test0090_AuthorizeIdentifier)))
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    var authzState = client.AuthorizeIdentifier(TEST_CN1);

                    foreach (var c in authzState.Challenges)
                    {
                        if (c.Type == AcmeProtocol.CHALLENGE_TYPE_LEGACY_DNS
                                || c.Type == AcmeProtocol.CHALLENGE_TYPE_DNS)
                        {
                            var dnsResponse = c.GenerateDnsChallengeAnswer(
                                    authzState.Identifier, signer);
                        }
                        else if (c.Type == AcmeProtocol.CHALLENGE_TYPE_LEGACY_HTTP)
                        {
                            var httpResponse = c.GenerateLegacyHttpChallengeAnswer(
                                authzState.Identifier, signer, false);
                        }
                        else if (c.Type == AcmeProtocol.CHALLENGE_TYPE_HTTP)
                        {
                            var httpResponse = c.GenerateHttpChallengeAnswer(
                                authzState.Identifier, signer);
                        }
                    }

                    _testAuthz_AcmeAuthzFile = $"{_baseLocalStore}\\TestAuthz.acmeAuthz";
                    using (var fs = new FileStream(_testAuthz_AcmeAuthzFile, FileMode.Create))
                    {
                        authzState.Save(fs);
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0095_RefreshIdentifierAuthorization()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream(_testRegister_AcmeRegFile, FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = BuildClient(testTagHeader: nameof(Test0095_RefreshIdentifierAuthorization)))
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    AuthorizationState authzState;
                    using (var fs = new FileStream(_testAuthz_AcmeAuthzFile, FileMode.Open))
                    {
                        authzState = AuthorizationState.Load(fs);
                    }

                    var authzRefreshState = client.RefreshIdentifierAuthorization(authzState, true);

                    _testAuthzRefresh_AcmeAuthzFile = $"{_baseLocalStore}\\TestAuthz-Refresh.acmeAuthz";
                    using (var fs = new FileStream(_testAuthzRefresh_AcmeAuthzFile, FileMode.Create))
                    {
                        authzRefreshState.Save(fs);
                    }
                }
            }
        }

        //[TestMethod]
        //[TestCategory("skipCI")]
        //public void Test0100_RefreshAuthzDnsChallenge()
        //{
        //    using (var signer = new RS256Signer())
        //    {
        //        signer.Init();
        //        using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
        //        {
        //            signer.Load(fs);
        //        }
        //
        //        AcmeRegistration reg;
        //        using (var fs = new FileStream(_testRegister_AcmeRegFile, FileMode.Open))
        //        {
        //            reg = AcmeRegistration.Load(fs);
        //        }
        //
        //        using (var client = BuildClient(testTagHeader: nameof(Test0100_RefreshAuthzDnsChallenge)))
        //        {
        //            client.RootUrl = _rootUrl;
        //            client.Signer = signer;
        //            client.Registration = reg;
        //            client.Init();
        //
        //            client.GetDirectory(true);
        //
        //            AuthorizationState authzState;
        //            using (var fs = new FileStream(_testAuthz_AcmeAuthzFile, FileMode.Open))
        //            {
        //                authzState = AuthorizationState.Load(fs);
        //            }
        //
        //            client.RefreshAuthorizeChallenge(authzState, AcmeProtocol.CHALLENGE_TYPE_DNS, true);
        //
        //            _testAuthzChallengeDnsRefresh_AcmeAuthzFile = $"{_baseLocalStore}\\TestAuthz-DnsChallengeRefreshed.acmeAuthz";
        //            using (var fs = new FileStream(_testAuthzChallengeDnsRefresh_AcmeAuthzFile, FileMode.Create))
        //            {
        //                authzState.Save(fs);
        //            }
        //        }
        //    }
        //}

        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0110_RefreshAuthzLegacyHttpChallenge()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream(_testRegister_AcmeRegFile, FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = BuildClient(testTagHeader: nameof(Test0110_RefreshAuthzLegacyHttpChallenge)))
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    AuthorizationState authzState;
                    using (var fs = new FileStream(_testAuthz_AcmeAuthzFile, FileMode.Open))
                    {
                        authzState = AuthorizationState.Load(fs);
                    }

                    client.RefreshAuthorizeChallenge(authzState, AcmeProtocol.CHALLENGE_TYPE_LEGACY_HTTP, true);

                    _testAuthzChallengeLegacyHttpRefresh_AcmeAuthzFile = $"{_baseLocalStore}\\TestAuthz-LegacyHttpChallengeRefreshed.acmeAuthz";
                    using (var fs = new FileStream(_testAuthzChallengeLegacyHttpRefresh_AcmeAuthzFile, FileMode.Create))
                    {
                        authzState.Save(fs);
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0111_RefreshAuthzHttpChallenge()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream(_testRegister_AcmeRegFile, FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = BuildClient(testTagHeader: nameof(Test0111_RefreshAuthzHttpChallenge)))
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    AuthorizationState authzState;
                    using (var fs = new FileStream(_testAuthz_AcmeAuthzFile, FileMode.Open))
                    {
                        authzState = AuthorizationState.Load(fs);
                    }

                    client.RefreshAuthorizeChallenge(authzState, AcmeProtocol.CHALLENGE_TYPE_LEGACY_HTTP, true);

                    _testAuthzChallengeHttpRefresh_AcmeAuthzFile = $"{_baseLocalStore}\\TestAuthz-HttpChallengeRefreshed.acmeAuthz";
                    using (var fs = new FileStream(_testAuthzChallengeHttpRefresh_AcmeAuthzFile, FileMode.Create))
                    {
                        authzState.Save(fs);
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0120_GenerateChallengeAnswers()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream(_testRegister_AcmeRegFile, FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = BuildClient(testTagHeader: nameof(Test0120_GenerateChallengeAnswers)))
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    AuthorizationState authzState;
                    using (var fs = new FileStream(_testAuthz_AcmeAuthzFile, FileMode.Open))
                    {
                        authzState = AuthorizationState.Load(fs);
                    }

                    //client.GenerateAuthorizeChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_DNS);
                    client.GenerateAuthorizeChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_LEGACY_HTTP);
                    client.GenerateAuthorizeChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_HTTP);

                    _testAuthzChallengeAnswers_AcmeAuthzFile = $"{_baseLocalStore}\\TestAuthz-ChallengeAnswers.acmeAuthz";
                    using (var fs = new FileStream(_testAuthzChallengeAnswers_AcmeAuthzFile, FileMode.Create))
                    {
                        authzState.Save(fs);
                    }
                }
            }
        }

        //[TestMethod]
        //[TestCategory("skipCI")]
        //[Timeout(120 * 1000)]
        //public void Test0130_HandleDnsChallenge()
        //{
        //    using (var signer = new RS256Signer())
        //    {
        //        signer.Init();
        //        using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
        //        {
        //            signer.Load(fs);
        //        }
        //
        //        AcmeRegistration reg;
        //        using (var fs = new FileStream(_testRegister_AcmeRegFile, FileMode.Open))
        //        {
        //            reg = AcmeRegistration.Load(fs);
        //        }
        //
        //        using (var client = BuildClient(testTagHeader: nameof(Test0130_HandleDnsChallenge)))
        //        {
        //            client.RootUrl = _rootUrl;
        //            client.Signer = signer;
        //            client.Registration = reg;
        //            client.Init();
        //
        //            client.GetDirectory(true);
        //
        //            AuthorizationState authzState;
        //            using (var fs = new FileStream(_testAuthz_AcmeAuthzFile, FileMode.Open))
        //            {
        //                authzState = AuthorizationState.Load(fs);
        //            }
        //
        //            var authzChallenge = client.GenerateAuthorizeChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_DNS);
        //            _testAuthzChallengeDnsHandled_AcmeAuthzFile = $"{_baseLocalStore}\\TestAuthz-ChallengeAnswersHandleDns.acmeAuthz";
        //            using (var fs = new FileStream(_testAuthzChallengeDnsHandled_AcmeAuthzFile, FileMode.Create))
        //            {
        //                authzState.Save(fs);
        //            }
        //
        //            var dnsName = authzChallenge.ChallengeAnswer.Key;
        //            var dnsValue = Regex.Replace(authzChallenge.ChallengeAnswer.Value, "\\s", "");
        //            var dnsValues = Regex.Replace(dnsValue, "(.{100,100})", "$1\n").Split('\n');
        //
        //            var dnsInfo = DnsInfo.Load(File.ReadAllText("dnsInfo.json"));
        //            dnsInfo.Provider.EditTxtRecord(dnsName, dnsValues);
        //        }
        //    }
        //
        //    Thread.Sleep(90 * 1000);
        //}

        //[TestMethod]
        //[TestCategory("skipCI")]
        //public void Test0135_SubmitDnsChallengeAnswers()
        //{
        //    using (var signer = new RS256Signer())
        //    {
        //        signer.Init();
        //        using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
        //        {
        //            signer.Load(fs);
        //        }
        //
        //        AcmeRegistration reg;
        //        using (var fs = new FileStream(_testRegister_AcmeRegFile, FileMode.Open))
        //        {
        //            reg = AcmeRegistration.Load(fs);
        //        }
        //
        //        using (var client = BuildClient(testTagHeader: nameof(Test0135_SubmitDnsChallengeAnswers)))
        //        {
        //            client.RootUrl = _rootUrl;
        //            client.Signer = signer;
        //            client.Registration = reg;
        //            client.Init();
        //
        //            client.GetDirectory(true);
        //
        //            AuthorizationState authzState;
        //            using (var fs = new FileStream(_testAuthzChallengeDnsHandled_AcmeAuthzFile, FileMode.Open))
        //            {
        //                authzState = AuthorizationState.Load(fs);
        //            }
        //
        //            client.GenerateAuthorizeChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_DNS);
        //            client.SubmitAuthorizeChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_DNS, true);
        //
        //            _testAuthzChallengeDnsAnswered_AcmeAuthzFile = $"{_baseLocalStore}\\TestAuthz-DnsChallengeAnswered.acmeAuthz";
        //            using (var fs = new FileStream(_testAuthzChallengeDnsAnswered_AcmeAuthzFile, FileMode.Create))
        //            {
        //                authzState.Save(fs);
        //            }
        //        }
        //    }
        //}

        //[TestMethod]
        //[TestCategory("skipCI")]
        //public void Test0137_RefreshAuthzDnsChallenge()
        //{
        //    Test0100_RefreshAuthzDnsChallenge();
        //}

        [TestMethod]
        [TestCategory("skipCI")]
        [Timeout(120 * 1000)]
        public void Test0140_HandleLegacyHttpChallenge()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream(_testRegister_AcmeRegFile, FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = BuildClient(testTagHeader: nameof(Test0140_HandleLegacyHttpChallenge)))
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    AuthorizationState authzState;
                    using (var fs = new FileStream(_testAuthz_AcmeAuthzFile, FileMode.Open))
                    {
                        authzState = AuthorizationState.Load(fs);
                    }

                    var authzChallenge = client.GenerateAuthorizeChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_LEGACY_HTTP);
                    _testAuthzChallengeLegacyHttpHandled_AcmeAuthzFile = $"{_baseLocalStore}\\TestAuthz-ChallengeAnswersHandleLegacyHttp.acmeAuthz";
                    using (var fs = new FileStream(_testAuthzChallengeLegacyHttpHandled_AcmeAuthzFile, FileMode.Create))
                    {
                        authzState.Save(fs);
                    }

                    var wsFilePath = authzChallenge.ChallengeAnswer.Key;
                    var wsFileBody = authzChallenge.ChallengeAnswer.Value;

                    var wsInfo = WebServerInfo.Load(File.ReadAllText("webServerInfo.json"));
                    using (var s = new MemoryStream(Encoding.UTF8.GetBytes(wsFileBody)))
                    {
                        var fileUrl = new Uri($"http://{authzState.Identifier}/{wsFilePath}");
                        wsInfo.Provider.UploadFile(fileUrl, s);
                    }
                }
            }

            Thread.Sleep(90 * 1000);
        }

        [TestMethod]
        [TestCategory("skipCI")]
        [Timeout(120 * 1000)]
        public void Test0141_HandleHttpChallenge()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream(_testRegister_AcmeRegFile, FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = BuildClient(testTagHeader: nameof(Test0141_HandleHttpChallenge)))
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    AuthorizationState authzState;
                    using (var fs = new FileStream(_testAuthz_AcmeAuthzFile, FileMode.Open))
                    {
                        authzState = AuthorizationState.Load(fs);
                    }

                    var authzChallenge = client.GenerateAuthorizeChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_HTTP);
                    _testAuthzChallengeHttpHandled_AcmeAuthzFile = $"{_baseLocalStore}\\TestAuthz-ChallengeAnswersHandleHttp.acmeAuthz";
                    using (var fs = new FileStream(_testAuthzChallengeHttpHandled_AcmeAuthzFile, FileMode.Create))
                    {
                        authzState.Save(fs);
                    }

                    var wsFilePath = authzChallenge.ChallengeAnswer.Key;
                    var wsFileBody = authzChallenge.ChallengeAnswer.Value;

                    var wsInfo = WebServerInfo.Load(File.ReadAllText("webServerInfo.json"));
                    using (var s = new MemoryStream(Encoding.UTF8.GetBytes(wsFileBody)))
                    {
                        var fileUrl = new Uri($"http://{authzState.Identifier}/{wsFilePath}");
                        wsInfo.Provider.UploadFile(fileUrl, s);
                    }
                }
            }

            Thread.Sleep(90 * 1000);
        }

        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0145_SubmitLegacyHttpChallengeAnswers()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream(_testRegister_AcmeRegFile, FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = BuildClient(testTagHeader: nameof(Test0145_SubmitLegacyHttpChallengeAnswers)))
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    AuthorizationState authzState;
                    using (var fs = new FileStream(_testAuthzChallengeLegacyHttpHandled_AcmeAuthzFile, FileMode.Open))
                    {
                        authzState = AuthorizationState.Load(fs);
                    }

                    client.GenerateAuthorizeChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_LEGACY_HTTP);
                    client.SubmitAuthorizeChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_LEGACY_HTTP, true);

                    _testAuthzChallengeLegacyHttpAnswered_AcmeAuthzFile = $"{_baseLocalStore}\\TestAuthz-LegacyHttpChallengeAnswered.acmeAuthz";
                    using (var fs = new FileStream(_testAuthzChallengeLegacyHttpAnswered_AcmeAuthzFile, FileMode.Create))
                    {
                        authzState.Save(fs);
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0146_SubmitHttpChallengeAnswers()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream(_testRegister_AcmeRegFile, FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = BuildClient(testTagHeader: nameof(Test0146_SubmitHttpChallengeAnswers)))
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    AuthorizationState authzState;
                    using (var fs = new FileStream(_testAuthzChallengeHttpHandled_AcmeAuthzFile, FileMode.Open))
                    {
                        authzState = AuthorizationState.Load(fs);
                    }

                    client.GenerateAuthorizeChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_HTTP);
                    client.SubmitAuthorizeChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_HTTP, true);

                    _testAuthzChallengeHttpAnswered_AcmeAuthzFile = $"{_baseLocalStore}\\TestAuthz-HttpChallengeAnswered.acmeAuthz";
                    using (var fs = new FileStream(_testAuthzChallengeHttpAnswered_AcmeAuthzFile, FileMode.Create))
                    {
                        authzState.Save(fs);
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0147_RefreshAuthzLegacyHttpChallenge()
        {
            Test0110_RefreshAuthzLegacyHttpChallenge();
        }

        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0148_RefreshAuthzHttpChallenge()
        {
            Test0111_RefreshAuthzHttpChallenge();
        }

        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0160_RequestCertificateInvalidCsr()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream(_testRegister_AcmeRegFile, FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = BuildClient(testTagHeader: nameof(Test0160_RequestCertificateInvalidCsr)))
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    try
                    {
                        client.RequestCertificate("FOOBARNON");
                        Assert.Fail("WebException expected");
                    }
                    catch (AcmeClient.AcmeWebException ex)
                    {
                        Assert.IsNotNull(ex.WebException);
                        Assert.IsNotNull(ex.Response);
                        Assert.AreEqual(HttpStatusCode.BadRequest, ex.Response.StatusCode);
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0170_GenCsrAndRequestCertificate()
        {
            var cp = CertificateProvider.GetProvider();

            var rsaKeyParams = new RsaPrivateKeyParams();
            var rsaKey = cp.GeneratePrivateKey(rsaKeyParams);

            _testGenCsr_RsaKeysFile = $"{_baseLocalStore}\\TestGenCsr-rsaKeys.txt";
            using (var fs = new FileStream(_testGenCsr_RsaKeysFile, FileMode.Create))
            {
                cp.SavePrivateKey(rsaKey, fs);
            }

            var csrParams = new CsrParams
            {
                Details = new CsrDetails
                {
                    CommonName = TEST_CN1
                }
            };

            var csr = cp.GenerateCsr(csrParams, rsaKey, Crt.MessageDigest.SHA256);
            _testGenCsr_CsrDetailsFile = $"{_baseLocalStore}\\TestGenCsr-csrDetails.txt";
            using (var fs = new FileStream(_testGenCsr_CsrDetailsFile, FileMode.Create))
            {
                cp.SaveCsrParams(csrParams, fs);
            }
            _testGenCsr_CsrFile = $"{_baseLocalStore}\\TestGenCsr-csr.txt";
            using (var fs = new FileStream(_testGenCsr_CsrFile, FileMode.Create))
            {
                cp.SaveCsr(csr, fs);
            }

            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream(_testRegister_AcmeRegFile, FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                byte[] derRaw;
                using (var bs = new MemoryStream())
                {
                    cp.ExportCsr(csr, EncodingFormat.DER, bs);
                    derRaw = bs.ToArray();
                }
                var derB64u = JwsHelper.Base64UrlEncode(derRaw);

                using (var client = BuildClient(testTagHeader: nameof(Test0170_GenCsrAndRequestCertificate)))
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    var certRequ = client.RequestCertificate(derB64u);

                    _testCertRequ_AcmeCertRequFile = $"{_baseLocalStore}\\TestCertRequ.acmeCertRequ";
                    using (var fs = new FileStream(_testCertRequ_AcmeCertRequFile, FileMode.Create))
                    {
                        certRequ.Save(fs);
                    }
                }
            }
        }

        /*
        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0180_RequestCertificate()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream(_testRegisterAcmeSigner, FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream(_testRegisterAcmeReg, FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                var csrRaw = File.ReadAllBytes($"{_baseLocalStoreXXX}\\test-csr.der???");
                var csrB64u = JwsHelper.Base64UrlEncode(csrRaw);

                using (var client = BuildClient(testTagHeader: nameof(Test0180_RequestCertificate)))
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    var certRequ = client.RequestCertificate(csrB64u);

                    _testCertRequAcmeCertRequ = $"{_baseLocalStore}\\TestCertRequ.acmeCertRequ"
					using (var fs = new FileStream(_testCertRequAcmeCertRequ, FileMode.Create))
                    {
                        certRequ.Save(fs);
                    }
                }
            }
        }
        */

        [TestMethod]
        [TestCategory("skipCI")]
        public void Test0190_RefreshCertificateRequest()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream(_testRegister_AcmeSignerFile, FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream(_testRegister_AcmeRegFile, FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                //var csrRaw = File.ReadAllBytes($"{_baseLocalStore}\\test-csr.der");
                //var csrB64u = JwsHelper.Base64UrlEncode(csrRaw);

                using (var client = BuildClient(testTagHeader: nameof(Test0190_RefreshCertificateRequest)))
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    CertificateRequest certRequ;
                    using (var fs = new FileStream(_testCertRequ_AcmeCertRequFile, FileMode.Open))
                    {
                        certRequ = CertificateRequest.Load(fs);
                    }

                    client.RefreshCertificateRequest(certRequ, true);

                    _testCertRequRefreshed_AcmeCertRequFile = $"{_baseLocalStore}\\TestCertRequ-Refreshed.acmeCertRequ";
                    using (var fs = new FileStream(_testCertRequRefreshed_AcmeCertRequFile, FileMode.Create))
                    {
                        certRequ.Save(fs);
                    }

                    if (!string.IsNullOrEmpty(certRequ.CertificateContent))
                    {
                        _testCertRequRefreshed_CerFile = $"{_baseLocalStore}\\TestCertRequ-Refreshed.cer";
                        using (var fs = new FileStream(_testCertRequRefreshed_CerFile, FileMode.Create))
                        {
                            certRequ.SaveCertificate(fs);
                        }
                    }
                }
            }
        }


        private class WebProxyConfig
        {
            public const string DEFAULT_HOST_NAME = "localhost";
            public const int DEFAULT_HOST_PORT = 8888;

            public bool UseProxy
            { get; set; }

            public string HostName
            { get; set; } = DEFAULT_HOST_NAME;

            public int HostPort
            { get; set; } = DEFAULT_HOST_PORT;

            public bool AcceptAllServerCerts
            { get; set; }

            public bool UseNewSigner
            { get; set; }

            public static WebProxyConfig Load(string filename)
            {
                using (var fs = new StreamReader(filename))
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<WebProxyConfig>(fs.ReadToEnd());
                }
            }
        }
    }
}
