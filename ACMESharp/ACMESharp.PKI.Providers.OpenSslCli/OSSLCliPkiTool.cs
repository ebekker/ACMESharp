using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.PKI.Providers
{
    public class OSSLCliPkiTool : OpenSslCliProvider, IPkiTool
    {
        public OSSLCliPkiTool(IReadOnlyDictionary<string, string> initParams)
            : base(initParams)
        { }
    }
}
