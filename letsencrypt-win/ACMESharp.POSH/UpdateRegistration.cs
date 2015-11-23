using LetsEncrypt.ACME.POSH.Util;
using LetsEncrypt.ACME.POSH.Vault;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH
{
    [Cmdlet(VerbsData.Update, "Registration", DefaultParameterSetName = PSET_DEFAULT)]
    [OutputType(typeof(AcmeRegistration))]
    public class UpdateRegistration : Cmdlet
    {
        public const string PSET_DEFAULT = "Default";
        public const string PSET_LOCAL_ONLY = "LocalOnly";

        [Parameter(ParameterSetName = PSET_DEFAULT)]
        public SwitchParameter UseBaseURI
        { get; set; }

        [Parameter(ParameterSetName = PSET_LOCAL_ONLY)]
        public SwitchParameter LocalOnly
        { get; set; }

        [Parameter(ParameterSetName = PSET_DEFAULT)]
        [ValidateCount(1, 100)]
        public string[] Contacts
        { get; set; }

        [Parameter(ParameterSetName = PSET_DEFAULT)]
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

        [Parameter]
        public string VaultProfile
        { get; set; }

        protected override void ProcessRecord()
        {
            using (var vp = InitializeVault.GetVaultProvider(VaultProfile))
            {
                vp.OpenStorage();
                var v = vp.LoadVault();

                if (v.Registrations == null || v.Registrations.Count < 1)
                    throw new InvalidOperationException("No registrations found");

                var ri = v.Registrations[0];
                var r = ri.Registration;

                if (!LocalOnly)
                {
                    using (var c = ClientHelper.GetClient(v, ri))
                    {
                        c.Init();
                        c.GetDirectory(true);

                        r = c.UpdateRegistration(UseBaseURI, AcceptTOS, Contacts);
                        ri.Registration = r;
                    }

                    vp.SaveVault(v);
                }

                v.Alias = StringHelper.IfNullOrEmpty(Alias);
                v.Label = StringHelper.IfNullOrEmpty(Label);
                v.Memo = StringHelper.IfNullOrEmpty(Memo);

                vp.SaveVault(v);

                WriteObject(r);
            }
        }
    }
}
