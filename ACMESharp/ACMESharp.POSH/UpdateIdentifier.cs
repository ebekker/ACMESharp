using ACMESharp.POSH.Util;
using ACMESharp.Util;
using System;
using System.Management.Automation;

namespace ACMESharp.POSH
{
    /// <summary>
    /// <para type="synopsis">
    ///   Updates the status and details of an Identifier stored in the Vault.
    /// </para>
    /// <para type="description">
    ///   Use this cmdlet to update characteristics of an Identifier that are
    ///   defined locally, such as the Alias or Label.
    /// </para>
    /// <para type="description">
    ///   Also use this cmdlet to refresh the state and status of an Identifier
    ///   by probing the associated ACME CA Server for Identifier details.
    /// </para>
    /// <para type="link">New-Identifier</para>
    /// <para type="link">Get-Identifier</para>
    /// <para type="link">Complete-Challenge</para>
    /// <para type="link">Submit-Challenge</para>
    /// </summary>
    [Cmdlet(VerbsData.Update, "Identifier", DefaultParameterSetName = PSET_DEFAULT)]
    public class UpdateIdentifier : Cmdlet
    {
        public const string PSET_DEFAULT = "Default";
        public const string PSET_CHALLENGE = "Challenge";
        public const string PSET_LOCAL_ONLY = "LocalOnly";

        /// <summary>
        /// <para type="description">
        ///     A reference (ID or alias) to a previously defined Identifier submitted
        ///     to the ACME CA Server for verification.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [Alias("Ref")]
        public string IdentifierRef
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     Specifies the ACME Challenge type that should be updated.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_CHALLENGE, Position = 1, Mandatory = true)]
        [ValidateSet(
                AcmeProtocol.CHALLENGE_TYPE_DNS,
                AcmeProtocol.CHALLENGE_TYPE_HTTP,
                IgnoreCase = true)]
        public string ChallengeType
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     Overrides the base URI associated with the target Registration and used
        ///     for subsequent communication with the associated ACME CA Server.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_DEFAULT)]
        [Parameter(ParameterSetName = PSET_CHALLENGE)]
        public SwitchParameter UseBaseUri
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   Indicates that updates should be performed locally only, and no attempt
        ///   should be made to retrieve the current status from the ACME CA Server.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_LOCAL_ONLY)]
        public SwitchParameter LocalOnly
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   Optionaly, set or update the unique alias assigned to the Identifier
        ///   for future reference.  To remove the alias, use the empty string.
        /// </para>
        /// </summary>
        [Parameter]
        public string NewAlias
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   Optionally, set or update the human-friendly label to assigned to the
        ///   Identifier for easy recognition.
        /// </para>
        /// </summary>
        [Parameter]
        public string Label
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   Optionall, set or update the arbitrary text field used to capture any
        ///   notes or details associated with the Identifier.
        /// </para>
        /// </summary>
        [Parameter]
        public string Memo
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

                if (v.Registrations == null || v.Registrations.Count < 1)
                    throw new InvalidOperationException("No registrations found");

                var ri = v.Registrations[0];
                var r = ri.Registration;

                if (v.Identifiers == null || v.Identifiers.Count < 1)
                    throw new InvalidOperationException("No identifiers found");

                var ii = v.Identifiers.GetByRef(IdentifierRef, throwOnMissing: false);
                if (ii == null)
                    throw new Exception("Unable to find an Identifier for the given reference");

                // If we're renaming the Alias, do that
                // first in case there are any problems
                if (NewAlias != null)
                {
                    v.Identifiers.Rename(IdentifierRef, NewAlias);
                    ii.Alias = NewAlias == "" ? null : NewAlias;
                }


                var authzState = ii.Authorization;

                if (!LocalOnly)
                {
                    try {
                        using (var c = ClientHelper.GetClient(v, ri))
                        {
                            c.Init();
                            c.GetDirectory(true);

                            if (string.IsNullOrEmpty(ChallengeType))
                            {
                                authzState = c.RefreshIdentifierAuthorization(authzState, UseBaseUri);
                            }
                            else
                            {
                                c.RefreshAuthorizeChallenge(authzState, ChallengeType, UseBaseUri);
                            }
                        }

                        ii.Authorization = authzState;
                    }
                    catch (AcmeClient.AcmeWebException ex)
                    {
                        ThrowTerminatingError(PoshHelper.CreateErrorRecord(ex, ii));
                        return;
                    }
                }

                ii.Label = StringHelper.IfNullOrEmpty(Label);
                ii.Memo = StringHelper.IfNullOrEmpty(Memo);

                vlt.SaveVault(v);

                WriteObject(authzState);
            }
        }
    }
}
