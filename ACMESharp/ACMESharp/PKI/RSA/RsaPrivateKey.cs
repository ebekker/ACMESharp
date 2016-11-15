using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.PKI.RSA
{
    public class RsaPrivateKey : PrivateKey
    {
        public RsaPrivateKey(int bits, string e, string pem)
        {
            Bits = bits;
            E = e;
            Pem = pem;
        }

        public int Bits
        { get; private set; }

        public string E
        { get; private set; }

        public object BigNumber
        { get; set; }

        public string Pem
        { get; private set; }
    }
}
