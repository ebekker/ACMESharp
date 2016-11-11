using System;
using System.Linq;
using System.Management.Automation;

namespace ACMESharp.POSH
{
    /// <summary>
    /// <para type="synopsis">Lists all, or retrieves details for, Identifiers submitted for
    ///     verification.</para>
    /// <para type="description">
    ///   Use this cmdlet to list all of the Identifier that have been previously
    ///   defined and submitted to the ACME CA Server of the current Vault.  You
    ///   also use this cmdlet to specify specific Identifier references (ID or alias)
    ///   to retrieve more specific details as they are captured in the Vault.
    /// </para>
    /// <para type="link">New-Identifier</para>
    /// <para type="link">Update-Identifier</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Identifier", DefaultParameterSetName = PSET_LIST)]
    [OutputType(typeof(AuthorizationState))]
    public class GetIdentifier : Cmdlet
    {
        public const string PSET_LIST = "List";
        public const string PSET_GET = "Get";


        /// <summary>
        /// <para type="description">
        ///     A reference (ID or alias) to a previously defined Identifier submitted
        ///     to the ACME CA Server for verification.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = PSET_GET)]
        [Alias("Ref")]
        public string IdentifierRef
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     Specifies a Vault profile name that will resolve to the Vault instance to be
        ///     used for all related operations and storage/retrieval of all related assets.
        /// </para>
        /// </summary>
        [Parameter]
        public string VaultProfile
        { get; set; }

        protected override void ProcessRecord()
        {
            using (var vlt = Util.VaultHelper.GetVault(VaultProfile))
            {
                vlt.OpenStorage();
                var v = vlt.LoadVault();

                // branch based on whether an ID ref was specified
                if (string.IsNullOrEmpty(IdentifierRef))
                {
                    if (v.Identifiers == null || v.Identifiers.Count < 1)
                    {
                        // just return null if there are no IDs
                        WriteObject(null);
                    }
                    else
                    {
                        // otherwise, return all IDs
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
                }
                else
                {
                    if (v.Identifiers == null || v.Identifiers.Count < 1)
                    {
                        // throw because none exist (and thus, we couldn't find the one specified)
                        throw new ItemNotFoundException("Unable to find an Identifier for the given reference");
                    }
                    else
                    {
                        var ii = v.Identifiers.GetByRef(IdentifierRef, throwOnMissing: false);
                        if (ii == null)
                            throw new ItemNotFoundException("Unable to find an Identifier for the given reference");

                        var authzState = ii.Authorization;

                        WriteObject(authzState);
                    }
                }
            }
        }
    }
}
