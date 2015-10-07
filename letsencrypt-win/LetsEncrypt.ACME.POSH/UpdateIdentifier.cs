using LetsEncrypt.ACME.POSH.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH
{
    [Cmdlet(VerbsData.Update, "Identifier", DefaultParameterSetName = PSET_DEFAULT)]
    public class UpdateIdentifier : Cmdlet
    {
        public const string PSET_DEFAULT = "Default";
        public const string PSET_CHALLENGE = "Challenge";
        public const string PSET_LOCAL_ONLY = "LocalOnly";

        [Parameter(Mandatory = true)]
        public string Ref
        { get; set; }

        [Parameter(ParameterSetName = PSET_CHALLENGE, Mandatory = true)]
        [ValidateSet("dns", "simpleHttp", IgnoreCase = true)]
        public string Challenge
        { get; set; }

        [Parameter(ParameterSetName = PSET_DEFAULT)]
        [Parameter(ParameterSetName = PSET_CHALLENGE)]
        public SwitchParameter UseBaseURI
        { get; set; }

        [Parameter(ParameterSetName = PSET_LOCAL_ONLY)]
        public SwitchParameter LocalOnly
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

                if (v.Registrations == null || v.Registrations.Count < 1)
                    throw new InvalidOperationException("No registrations found");

                var ri = v.Registrations[0];
                var r = ri.Registration;

                if (v.Identifiers == null || v.Identifiers.Count < 1)
                    throw new InvalidOperationException("No identifiers found");

                var ii = v.Identifiers.GetByRef(Ref);
                if (ii == null)
                    throw new Exception("Unable to find an Identifier for the given reference");

                var authzState = ii.Authorization;

                if (!LocalOnly)
                {
                    using (var c = ClientHelper.GetClient(v, ri))
                    {
                        c.Init();
                        c.GetDirectory(true);

                        if (string.IsNullOrEmpty(Challenge))
                        {
                            authzState = c.RefreshIdentifierAuthorization(authzState, UseBaseURI);
                            ii.AuthorizationUpdate = authzState;
                        }
                        else
                        {
                            c.RefreshAuthorizeChallenge(authzState, Challenge, UseBaseURI);
                            ii.Authorization = authzState;
                        }
                    }
                }

                v.Alias = StringHelper.IfNullOrEmpty(Alias);
                v.Label = StringHelper.IfNullOrEmpty(Label);
                v.Memo = StringHelper.IfNullOrEmpty(Memo);

                vp.SaveVault(v);

                WriteObject(authzState);
            }
        }
    }
}
