using ACMESharp.Ext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.PKI
{
    public interface IPkiToolProviderInfo : IAliasesSupported
    {
        string Name
        { get; }

        string Label
        { get; }

        string Description
        { get; }
    }
}
