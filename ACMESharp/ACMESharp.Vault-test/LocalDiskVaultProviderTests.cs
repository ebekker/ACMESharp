using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACMESharp.Vault
{
    [TestClass]
    public class LocalDiskVaultProviderTests
    {
        [TestMethod]
        public void TestProviderNamesCount()
        {
            var all = VaultExtManager.GetProviderInfos();

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
