using ACMESharp.Vault.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsCommon.Get, "VaultProfile", DefaultParameterSetName = PSET_LIST)]
    [OutputType(typeof(VaultProfile), ParameterSetName = new string[] { PSET_GET })]
    public class GetVaultProfile : Cmdlet
    {
        public const string PSET_LIST = "List";
        public const string PSET_GET = "Get";

        [Parameter(ParameterSetName = PSET_LIST)]
        public SwitchParameter ListProfiles
        { get; set; }

        [Parameter(ParameterSetName = PSET_GET, Position = 0)]
        public string ProfileName
        { get; set; }

        protected override void ProcessRecord()
        {
            if (ListProfiles)
            {
                WriteObject(VaultProfileManager.GetProfileNames(), true);
            }
            else
            {
                var profileName = VaultProfileManager.ResolveProfileName(ProfileName);
                WriteObject(VaultProfileManager.GetProfile(profileName));
            }
        }
    }
}
