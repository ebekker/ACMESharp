using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Installer
{
    public interface IInstallerProviderInfo
    {
        string Name
        { get; }

        string[] Aliases
        { get; }

        string Label
        { get; }

        string Description
        { get; }

        bool IsUninstallSupported
        { get; }
    }
}
