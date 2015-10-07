using LetsEncrypt.ACME.POSH.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH.Vault
{
    public class IssuerCertificateInfo
    {
        public string SerialNumber
        { get; set; }

        public string CrtPemFile
        { get; set; }

        public string CrtDerFile
        { get; set; }
    }
}
