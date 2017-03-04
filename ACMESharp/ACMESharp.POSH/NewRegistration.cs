using ACMESharp.POSH.Util;
using ACMESharp.Vault;
using ACMESharp.Vault.Model;
using ACMESharp.Vault.Util;
using System.Management.Automation;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsCommon.New, "Registration")]
    public class NewRegistration : Cmdlet
    {
        [Parameter(Mandatory = true)]
        [ValidateCount(1, 100)]
        public string[] Contacts
        { get; set; }

        [Parameter]
        [ValidateSet("RS256")]
        public string Signer
        { get; set; } = "RS256";

        [Parameter]
        public SwitchParameter AcceptTos
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
            using (var vlt = Util.VaultHelper.GetVault(VaultProfile))
            {
                vlt.OpenStorage();
                var v = vlt.LoadVault();

                AcmeRegistration r = null;
                var ri = new RegistrationInfo
                {
                    Id = EntityHelper.NewId(),
                    Alias = Alias,
                    Label = Label,
                    Memo = Memo,
                    SignerProvider = Signer,
                };

                try
                {
                    using (var c = ClientHelper.GetClient(v, ri))
                    {
                        c.Init();
                        c.GetDirectory(true);

                        r = c.Register(Contacts);
                        if (AcceptTos)
                            r = c.UpdateRegistration(agreeToTos: true);

                        ri.Registration = r;

                        if (v.Registrations == null)
                            v.Registrations = new EntityDictionary<RegistrationInfo>();

                        v.Registrations.Add(ri);
                    }
                }
                catch (AcmeClient.AcmeWebException ex)
                {
                    ThrowTerminatingError(PoshHelper.CreateErrorRecord(ex, ri));
                    return;
                }

                vlt.SaveVault(v);

                WriteObject(r);
            }
        }
    }
}
