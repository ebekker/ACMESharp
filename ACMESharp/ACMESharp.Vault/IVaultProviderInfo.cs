using ACMESharp.Ext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Vault
{
    public interface IVaultProviderInfo : IAliasesSupported
    {
        string Name
        { get; }

        string Label
        { get; }

        string Description
        { get; }
    }
}
