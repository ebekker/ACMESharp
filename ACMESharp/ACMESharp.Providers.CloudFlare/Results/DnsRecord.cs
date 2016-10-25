namespace ACMESharp.Providers.CloudFlare
{
    internal class DnsRecord
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
    }
}