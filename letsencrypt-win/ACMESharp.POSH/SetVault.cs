using LetsEncrypt.ACME.POSH.Util;
using System.Management.Automation;

namespace LetsEncrypt.ACME.POSH
{
    [Cmdlet(VerbsCommon.Set, "Vault")]
    public class SetVault : Cmdlet
    {
        [Parameter]
        public string BaseURI
        { get; set; }

        [Parameter]
        [ValidateSet("RSA256")]
        public string Signer
        { get; set; }

        [Parameter]
        public bool Force
        { get; set; }

        [Parameter]
        public string Alias
        { get; set; }

        [Parameter]
        public string Label
        { get; set; }

        [Parameter]
        public string Memo
        { get; set; }

        [Parameter]
        public string VaultProfile
        { get; set; }

        protected override void ProcessRecord()
        {
            using (var vp = InitializeVault.GetVaultProvider(VaultProfile))
            {
                vp.OpenStorage(Force);
                var v = vp.LoadVault();

                v.Alias = StringHelper.IfNullOrEmpty(Alias);
                v.Label = StringHelper.IfNullOrEmpty(Label);
                v.Memo = StringHelper.IfNullOrEmpty(Memo);
                v.BaseURI = StringHelper.IfNullOrEmpty(BaseURI);

                vp.SaveVault(v);
            }
        }
    }
}
