using ACMESharp.Ext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Vault
{
    public interface IVaultProvider // : IDisposable
    {
        IEnumerable<ParameterDetail> DescribeParameters();

        IVault GetVault(IReadOnlyDictionary<string, object> initParams);
    }
}
