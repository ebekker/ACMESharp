using ACMESharp.POSH.Util;
using System;
using System.Linq;
using System.Management.Automation;

namespace ACMESharp.POSH
{
    /// <summary>
    /// <para type="synopsis">
    ///     Submits a completed Challenge for verification by ACME CA Server.
    /// </para>
    /// <para type="description">
    ///   After a Challenge has been handled and completed, it needs to be submitted to the
    ///   ACME CA Server that issued the Challenge.  This cmdlet submits the Challenge of a
    ///   particular type to the ACME Server to be verified.  If the ACME Server issued multiple
    ///   Challenges and combinations that will satisfy the validation of ownership of an
    ///   Identifier, you can use this cmdlet to submit each Challenge type completed.
    /// </para>
    /// <para type="link">New-Identifier</para>
    /// <para type="link">Complete-Challenge</para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Submit, "Challenge")]
    public class SubmitChallenge : Cmdlet
    {
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
        ///     Specifies the ACME Challenge type that should be submitted.  This type
        ///     is expected to be found in the list of Challenges returned by the
        ///     ACME CA Server for the associated Identifier and it should already have
        ///     been handled previously, either externally to the ACMESharp operations
        ///     or via the Handler mechanisms within.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 1)]
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
        [Parameter]
        public SwitchParameter UseBaseUri
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
        /// <summary>
        /// <para type="description">
        ///     Forces an attempt to submit the challenge even when the state of the
        ///     current Identifier authorization is in a failed or completed state.
        /// </para>
        /// </summary>
        [Parameter]
        public SwitchParameter Force
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

                var authzState = ii.Authorization;

                if (!Force)
                {
                    if (!authzState.IsPending())
                        throw new InvalidOperationException(
                                "authorization is not in pending state;"
                                + " use Force flag to override this validation");

                    if (authzState.Challenges.Any(_ => _.IsInvalid()))
                        throw new InvalidOperationException(
                                "authorization already contains challenges in an invalid state;"
                                + " use Force flag to override this validation");
                }

                try
                {
                    using (var c = ClientHelper.GetClient(v, ri))
                    {
                        c.Init();
                        c.GetDirectory(true);

                        var challenge = c.SubmitChallengeAnswer(authzState, ChallengeType, UseBaseUri);
                        ii.Challenges[ChallengeType] = challenge;
                    }
                }
                catch (AcmeClient.AcmeWebException ex)
                {
                    ThrowTerminatingError(PoshHelper.CreateErrorRecord(ex, ii));
                    return;
                }

                vlt.SaveVault(v);

                WriteObject(authzState);
            }
        }
    }
}
