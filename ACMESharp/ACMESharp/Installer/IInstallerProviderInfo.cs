using ACMESharp.Ext;

namespace ACMESharp.Installer
{
	public interface IInstallerProviderInfo : IAliasesSupported
    {
        string Name
        { get; }

        string Label
        { get; }

        string Description
        { get; }

        bool IsUninstallSupported
        { get; }
    }
}
