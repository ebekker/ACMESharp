using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Ext;

namespace ACMESharp.PKI.Providers
{
    [PkiToolProvider(BouncyCastleProvider.PROVIDER_NAME,
            Aliases = new[] { "BC" },
            Label = "Bouncy Castle PKI Tool",
            Description = "PKI Tooling implemented using the Bouncy Castle library.")]
    public class BCPkiToolProvider : IPkiToolProvider
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
            return new BCPkiTool(newParams);
        }
    }
}
