using ACMESharp.Installer;
using ACMESharp.POSH.Util;
using ACMESharp.Util;
using ACMESharp.Vault.Profile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsCommon.Set, "InstallerProfile", DefaultParameterSetName = PSET_SET)]
    public class SetInstallerProfile : Cmdlet
    {
        public const string PSET_SET = "Set";
        public const string PSET_RENAME = "Rename";
        public const string PSET_REMOVE = "Remove";

        [Parameter(Mandatory = true, Position = 0)]
        public string ProfileName
        { get; set; }

        [Parameter(Mandatory = true, Position = 2, ParameterSetName = PSET_SET)]
        public string Installer
        { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = PSET_SET)]
        public string Label
        { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = PSET_SET)]
        public string Memo
        { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = PSET_SET)]
        public Hashtable InstallerParameters
        { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = PSET_RENAME)]
        public string Rename
        { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = PSET_REMOVE)]
        public SwitchParameter Remove
        { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force
        { get; set; }

        [Parameter]
        public string VaultProfile
        { get; set; }

        protected override void ProcessRecord()
        {
            using (var vlt = Util.VaultHelper.GetVault(VaultProfile))
            {
                vlt.OpenStorage();
                var v = vlt.LoadVault();

                if (v.InstallerProfiles == null)
                {
                    WriteVerbose("Initializing Installer Profile collection");
                    v.InstallerProfiles = new Vault.Util.EntityDictionary<Vault.Model.InstallerProfileInfo>();
                }

                WriteVerbose($"Searching for existing Installer Profile for reference [{ProfileName}]");
                var ipi = v.InstallerProfiles.GetByRef(ProfileName, throwOnMissing: false);
                if (ipi == null)
                    WriteVerbose("No existing Profile found");
                else
                    WriteVerbose($"Existing Profile found [{ipi.Id}][{ipi.Alias}]");

                if (!string.IsNullOrEmpty(Rename))
                {
                    if (ipi == null)
                        throw new KeyNotFoundException("no existing profile found that can be renamed");

                    v.InstallerProfiles.Rename(ProfileName, Rename);
                    ipi.Alias = Rename;
                }
                else if (Remove)
                {
                    WriteVerbose($"Removing named Installer Profile for name [{ProfileName}]");
                    if (ipi == null)
                    {
                        WriteVerbose("No Installer Profile found for given name");
                        return;
                    }
                    else
                    {
                        v.InstallerProfiles.Remove(ipi.Id);
                        WriteVerbose("Installer Profile removed");
                    }
                }
                else
                {
                    if (ipi != null)
                    {
                        if (!Force)
                            throw new InvalidOperationException("existing profile found;"
                                    + " specify -Force to overwrite");

                        WriteVerbose("Removing existing Profile");
                        v.InstallerProfiles.Remove(ipi.Id);
                    }

                    if (InstallerExtManager.GetProviderInfo(Installer) == null)
                    {
                        throw new ArgumentException("Unknown or invalid Installer provider name")
                                .With(nameof(Installer), Installer);
                    }

                    WriteVerbose("Adding new Installer Profile Info");
                    ipi = new Vault.Model.InstallerProfileInfo
                    {
                        Id = Guid.NewGuid(),
                        Alias = ProfileName,
                        Label = Label,
                        Memo = Memo,
                    };
                    var pp = new InstallerProfile
                    {
                        InstallerProvider = Installer,
                        InstanceParameters = (IReadOnlyDictionary<string, object>
                                )InstallerParameters.Convert<string, object>(),
                    };

                    var asset = vlt.CreateAsset(Vault.VaultAssetType.InstallerConfigInfo, ipi.Id.ToString());
                    using (var s = vlt.SaveAsset(asset))
                    {
                        JsonHelper.Save(s, pp);
                    }
                    v.InstallerProfiles.Add(ipi);
                }

                vlt.SaveVault(v);
            }
        }
    }
}
