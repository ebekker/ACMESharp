using System;
using System.Linq;
using System.Management.Automation;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsCommon.Remove, "Identifier")]
    public class RemoveIdentifier : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        [Alias("Ref")]
        public string IdentifierRef
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

                if (v.Identifiers == null || v.Identifiers.Count < 1)
                {
                    // throw because none exist (and thus, we couldn't find the one specified)
                    throw new ItemNotFoundException("No Identifiers found");
                }
                else
                {
                    var ii = v.Identifiers.GetByRef(IdentifierRef, throwOnMissing: false);
                    if (ii == null)
                    {
                        throw new ItemNotFoundException("Unable to find an Identifier for the given reference");
                    }
                    else
                    {
                        v.Identifiers.Remove(ii.Id);
                    }
                    vlt.SaveVault(v);
                }
            }
        }
    }
}
