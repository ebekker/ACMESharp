using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.PKI
{

    public abstract class PrivateKeyParams
    { }

    public abstract class PrivateKey
    { }

    /// <summary>
    /// Defines the parameters that may be provided as input to generate
    /// an <see cref="RsaPrivateKey"/>.
    /// </summary>
    public class RsaPrivateKeyParams : PrivateKeyParams
    {
        public delegate int RsaKeyGeneratorCallback(int p, int n, object cbArg);

        /// <summary>
        /// The number of bits in the generated key. If not specified 1024 is used.
        /// </summary>
        public int NumBits
        { get; set; }

        /// <summary>
        /// The RSA public exponent value. This can be a large decimal or hexadecimal value
        /// if preceded by 0x.  Default value is 65537.
        /// </summary>
        public string PubExp
        { get; set; }

        public RsaKeyGeneratorCallback Callback
        { get; set; }

        public object CallbackArg
        { get; set; }
    }

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

    /// <summary>
    /// Defines the parameters that may be provided as input to generate
    /// an <see cref="EcKeyPair"/>.
    /// </summary>
    public class EcPrivateKeyParams : PrivateKeyParams
    {
        /// <summary>
        /// The EC curve to use, using NIST curve names such as "P-256".
        /// </summary>
        public string CurveName
        { get; set; } = "P-256";

        public bool NamedCurveEncoding
        { get; set; }
    }

    public class EcKeyPair : PrivateKey
    { }
}
