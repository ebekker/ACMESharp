namespace ACMESharp.Vault.Model
{
    public class IssuerCertificateInfo
    {
        public string SerialNumber
        { get; set; }

        public string Thumbprint
        { get; set; }

        public string Signature
        { get; set; }

        public string SignatureAlgorithm
        { get; set; }

        public string CrtPemFile
        { get; set; }

        public string CrtDerFile
        { get; set; }
    }
}
