using ACMESharp.POSH.Util;
using System;
using System.Management.Automation;

namespace ACMESharp.POSH
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
        [ValidateSet(
                AcmeProtocol.CHALLENGE_TYPE_DNS,
                AcmeProtocol.CHALLENGE_TYPE_HTTP,
                IgnoreCase = true)]
        public string Challenge
        { get; set; }

        [Parameter(ParameterSetName = PSET_DEFAULT)]
        [Parameter(ParameterSetName = PSET_CHALLENGE)]
        public SwitchParameter UseBaseUri
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
                            authzState = c.RefreshIdentifierAuthorization(authzState, UseBaseUri);
                            ii.AuthorizationUpdate = authzState;
                        }
                        else
                        {
                            c.RefreshAuthorizeChallenge(authzState, Challenge, UseBaseUri);
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
