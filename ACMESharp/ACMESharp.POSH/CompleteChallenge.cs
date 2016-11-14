using ACMESharp.POSH.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using ACMESharp.DNS;
using ACMESharp.WebServer;
using System.Collections;
using ACMESharp.Vault.Profile;
using ACMESharp.Util;
using ACMESharp.ACME;
using System.Linq;

namespace ACMESharp.POSH
{
    /// <summary>
    /// <para type="synopsis">Completes a Challenge using a prescribed Handler.</para>
    /// <para type="description">
    ///   Use this cmdlet to complete a Challenge associated with an Identifier
    ///   defined in an ACMESharp Vault that has been submitted for verification
    ///   to an ACME CA Server.
    /// </para>
    /// <para type="link">Get-ChallengeHandlerProfile</para>
    /// <para type="link">Set-ChallengeHandlerProfile</para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Complete, "Challenge")]
    public class CompleteChallenge : Cmdlet
    {
        public const string PSET_CHALLENGE_HANDLER_INLINE = "ChallengeHandlerInline";
        public const string PSET_CHALLENGE_HANDLER_PROFILE = "ChallengeHandlerProfile";

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
        ///     Specifies a reference (ID or alias) to a previously defined Challenge
        ///     Handler profile in the associated Vault that defines the Handler
        ///     provider and associated instance parameters that should be used to
        ///     resolve the Challenge.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = PSET_CHALLENGE_HANDLER_PROFILE)]
        public string HandlerProfileRef
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     Specifies the ACME Challenge type that should be handled.  This type
        ///     is expected to be found in the list of Challenges returned by the
        ///     ACME CA Server for the associated Identifier.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = PSET_CHALLENGE_HANDLER_INLINE)]
        [ValidateSet(
                AcmeProtocol.CHALLENGE_TYPE_DNS,
                AcmeProtocol.CHALLENGE_TYPE_HTTP,
                IgnoreCase = true)]
        public string ChallengeType
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     Specifies the Challenge Handler instance provider that will be used to
        ///     handle the associated Challenge.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = PSET_CHALLENGE_HANDLER_INLINE)]
        public string Handler
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     Specifies the parameters that will be passed to the Challenge Handler
        ///     instance that will be used to handle the associated Challenge.
        /// </para>
        /// <para type="description">
        ///     If this cmdlet is invoked *in-line*, then these are the only parameters
        ///     that will be passed to the handler.  If this cmdlet is invoked with a
        ///     handler profile reference, then these parameters are merged with, and
        ///     override, whatever parameters are already defined within the profile.
        /// </para>
        /// </summary>
        [Parameter]
        public Hashtable HandlerParameters
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     When specified, executes the <i>clean up</i> operation associated with
        ///     the resolved Challenge Handler.  This is typcially invoked after the
        ///     challenge has been previously successfully completed and submitted to
        ///     the ACME server, and is used to remove any residual resources or traces
        ///     of the steps that were needed during the challenge-handling process.
        /// </para>
        /// </summary>
        [Parameter]
        public SwitchParameter CleanUp
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     When specified, will force the decoding and regeneration of any ACME-defined
        ///     heuristics and parameters for the given Challenge type.
        /// </para>
        /// </summary>
        [Parameter]
        public SwitchParameter Regenerate
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     When specified, forces the resolved Handler to repeat the process of
        ///     handling the given Challenge, even if the process has already been
        ///     completed previously.
        /// </para>
        /// </summary>
        [Parameter]
        public SwitchParameter Repeat
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
        ///     Forces an attempt to complete the challenge even when the state of the
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

                if (ii.Challenges == null)
                    ii.Challenges = new Dictionary<string, AuthorizeChallenge>();

                if (ii.ChallengeCompleted == null)
                    ii.ChallengeCompleted = new Dictionary<string, DateTime?>();

                if (ii.ChallengeCleanedUp == null)
                    ii.ChallengeCleanedUp = new Dictionary<string, DateTime?>();

                // Resolve details from inline or profile attributes
                string challengeType = null;
                string handlerName = null;
                IReadOnlyDictionary<string, object> handlerParams = null;
                IReadOnlyDictionary<string, object> cliHandlerParams = null;

                if (HandlerParameters?.Count > 0)
                    cliHandlerParams = (IReadOnlyDictionary<string, object>
                                    )PoshHelper.Convert<string, object>(HandlerParameters);

                if (!Force && !CleanUp)
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

                if (!string.IsNullOrEmpty(HandlerProfileRef))
                {
                    var ppi = v.ProviderProfiles.GetByRef(HandlerProfileRef, throwOnMissing: false);
                    if (ppi == null)
                        throw new ItemNotFoundException("no Handler profile found for the given reference")
                                .With(nameof(HandlerProfileRef), HandlerProfileRef);

                    var ppAsset = vlt.GetAsset(Vault.VaultAssetType.ProviderConfigInfo,
                            ppi.Id.ToString());
                    ProviderProfile pp;
                    using (var s = vlt.LoadAsset(ppAsset))
                    {
                        pp = JsonHelper.Load<ProviderProfile>(s);
                    }
                    if (pp.ProviderType != ProviderType.CHALLENGE_HANDLER)
                        throw new InvalidOperationException("referenced profile does not resolve to a Challenge Handler")
                                .With(nameof(HandlerProfileRef), HandlerProfileRef)
                                .With("actualProfileProviderType", pp.ProviderType.ToString());

                    if (!pp.ProfileParameters.ContainsKey(nameof(ChallengeType)))
                        throw new InvalidOperationException("handler profile is incomplete; missing Challenge Type")
                                .With(nameof(HandlerProfileRef), HandlerProfileRef);

                    challengeType = (string)pp.ProfileParameters[nameof(ChallengeType)];
                    handlerName = pp.ProviderName;
                    handlerParams = pp.InstanceParameters;
                    if (cliHandlerParams != null)
                    {
                        WriteVerbose("Override Handler parameters specified");
                        if (handlerParams == null || handlerParams.Count == 0)
                        {
                            WriteVerbose("Profile does not define any parameters, using override parameters only");
                            handlerParams = cliHandlerParams;
                        }
                        else
                        {
                            WriteVerbose("Merging Handler override parameters with profile");
                            var mergedParams = new Dictionary<string, object>();

                            foreach (var kv in pp.InstanceParameters)
                                mergedParams[kv.Key] = kv.Value;
                            foreach (var kv in cliHandlerParams)
                                mergedParams[kv.Key] = kv.Value;

                            handlerParams = mergedParams;
                        }
                    }
                }
                else
                {
                    challengeType = ChallengeType;
                    handlerName = Handler;
                    handlerParams = cliHandlerParams;
                }

                AuthorizeChallenge challenge = null;
                DateTime? challengeCompleted = null;
                DateTime? challengeCleanedUp = null;
                ii.Challenges.TryGetValue(challengeType, out challenge);
                ii.ChallengeCompleted.TryGetValue(challengeType, out challengeCompleted);
                ii.ChallengeCleanedUp.TryGetValue(challengeType, out challengeCleanedUp);

                try
                {
                    if (challenge == null || Regenerate)
                    {
                        using (var c = ClientHelper.GetClient(v, ri))
                        {
                            c.Init();
                            c.GetDirectory(true);

                            challenge = c.DecodeChallenge(authzState, challengeType);
                            ii.Challenges[challengeType] = challenge;
                        }
                    }

                    if (CleanUp && (Repeat || challengeCleanedUp == null))
                    {
                        using (var c = ClientHelper.GetClient(v, ri))
                        {
                            c.Init();
                            c.GetDirectory(true);

                            challenge = c.HandleChallenge(authzState, challengeType,
                                    handlerName, handlerParams, CleanUp);
                            ii.ChallengeCleanedUp[challengeType] = DateTime.Now;
                        }
                    }
                    else if (Repeat || challengeCompleted == null)
                    {
                        using (var c = ClientHelper.GetClient(v, ri))
                        {
                            c.Init();
                            c.GetDirectory(true);

                            challenge = c.HandleChallenge(authzState, challengeType,
                                    handlerName, handlerParams);
                            ii.ChallengeCompleted[challengeType] = DateTime.Now;
                        }
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
