using ACMESharp.ACME;
using System;
using System.Text.RegularExpressions;

namespace ACMESharp.Providers.ClouDNS
{
    public class ClouDNSChallengeHandler : IChallengeHandler
    {
        public string AuthPassword { get; set; }
        public string AuthId { get; set; }
        public string DomainName { get; set; }
        public void Handle(Challenge c)
        {
            AssertNotDisposed();
            DnsChallenge challenge = (DnsChallenge)c;
            var helper = new ClouDNSHelper(AuthId, AuthPassword, DomainName);
            helper.AddOrUpdateDnsRecord(challenge.RecordName, GetCleanedRecordValue(challenge.RecordValue));
        }

        public void CleanUp(Challenge c)
        {
            AssertNotDisposed();
            DnsChallenge challenge = (DnsChallenge)c;
            var helper = new ClouDNSHelper(AuthId, AuthPassword, DomainName);
            helper.DeleteDnsRecord(challenge.RecordName);
        }

        private string GetCleanedRecordValue(string recordValue)
        {
            var dnsValue = Regex.Replace(recordValue, "\\s", "");
            var dnsValues = string.Join("\" \"", Regex.Replace(dnsValue, "(.{100,100})", "$1\n").Split('\n'));
            return dnsValues;
        }

        private void AssertNotDisposed()
        {
            if (IsDisposed)
                throw new InvalidOperationException("ClouDNS Challenge Handler is disposed");
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        public bool IsDisposed { get; private set; }
    }
}