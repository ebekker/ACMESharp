using ACMESharp.Installer;
using ACMESharp.Util;
using ACMESharp.Vault.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsCommon.Get, "InstallerProfile", DefaultParameterSetName = PSET_GET_INSTALLER_PROFILE)]
    public class GetInstallerProfile : Cmdlet
    {
        public const string PSET_GET_INSTALLER = "GetInstaller";
        public const string PSET_LIST_INSTALLER_PROFILES = "ListInstallerProfiles";
        public const string PSET_GET_INSTALLER_PROFILE = "GetInstallerProfile";
        public const string PSET_RELOAD_PROVIDERS = "ReloadProviders";

        [Parameter(ParameterSetName = PSET_GET_INSTALLER)]
        public SwitchParameter ListInstallers
        { get; set; }

        [Parameter(ParameterSetName = PSET_GET_INSTALLER)]
        public string GetInstaller
        { get; set; }

        [Parameter(ParameterSetName = PSET_GET_INSTALLER)]
        public SwitchParameter ParametersOnly
        { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = PSET_LIST_INSTALLER_PROFILES)]
        public SwitchParameter ListProfiles
        { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = PSET_GET_INSTALLER_PROFILE)]
        public string ProfileRef
        { get; set; }

        [Parameter(ParameterSetName = PSET_LIST_INSTALLER_PROFILES)]
        [Parameter(ParameterSetName = PSET_GET_INSTALLER_PROFILE)]
        public string VaultProfile
        { get; set; }

        [Parameter(ParameterSetName = PSET_RELOAD_PROVIDERS)]
        public SwitchParameter ReloadProviders
        { get; set; }

        protected override void ProcessRecord()
        {
            // We have to invoke this here because we *may not* invoke
            // any Vault access but we do rely on Ext mechanism access.
            Util.PoshHelper.BeforeExtAccess();

            if (ReloadProviders)
            {
                InstallerExtManager.Reload();
            }
            else if (!string.IsNullOrEmpty(GetInstaller))
            {
                WriteVerbose("Getting details of Installer");
                var pInfo = InstallerExtManager.GetProviderInfos()
                        .FirstOrDefault(_ => _.Name == GetInstaller);
                var p = InstallerExtManager.GetProvider(GetInstaller);
                if (ParametersOnly)
                {
                    WriteVerbose("Showing parameter details only");
                    WriteObject(p.DescribeParameters().Select(_ => new {
                        _.Name,
                        _.Label,
                        _.Type,
                        _.IsRequired,
                        _.IsMultiValued,
                        _.Description,
                    }), true);
                }
                else
                {
                    WriteObject(new
                    {
                        pInfo.Name,
                        pInfo.Info.Label,
                        pInfo.Info.IsUninstallSupported,
                        pInfo.Info.Description,
                        Parameters = p.DescribeParameters().Select(_ => new {
                            _.Name,
                            _.Label,
                            _.Type,
                            _.IsRequired,
                            _.IsMultiValued,
                            _.Description,
                        }),
                    });
                }
            }
            else if (ListInstallers)
            {
                WriteVerbose("Listing all Installers");
                WriteObject(InstallerExtManager.GetProviderInfos().Select(_ => _.Name), true);
            }
            else
            {
                WriteVerbose("Getting details of preconfigured Installer Profile");
                using (var vlt = Util.VaultHelper.GetVault(VaultProfile))
                {
                    vlt.OpenStorage();
                    var v = vlt.LoadVault();

                    if (ListProfiles)
                    {
                        WriteObject(v.InstallerProfiles?.Values, true);
                    }
                    else
                    {
                        var ipi = v.InstallerProfiles?.GetByRef(ProfileRef, throwOnMissing: false);
                        if (ipi == null)
                        {
                            WriteObject(ipi);
                        }
                        else
                        {
                            var asset = vlt.GetAsset(Vault.VaultAssetType.InstallerConfigInfo,
                                    ipi.Id.ToString());
                            using (var s = vlt.LoadAsset(asset))
                            {
                                WriteObject(JsonHelper.Load<InstallerProfile>(s), false);
                            }
                        }
                    }
                }
            }
        }
    }
}
