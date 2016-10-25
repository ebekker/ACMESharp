using ACMESharp.ACME;
using System;
using System.Text.RegularExpressions;

namespace ACMESharp.Providers.CloudFlare
{
    public class CloudFlareChallengeHandler : IChallengeHandler
    {
        public string AuthKey { get; set; }
        public string EmailAddress { get; set; }
        public string DomainName { get; set; }
        public void Handle(Challenge c)
        {
            AssertNotDisposed();
            DnsChallenge challenge = (DnsChallenge)c;
            var helper = new CloudFlareHelper(AuthKey, EmailAddress, DomainName);
            helper.AddOrUpdateDnsRecord(challenge.RecordName, GetCleanedRecordValue(challenge.RecordValue));
        }

        public void CleanUp(Challenge c)
        {
            AssertNotDisposed();
            DnsChallenge challenge = (DnsChallenge)c;
            var helper = new CloudFlareHelper(AuthKey, EmailAddress, DomainName);
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
                throw new InvalidOperationException("AWS Challenge Handler is disposed");
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        public bool IsDisposed { get; private set; }
    }
}