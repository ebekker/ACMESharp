namespace LetsEncrypt.ACME.CLI
{
    internal class SiteHost
    {
        public string Host { get; set; }
        public string PhysicalPath { get; set; }

        public override string ToString() => $"{Host} ({PhysicalPath})";
    }
}