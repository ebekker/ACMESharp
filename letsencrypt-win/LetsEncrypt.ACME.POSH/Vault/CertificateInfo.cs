using LetsEncrypt.ACME.POSH.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH.Vault
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
    }
}
