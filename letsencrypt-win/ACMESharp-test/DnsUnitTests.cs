using LetsEncrypt.ACME.DNS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace LetsEncrypt.ACME
{
    [TestClass]
    public class DnsUnitTests
    {
        [TestMethod]
        public void TestUpdateDnsTxt()
        {
            var dnsInfo = DnsInfo.Load(File.ReadAllText("dnsInfo.json"));

            dnsInfo.Provider.EditTxtRecord(
                    $"_acme-challenge.foo1.{dnsInfo.DefaultDomain}",
                    new string[] { $"{Environment.UserName}@{Environment.MachineName}@{DateTime.Now}" });
            dnsInfo.Provider.EditTxtRecord(
                    $"_acme-challenge.foo2.{dnsInfo.DefaultDomain}",
                    new string[] { Environment.UserName, Environment.MachineName, DateTime.Now.ToString() });
        }
    }
}
