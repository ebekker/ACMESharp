using ACMESharp.Vault;
using System;
using System.IO;
using System.Management.Automation;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsData.Edit, "ProviderConfig", DefaultParameterSetName = PSET_LIST)]
    public class EditProviderConfig : Cmdlet
    {
        public const string PSET_LIST = "List";
        public const string PSET_EDIT = "Edit";

        [Parameter(ParameterSetName = PSET_LIST, Mandatory = true)]
        public SwitchParameter List
        { get; set; }

        [Parameter(ParameterSetName = PSET_EDIT, Mandatory = true)]
        public string Ref
        { get; set; }

        [Parameter]
        public string EditWith
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

                if (v.ProviderConfigs == null || v.ProviderConfigs.Count < 1)
                    throw new InvalidOperationException("No provider configs found");

                if (List)
                {
                    foreach (var item in v.ProviderConfigs.Values)
                        WriteObject(item);
                }
                else
                {
                    var pc = v.ProviderConfigs.GetByRef(Ref);
                    if (pc == null)
                        throw new Exception("Unable to find Provider Config for the given reference");
                    var pcFilePath = Path.GetFullPath($"{pc.Id}.json");

                    // Copy out the asset to a temp file for editing
                    var pcAsset = vlt.GetAsset(VaultAssetType.ProviderConfigInfo, $"{pc.Id}.json");
                    var temp = Path.GetTempFileName();
                    using (var s = vlt.LoadAsset(pcAsset))
                    {
                        using (var fs = new FileStream(temp, FileMode.Create))
                        {
                            s.CopyTo(fs);
                        }
                    }
                    NewProviderConfig.EditFile(temp, EditWith);

                    using (Stream fs = new FileStream(temp, FileMode.Open),
                            assetStream = vlt.SaveAsset(pcAsset))
                    {
                        fs.CopyTo(assetStream);
                    }
                }
            }
        }
    }
}
