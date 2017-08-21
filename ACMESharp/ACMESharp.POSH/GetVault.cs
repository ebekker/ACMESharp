using System.Management.Automation;

namespace ACMESharp.POSH
{
	[Cmdlet(VerbsCommon.Get, "Vault")]
    [OutputType(typeof(Vault.Model.VaultInfo))]
    public class GetVault : AcmeCmdlet
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
