using LetsEncrypt.ACME.POSH.Util;
using LetsEncrypt.ACME.POSH.Vault;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH
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
        public SwitchParameter AcceptTOS
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

        protected override void ProcessRecord()
        {
            using (var vp = InitializeVault.GetVaultProvider())
            {
                vp.OpenStorage();
                var v = vp.LoadVault();

                AcmeRegistration r = null;
                var ri = new RegistrationInfo
                {
                    Id = EntityHelper.NewId(),
                    Alias = Alias,
                    Label = Label,
                    Memo = Memo,
                    SignerProvider = Signer,
                };

                using (var c = ClientHelper.GetClient(v, ri))
                {
                    c.Init();
                    c.GetDirectory(true);

                    r = c.Register(Contacts);
                    ri.Registration = r;

                    if (v.Registrations == null)
                        v.Registrations = new OrderedDictionary();

                    v.Registrations.Add(ri.Id, ri);
                }

                vp.SaveVault(v);

                WriteObject(r);
            }
        }
    }
}
