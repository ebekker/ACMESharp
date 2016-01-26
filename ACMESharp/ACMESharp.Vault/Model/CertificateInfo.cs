using System;

namespace ACMESharp.Vault.Model
{
    public class CertificateInfo : IIdentifiable
    {
        public Guid Id
        { get; set; }

        public string Alias
        { get; set; }

        public string Label
        { get; set; }

        public string Memo
        { get; set; }

        public Guid IdentifierRef
        { get; set; }

        public string IdentifierDns
        { get; set; }

        public string[] AlternativeIdentifierDns
        { get; set; }

        public string KeyPemFile
        { get; set; }

        public string CsrPemFile
        { get; set; }

        public string GenerateDetailsFile
        { get; set; }

        public CertificateRequest CertificateRequest
        { get; set; }

        public string CrtPemFile
        { get; set; }

        public string CrtDerFile
        { get; set; }

        public string IssuerSerialNumber
        { get; set; }

        public string SerialNumber
        { get; set; }

        public string Thumbprint
        { get; set; }

        public string Signature
        { get; set; }

        public string SignatureAlgorithm
        { get; set; }
    }
}
