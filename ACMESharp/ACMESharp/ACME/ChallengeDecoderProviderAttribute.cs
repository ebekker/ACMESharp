using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.ACME
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ChallengeDecoderProviderAttribute : ExportAttribute
    {
        public ChallengeDecoderProviderAttribute(string type,
                ChallengeTypeKind supportedType) : base(typeof(IChallengeDecoderProvider))
        {
            Type = type;
            SupportedType = supportedType;
        }

        public ChallengeTypeKind SupportedType
        { get; private set; }

        public string Type
        { get; private set; }

        public string Label
        { get; set; }

        public string Description
        { get; set; }
    }
}
