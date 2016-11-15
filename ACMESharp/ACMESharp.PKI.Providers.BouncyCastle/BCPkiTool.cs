using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.PKI.Providers
{
    public class BCPkiTool : BouncyCastleProvider, IPkiTool
    {
        public BCPkiTool(IReadOnlyDictionary<string, string> newParams)
            : base(newParams)
        { }
    }
}
