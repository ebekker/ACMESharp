using ACMESharp.POSH.Util;
using ACMESharp.Util;
using System;
using System.Management.Automation;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsData.Update, "Registration", DefaultParameterSetName = PSET_DEFAULT)]
    [OutputType(typeof(AcmeRegistration))]
    public class UpdateRegistration : Cmdlet
    {
        public const string PSET_DEFAULT = "Default";
        public const string PSET_LOCAL_ONLY = "LocalOnly";

        [Parameter(ParameterSetName = PSET_DEFAULT)]
        public SwitchParameter UseBaseUri
        { get; set; }

        [Parameter(ParameterSetName = PSET_LOCAL_ONLY)]
        public SwitchParameter LocalOnly
        { get; set; }

        [Parameter(ParameterSetName = PSET_DEFAULT)]
        [ValidateCount(1, 100)]
        public string[] Contacts
        { get; set; }

        [Parameter(ParameterSetName = PSET_DEFAULT)]
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

                        r = c.UpdateRegistration(UseBaseUri, AcceptTos, Contacts);
                        ri.Registration = r;
                    }

                    vlt.SaveVault(v);
                }

                v.Alias = StringHelper.IfNullOrEmpty(Alias);
                v.Label = StringHelper.IfNullOrEmpty(Label);
                v.Memo = StringHelper.IfNullOrEmpty(Memo);

                vlt.SaveVault(v);

                WriteObject(r);
            }
        }
    }
}
