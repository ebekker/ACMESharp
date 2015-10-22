using LetsEncrypt.ACME.POSH.Util;
using LetsEncrypt.ACME.POSH.Vault;
using System;
using System.IO;
using System.Management.Automation;

namespace LetsEncrypt.ACME.POSH
{
    [Cmdlet(VerbsData.Initialize, "Vault")]
    public class InitializeVault : Cmdlet
    {
        [Parameter]
        public string BaseURI
        { get; set; } = "https://acme-staging.api.letsencrypt.org/";

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
            using (var vp = GetVaultProvider(VaultProfile))
            {
                vp.InitStorage(Force);
                var v = new VaultConfig
                {
                    Id = EntityHelper.NewId(),
                    Alias = Alias,
                    Label = Label,
                    Memo = Memo,
                    BaseURI = BaseURI,
                    ServerDirectory = new AcmeServerDirectory()
                };

                vp.SaveVault(v);
            }
        }

        // TODO:  this routine doesn't belong here
        public static IVaultProvider GetVaultProvider(string profile, string provider = null)
        {
            // TODO: implement provider resolution
            var vp = new FileVaultProvider();
            vp.VaultProfile = profile;
            vp.Init();
            return vp;
        }
    }
}
