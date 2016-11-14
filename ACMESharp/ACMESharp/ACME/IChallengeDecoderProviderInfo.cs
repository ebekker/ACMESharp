using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.ACME
{
    public interface IChallengeDecoderProviderInfo
    {
        string Type
        { get; }

        ChallengeTypeKind SupportedType
        { get; }

        string Label
        { get; }

        string Description
        { get; }
    }
}
