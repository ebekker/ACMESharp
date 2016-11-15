using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Ext;

namespace ACMESharp.PKI.Providers
{
    [PkiToolProvider(OpenSslLibProvider.PROVIDER_NAME,
        Label = "OpenSSL Library",
        Description = "Provider for a PKI Tool that is based on the .NET"
                + " port of OpenSSL library; you must also include at"
                + " least one of the x86 or x64 companion libraries"
                + " depending on your execution environment.")]
    public class OSSLLibPkiToolProvider : IPkiToolProvider
    {
        public IEnumerable<ParameterDetail> DescribeParameters()
        {
            return new ParameterDetail[0];
        }

        public IPkiTool GetPkiTool(IReadOnlyDictionary<string, object> initParams)
        {
            var kvPairs = initParams.Select(_ =>
                    new KeyValuePair<string, string>(_.Key, _.Value.ToString()));
            var newParams = kvPairs.ToDictionary(
                    _ => _.Key,
                    _ => _.Value);
            return new OSSLLibPkiTool(newParams);
        }
    }
}
