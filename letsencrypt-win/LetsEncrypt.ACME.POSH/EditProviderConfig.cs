using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH
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
        public string VaultProfile
        { get; set; }

        protected override void ProcessRecord()
        {
            using (var vp = InitializeVault.GetVaultProvider(VaultProfile))
            {
                vp.OpenStorage();
                var v = vp.LoadVault();

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

                    WriteObject(pcFilePath);
                }
            }
        }
    }
}
