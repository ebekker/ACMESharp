using ACMESharp.POSH.Util;
using ACMESharp.POSH.Vault;
using System;
using System.Management.Automation;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsCommon.New, "Identifier")]
    [OutputType(typeof(AuthorizationState))]
    public class NewIdentifier : Cmdlet
    {
        [Parameter]
        public string Dns
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

                AuthorizationState authzState = null;
                var ii = new IdentifierInfo
                {
                    Id = EntityHelper.NewId(),
                    Alias = Alias,
                    Label = Label,
                    Memo = Memo,
                    RegistrationRef = ri.Id,
                    Dns = Dns,
                };

                using (var c = ClientHelper.GetClient(v, ri))
                {
                    c.Init();
                    c.GetDirectory(true);

                    authzState = c.AuthorizeIdentifier(Dns);
                    ii.Authorization = authzState;

                    if (v.Identifiers == null)
                        v.Identifiers = new EntityDictionary<IdentifierInfo>();

                    v.Identifiers.Add(ii);
                }

                vp.SaveVault(v);

                WriteObject(authzState);
            }
        }
    }
}
