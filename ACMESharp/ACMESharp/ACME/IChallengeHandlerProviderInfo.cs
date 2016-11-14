using ACMESharp.Ext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.ACME
{
    public interface IChallengeHandlerProviderInfo : IAliasesSupported
    {
        string Name
        { get; }

        ChallengeTypeKind SupportedTypes
        { get; }

        string Label
        { get; }

        string Description
        { get; }
    }
}
