using OpenSSL.Core;
using OpenSSL.Crypto;
using OpenSSL.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.PKI
{

    public class Crt
    {
        public string Pem
        { get; set; }

        public enum MessageDigest
        {
            SHA256
        }
    }
}
