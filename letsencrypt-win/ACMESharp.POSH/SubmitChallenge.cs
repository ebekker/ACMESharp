using LetsEncrypt.ACME.POSH.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH
{
    [Cmdlet(VerbsLifecycle.Submit, "Challenge")]
    public class SubmitChallenge : Cmdlet
    {
        [Parameter(Mandatory = true)]
        public string Ref
        { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateSet(
                AcmeProtocol.CHALLENGE_TYPE_DNS,
                AcmeProtocol.CHALLENGE_TYPE_HTTP,
                AcmeProtocol.CHALLENGE_TYPE_LEGACY_DNS,
                AcmeProtocol.CHALLENGE_TYPE_LEGACY_HTTP,
                IgnoreCase = true)]
        public string Challenge
        { get; set; }

        [Parameter]
        public SwitchParameter UseBaseURI
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

                using (var c = ClientHelper.GetClient(v, ri))
                {
                    c.Init();
                    c.GetDirectory(true);

                    var challenge = c.SubmitAuthorizeChallengeAnswer(authzState, Challenge, UseBaseURI);
                    ii.Challenges[Challenge] = challenge;
                }

                vp.SaveVault(v);

                WriteObject(authzState);
            }
        }
    }
}
