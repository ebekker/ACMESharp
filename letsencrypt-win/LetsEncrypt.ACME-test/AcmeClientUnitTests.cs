using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LetsEncrypt.ACME.JOSE;
using System.IO;
using System.Net;
using System.Collections.Generic;

namespace LetsEncrypt.ACME
{
    [TestClass]
    public class AcmeClientUnitTests
    {
        Uri _rootUrl = new Uri("http://acme2.aws3.ezshield.ws:4000/");

        [TestMethod]
        public void TestInit()
        {
            using (var signer = new RS256Signer())
            {
                using (var client = new AcmeClient(_rootUrl, signer: signer))
                {
                    client.Init();

                    Assert.IsNotNull(client.Directory);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(client.NextNonce));
                }
            }
        }

        [TestMethod]
        public void TestGetDirectory()
        {
            var boulderUrlBase = "http://localhost:4000";
            var boulderResMap = new Dictionary<string, string>
            {
                ["new-authz"] = "http://localhost:4000/acme/new-authz",
                ["new-cert"] = "http://localhost:4000/acme/new-cert",
                ["new-reg"] = "http://localhost:4000/acme/new-reg",
                ["revoke-cert"] = "http://localhost:4000/acme/revoke-cert",
            };

            using (var signer = new RS256Signer())
            {
                using (var client = new AcmeClient(_rootUrl, signer: signer))
                {
                    client.Init();

                    // Test absolute URI paths
                    var acmeDir = client.GetDirectory(false);
                    foreach (var ent in boulderResMap)
                    {
                        Assert.IsTrue(acmeDir.Contains(ent.Key));
                        Assert.AreEqual(ent.Value, acmeDir[ent.Key]);
                    }

                    // Test relative URI paths
                    acmeDir = client.GetDirectory(true);
                    foreach (var ent in boulderResMap)
                    {
                        var relUrl = ent.Value.Replace(boulderUrlBase, "");
                        Assert.IsTrue(acmeDir.Contains(ent.Key));
                        Assert.AreEqual(relUrl, acmeDir[ent.Key]);
                    }
                }
            }
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

                    client.GetDirectory(true);

                    client.Register(new string[]
                            {
                                "mailto:letsencrypt@mailinator.com",
                                "tel:+14109361212",
                            });

                    Assert.IsNotNull(client.Registration);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(client.Registration.RegistrationUri));

                    using (var fs = new FileStream("..\\TestRegister.acmeReg", FileMode.Create))
                    {
                        client.Registration.Save(fs);
                    }
                }
                using (var fs = new FileStream("..\\TestRegister.acmeSigner", FileMode.Create))
                {
                    signer.Save(fs);
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
        public void TestRegisterEmptyUpdate()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream("..\\TestRegister.acmeSigner", FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream("..\\TestRegister.acmeReg", FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = new AcmeClient())
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

                    using (var fs = new FileStream("..\\TestRegisterUpdate.acmeReg", FileMode.Create))
                    {
                        client.Registration.Save(fs);
                    }
                }
            }
        }

        [TestMethod]
        public void TestRegisterUpdateTosAgreement()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream("..\\TestRegister.acmeSigner", FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream("..\\TestRegister.acmeReg", FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = new AcmeClient())
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    client.UpdateRegistration(true, true);

                    Assert.IsNotNull(client.Registration);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(client.Registration.RegistrationUri));

                    using (var fs = new FileStream("..\\TestRegisterUpdate.acmeReg", FileMode.Create))
                    {
                        client.Registration.Save(fs);
                    }
                }
            }
        }

        [TestMethod]
        public void TestRegisterUpdateContacts()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream("..\\TestRegister.acmeSigner", FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream("..\\TestRegister.acmeReg", FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = new AcmeClient())
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    client.UpdateRegistration(true, contacts: new string[]
                            {
                                "mailto:letsencrypt+update@mailinator.com",
                            });

                    Assert.IsNotNull(client.Registration);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(client.Registration.RegistrationUri));

                    using (var fs = new FileStream("..\\TestRegisterUpdate.acmeReg", FileMode.Create))
                    {
                        client.Registration.Save(fs);
                    }
                }
            }
        }

        [TestMethod]
        public void TestRegisterDuplicate()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream("..\\TestRegisterDuplicate.acmeSigner", FileMode.Open))
                {
                    signer.Load(fs);
                }

                using (var client = new AcmeClient())
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
        public void TestAuthorizeDnsBlacklisted()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream("..\\TestRegister.acmeSigner", FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream("..\\TestRegister.acmeReg", FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = new AcmeClient())
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    try
                    {
                        client.AuthorizeDns("foo.example.com");
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
        public void TestAuthorizeDns()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream("..\\TestRegister.acmeSigner", FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream("..\\TestRegister.acmeReg", FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = new AcmeClient())
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    var dnsValidation = client.AuthorizeDns("foo.letsencrypt.cc");

                    foreach (var c in dnsValidation.Challenges)
                    {
                        if (c.Type == "dns")
                        {
                            var dnsResponse = c.GenerateDnsChallengeResponse(
                                    dnsValidation.Dns, signer);
                        }
                    }

                    using (var fs = new FileStream("..\\TestAuthzDns.acmeChallenges", FileMode.Create))
                    {
                        dnsValidation.Save(fs);
                    }
                }
            }
        }

        [TestMethod]
        public void TestRefreshDnsChallengeWithDns()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream("..\\TestRegister.acmeSigner", FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream("..\\TestRegister.acmeReg", FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = new AcmeClient())
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    DnsIdentifierValidation dnsValidation;
                    using (var fs = new FileStream("..\\TestAuthzDns.acmeChallenges", FileMode.Open))
                    {
                        dnsValidation = DnsIdentifierValidation.Load(fs);
                    }

                    client.RefreshDnsChallenge(dnsValidation, "dns", true);

                    using (var fs = new FileStream("..\\TestAuthzDnsRefreshed.acmeChallenges", FileMode.Create))
                    {
                        dnsValidation.Save(fs);
                    }
                }
            }
        }

        [TestMethod]
        public void TestValidateDnsChallengeWithDns()
        {
            using (var signer = new RS256Signer())
            {
                signer.Init();
                using (var fs = new FileStream("..\\TestRegister.acmeSigner", FileMode.Open))
                {
                    signer.Load(fs);
                }

                AcmeRegistration reg;
                using (var fs = new FileStream("..\\TestRegister.acmeReg", FileMode.Open))
                {
                    reg = AcmeRegistration.Load(fs);
                }

                using (var client = new AcmeClient())
                {
                    client.RootUrl = _rootUrl;
                    client.Signer = signer;
                    client.Registration = reg;
                    client.Init();

                    client.GetDirectory(true);

                    DnsIdentifierValidation dnsValidation;
                    using (var fs = new FileStream("..\\TestAuthzDns.acmeChallenges", FileMode.Open))
                    {
                        dnsValidation = DnsIdentifierValidation.Load(fs);
                    }

                    client.ValidateDnsChallenge(dnsValidation, "dns", true);
                }
            }
        }
    }
}
