using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.PKI.EC
{
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
}
