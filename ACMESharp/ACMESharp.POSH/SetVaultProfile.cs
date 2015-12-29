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
    [Cmdlet(VerbsCommon.Set, "VaultProfile", DefaultParameterSetName = PSET_SET)]
    public class SetVaultProfile : Cmdlet
    {
        public const string PSET_SET = "Set";
        public const string PSET_REMOVE = "Remove";


        [Parameter(Mandatory = true, Position = 0)]
        public string ProfileName
        { get; set; }

        [Parameter(Mandatory = true, Position = 1, ParameterSetName = PSET_SET)]
        public string ProviderName
        { get; set; }

        [Parameter(Mandatory = false, Position = 2, ParameterSetName = PSET_SET)]
        public Dictionary<string, object> VaultParameters
        { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = PSET_SET)]
        public Dictionary<string, object> ProviderParameters
        { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = PSET_REMOVE)]
        public SwitchParameter Remove
        { get; set; }

        protected override void ProcessRecord()
        {
            if (Remove)
            {
                VaultProfileManager.RemoveProfile(ProfileName);
            }
            else
            {
                VaultProfileManager.SetProfile(ProfileName, ProviderName,
                        VaultParameters, ProviderParameters);
            }
        }
    }
}
