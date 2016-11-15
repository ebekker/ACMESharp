using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Ext;

namespace ACMESharp.PKI.Providers
{
    [PkiToolProvider(OpenSslCliProvider.PROVIDER_NAME,
        Label = "OpenSSL CLI",
        Description = "Provider for a PKI Tool that uses process calls"
                + " out to the command-line interface (CLI) OpenSSL"
                + " executable (openssl.exe).")]
    public class OSSLCliPkiToolProvider : IPkiToolProvider
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
            return new OSSLCliPkiTool(newParams);
        }
    }
}
