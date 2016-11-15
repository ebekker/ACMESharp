using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.PKI.RSA
{
    /// <summary>
    /// Defines the parameters that may be provided as input to generate
    /// an <see cref="RsaPrivateKey"/>.
    /// </summary>
    public class RsaPrivateKeyParams : PrivateKeyParams
    {
        public delegate int RsaKeyGeneratorCallback(int p, int n, object cbArg);

        /// <summary>
        /// The number of bits in the generated key. If not specified 2048 is used.
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
}
