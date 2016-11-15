using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.PKI.Providers
{
    public class OSSLLibPkiTool : OpenSslLibProvider, IPkiTool
    {
        public OSSLLibPkiTool(IReadOnlyDictionary<string, string> initParams)
            : base(initParams)
        { }
    }
}
