using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.PKI
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PkiToolProviderAttribute : ExportAttribute
    {
        public PkiToolProviderAttribute(string name)
            : base(typeof(IPkiToolProvider))
        {
            Name = name;
        }

        public string Name
        { get; private set; }

        public string[] Aliases
        { get; set; }

        public string Label
        { get; set; }

        public string Description
        { get; set; }
    }
}
