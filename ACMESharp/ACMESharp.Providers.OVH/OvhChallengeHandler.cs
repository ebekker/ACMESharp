using ACMESharp.ACME;
using Ovh.Api;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ACMESharp.Providers.OVH
{
    public class OvhChallengeHandler : IChallengeHandler
    {
        public string DomainName { get; set; }

        public string Endpoint { get; set; }

        public string ApplicationKey { get; set; }

        public string ApplicationSecret { get; set; }

        public string ConsumerKey { get; set; }

        public static string GetCreateApiUrl(string endpoint)
        {
            var client = new Client(endpoint: endpoint);

            return client.Endpoint.Replace("/1.0/", "/createApp/");
        }

        public static CredentialRequestResult RequestConsumerKey(string endpoint, string applicationKey, string applicationSecret,
            List<AccessRight> accessRules, string redirection)
        {
            Client client = new Client(endpoint: endpoint, applicationKey: applicationKey, applicationSecret: applicationSecret);

            if (accessRules == null)
            {
                accessRules = new List<AccessRight> {
                    new AccessRight("GET", "/domain/zone/*" ),
                    new AccessRight("POST", "/domain/zone/*" ),
                    new AccessRight("PUT", "/domain/zone/*" ),
                    new AccessRight("DELETE", "/domain/zone/*" )
                };
            }

            CredentialRequest requestPayload = new CredentialRequest(accessRules, redirection);

            return client.RequestConsumerKey(requestPayload);
        }

        public void Handle(Challenge c)
        {
            AssertNotDisposed();
            DnsChallenge challenge = (DnsChallenge) c;
            var helper = new OvhHelper(Endpoint, ApplicationKey, ApplicationSecret, ConsumerKey);
            helper.AddOrUpdateDnsRecord(challenge.RecordName, GetCleanedRecordValue(challenge.RecordValue));
        }

        public void CleanUp(Challenge c)
        {
            AssertNotDisposed();
            DnsChallenge challenge = (DnsChallenge) c;
            var helper = new OvhHelper(Endpoint, ApplicationKey, ApplicationSecret, ConsumerKey);
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
                throw new InvalidOperationException("OVH Challenge Handler is disposed");
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        public bool IsDisposed { get; private set; }

    }
}
