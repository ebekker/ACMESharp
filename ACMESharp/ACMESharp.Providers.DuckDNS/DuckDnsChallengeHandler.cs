using ACMESharp.ACME;
using System;
using System.Net;

namespace ACMESharp.Providers.DuckDNS
{
    public class DuckDnsChallengeHandler : IChallengeHandler
    {
        public string Token { get; set; }

        public bool IsDisposed
        {
            get; private set;
        }

        public void CleanUp(ChallengeHandlingContext ctx)
        {
            var dnsChallenge = (DnsChallenge)ctx.Challenge;
            var domain = GetDomainId(dnsChallenge);

            var wr = CreateRequest(Token, domain, "");
            using (var response = wr.GetResponse())
            { }
        }

        private void AssertNotDisposed()
        {
            if (IsDisposed)
                throw new InvalidOperationException("handler is disposed");
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        public void Handle(ChallengeHandlingContext ctx)
        {
            AssertNotDisposed();
            var dnsChallenge = (DnsChallenge)ctx.Challenge;
            var domain = GetDomainId(dnsChallenge);

            var wr = CreateRequest(Token, domain, dnsChallenge.RecordValue);
            using (var response = wr.GetResponse())
            { }
        }

        string GetDomainId(DnsChallenge dnsChallenge)
        {
            var segments = dnsChallenge.RecordName.Split('.');
            return segments[1];
        }

        WebRequest CreateRequest(string token, string domain, string text)
        {
            var url = "https://www.duckdns.org/update?token=" + token + "&domains=" + domain + "&txt=" + text;
            if (String.IsNullOrEmpty(text))
            {
                url += "&clear=true";
            }
            Console.WriteLine("Executing web request: " + url);
            return WebRequest.Create(url);
        }
    }
}
