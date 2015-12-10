using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACMESharp.Vault
{
    [TestClass]
    public class TestLocalDiskVaultProvider
    {
        [TestMethod]
        public void TestProviderNamesCount()
        {
            var all = VaultExtManager.GetProviders();

            // Make sure at least the local disk and the default
            Assert.IsTrue(all.Count() > 0);
        }

        [TestMethod]
        public void TestDefaultProvider()
        {
            var p = VaultExtManager.GetProvider();
        }
    }
}
