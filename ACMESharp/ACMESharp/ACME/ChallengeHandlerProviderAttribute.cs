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
    public class ChallengeHandlerProviderAttribute : ExportAttribute
    {
        public ChallengeHandlerProviderAttribute(string name,
                ChallengeTypeKind supportedTypes) : base(typeof(IChallengeHandlerProvider))
        {
            Name = name;
            SupportedTypes = supportedTypes;
        }

        public string Name
        { get; private set; }

        public ChallengeTypeKind SupportedTypes
        { get; private set; }

        public string[] Aliases
        { get; set; }

        public string Label
        { get; set; }

        public string Description
        { get; set; }
    }
}
