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

                if (v.Identifiers == null || v.Identifiers.Count < 1)
                    throw new InvalidOperationException("No identifiers found");

                if (string.IsNullOrEmpty(Ref))
                {
                    int seq = 0;
                    WriteObject(v.Identifiers.Values.Select(x => new
                    {
                        Seq = seq++,
                        Id = x.Id,
                        Alias = x.Alias,
                        Label = x.Label,
                        Dns = x.Dns,
                        Status = x.Authorization.Status
                    }), true);
                }
                else
                {
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
