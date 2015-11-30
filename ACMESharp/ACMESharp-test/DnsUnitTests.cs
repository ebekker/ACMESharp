using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using ACMESharp.DNS;

namespace ACMESharp
{
    [TestClass]
    public class DnsUnitTests
    {
        [TestMethod]
        public void TestUpdateDnsTxt()
        {
            var dnsInfo = DnsInfo.Load(File.ReadAllText("config\\dnsInfo.json"));

            dnsInfo.Provider.EditTxtRecord(
                    $"_acme-challenge.foo1.{dnsInfo.DefaultDomain}",
                    new string[] { $"{Environment.UserName}@{Environment.MachineName}@{DateTime.Now}" });
            dnsInfo.Provider.EditTxtRecord(
                    $"_acme-challenge.foo2.{dnsInfo.DefaultDomain}",
                    new string[] { Environment.UserName, Environment.MachineName, DateTime.Now.ToString() });
        }
    }
}
