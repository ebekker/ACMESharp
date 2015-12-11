using System;
using System.Linq;
using System.Management.Automation;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsCommon.Get, "Identifier", DefaultParameterSetName = PSET_LIST)]
    [OutputType(typeof(AuthorizationState))]
    public class GetIdentifier : Cmdlet
    {
        public const string PSET_LIST = "List";
        public const string PSET_GET = "Get";

        [Parameter(ParameterSetName = PSET_GET)]
        public string Ref
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

                if (string.IsNullOrEmpty(Ref))
                {
                    int seq = 0;
                    WriteObject(v.Identifiers.Values.Select(x => new
                    {
                        Seq = seq++,
                        x.Id,
                        x.Alias,
                        x.Label,
                        x.Dns,
                        x.Authorization.Status
                    }), true);
                }
                else
                {
                    if (v.Identifiers == null || v.Identifiers.Count < 1)
                        throw new InvalidOperationException("No identifiers found");

                    var ii = v.Identifiers.GetByRef(Ref);
                    if (ii == null)
                        throw new ItemNotFoundException("Unable to find an Identifier for the given reference");

                    var authzState = ii.Authorization;

                    WriteObject(authzState);
                }
            }
        }
    }
}
