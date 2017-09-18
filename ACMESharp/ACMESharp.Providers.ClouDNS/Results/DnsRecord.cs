namespace ACMESharp.Providers.ClouDNS
{
class DnsRecord
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Host { get; set; }
        public string Record { get; set; }
        public string Ttl { get; set; }
        public string Status { get; set; }
    }}