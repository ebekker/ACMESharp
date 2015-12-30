using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ACMESharp.Vault.Profile;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using ACMESharp.Util;

namespace ACMESharp.Vault
{
    [TestClass]
    public class VaultProfileTests
    {
        [TestMethod]
        public void TestGetProfiles()
        {
            var profiles = VaultProfileManager.GetProfileNames();
            Assert.IsNotNull(profiles);
            Assert.IsTrue(profiles.Count() > 0);

            var builtinProfiles = profiles.Where(x => x.StartsWith(":"))
                    .Select(x => VaultProfileManager.GetProfile(x));
            Assert.IsTrue(builtinProfiles.Count() > 0);
        }

        [TestMethod]
        public void TestGetDefaultSysProfile()
        {
            var p = VaultProfileManager.GetProfile(":sys");
            var p1 = VaultProfileManager.GetProfile(":SYS");
            var p2 = VaultProfileManager.GetProfile(":SyS");

            Assert.AreSame(p, p1);
            Assert.AreSame(p, p2);

            Assert.AreEqual("local", p.ProviderName);
            Assert.IsNotNull(p.VaultParameters);

            var pp = p.VaultParameters;
            Assert.IsTrue(pp.Count > 0);
            Assert.IsTrue(pp.ContainsKey("RootPath"));

            var rootPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "ACMESharp", "sysVault");

            Assert.AreEqual(rootPath, pp["RootPath"]);
        }

        [TestMethod]
        public void TestGetDefaultUserProfile()
        {
            var p = VaultProfileManager.GetProfile(":user");
            var p1 = VaultProfileManager.GetProfile(":USER");
            var p2 = VaultProfileManager.GetProfile(":UsEr");

            Assert.AreSame(p, p1);
            Assert.AreSame(p, p2);

            Assert.AreEqual("local", p.ProviderName);
            Assert.IsNotNull(p.VaultParameters);

            var pp = p.VaultParameters;
            Assert.IsTrue(pp.Count > 0);
            Assert.IsTrue(pp.ContainsKey("RootPath"));

            var rootPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ACMESharp", "userVault");

            Assert.AreEqual(rootPath, pp["RootPath"]);
        }

        [TestMethod]
        public void TestResolveProfileName()
        {
            var envVar = "ACMESHARP_VAULT_PROFILE";

            Environment.SetEnvironmentVariable(envVar, "");
            Assert.IsTrue(string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar)));

            var profileName = VaultProfileManager.ResolveProfileName();
            if (SysHelper.IsElevatedAdmin())
                Assert.AreEqual(VaultProfileManager.PROFILE_DEFAULT_SYS_NAME, profileName);
            else
                Assert.AreEqual(VaultProfileManager.PROFILE_DEFAULT_USER_NAME, profileName);

            Environment.SetEnvironmentVariable(envVar, "FooBar");
            Assert.AreEqual("FooBar", Environment.GetEnvironmentVariable(envVar));
            profileName = VaultProfileManager.ResolveProfileName();
            Assert.AreEqual("FooBar", profileName);

            profileName = VaultProfileManager.ResolveProfileName();
            Assert.AreEqual("FooBar", profileName);

            profileName = VaultProfileManager.ResolveProfileName("FooBaz");
            Assert.AreEqual("FooBaz", profileName);

            Environment.SetEnvironmentVariable(envVar, "");
            Assert.IsTrue(string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar)));

            profileName = VaultProfileManager.ResolveProfileName("FooBaz");
            Assert.AreEqual("FooBaz", profileName);
        }

        [TestMethod]
        public void TestSetGetProfile()
        {
            VaultProfileManager.SetProfile("Test1", "local",
                    vaultParams: new Dictionary<string, object>
                    {
                        ["RootPath"] = "zz:\\no\\such\\path"
                    });

            var profiles = VaultProfileManager.GetProfileNames();
            Assert.IsNotNull(profiles);
            Assert.IsTrue(profiles.Count() > 0);
            Assert.IsTrue(profiles.Contains("Test1"));

            var p = VaultProfileManager.GetProfile("Test1");
            Assert.IsNotNull(p);
            Assert.AreEqual("Test1", p.Name);
            Assert.AreEqual("local", p.ProviderName);
            Assert.IsNotNull(p.VaultParameters);
            Assert.IsTrue(p.VaultParameters.Count > 0);
            Assert.IsTrue(p.VaultParameters.ContainsKey("RootPath"));

            VaultProfileManager.RemoveProfile("Test1");
            profiles = VaultProfileManager.GetProfileNames();
            Assert.IsNotNull(profiles);
            Assert.IsFalse(profiles.Contains("Test1"));

            p = VaultProfileManager.GetProfile("Test1");
            Assert.IsNull(p);
        }
    }
}
