using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Installer
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class InstallerProviderAttribute : ExportAttribute
    {
        public InstallerProviderAttribute(string name)
            : base(typeof(IInstallerProvider))
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

        public bool IsUninstallSupported
        { get; set; }
    }
}
