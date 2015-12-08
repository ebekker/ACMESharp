using System;

namespace ACMESharp.Vault.Model
{
    public class ProviderConfig : IIdentifiable
    {
        public Guid Id
        { get; set; }

        public string Alias
        { get; set; }

        public string Label
        { get; set; }

        public string Memo
        { get; set; }

        public string DnsProvider
        { get; set; }

        public string WebServerProvider
        { get; set; }
    }
}
