using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsCommon.Get, "Vault")]
    [OutputType(typeof(Vault.Model.VaultInfo))]
    public class GetVault : Cmdlet
    {
        [Parameter]
        public string VaultProfile
        { get; set; }

        protected override void ProcessRecord()
        {
            using (var vlt = Util.VaultHelper.GetVault(VaultProfile))
            {
                if (!vlt.TestStorage())
                {
                    WriteObject(null);
                }
                else
                {
                    vlt.OpenStorage();
                    WriteObject(vlt.LoadVault(true));
                }
            }
        }
    }
}
