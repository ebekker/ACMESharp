using ACMESharp.POSH.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using ACMESharp.DNS;
using ACMESharp.WebServer;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsLifecycle.Complete, "Challenge")]
    public class CompleteChallenge : Cmdlet
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

        [Parameter(Mandatory = true)]
        public string ProviderConfig
        { get; set; }

        [Parameter]
        public SwitchParameter Regenerate
        { get; set; }

        [Parameter]
        public SwitchParameter Repeat
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

                if (ii.Challenges == null)
                    ii.Challenges = new Dictionary<string, AuthorizeChallenge>();

                if (ii.ChallengeCompleted == null)
                    ii.ChallengeCompleted = new Dictionary<string, DateTime?>();

                if (v.ProviderConfigs == null || v.ProviderConfigs.Count < 1)
                    throw new InvalidOperationException("No provider configs found");

                var pc = v.ProviderConfigs.GetByRef(ProviderConfig);
                if (pc == null)
                    throw new InvalidOperationException("Unable to find a Provider Config for the given reference");

                AuthorizeChallenge challenge = null;
                DateTime? challengCompleted = null;
                ii.Challenges.TryGetValue(Challenge, out challenge);
                ii.ChallengeCompleted.TryGetValue(Challenge, out challengCompleted);

                if (challenge == null || Regenerate)
                {
                    using (var c = ClientHelper.GetClient(v, ri))
                    {
                        c.Init();
                        c.GetDirectory(true);

                        challenge = c.GenerateAuthorizeChallengeAnswer(authzState, Challenge);
                        ii.Challenges[Challenge] = challenge;
                    }
                }

                if (Repeat || challengCompleted == null)
                {
                    var pcFilePath = $"{pc.Id}.json";
                    var pcAsset = vp.GetAsset(Vault.VaultAssetType.ProviderConfigInfo, pcFilePath);

                    // TODO:  There's *way* too much logic buried in here
                    // this needs to be refactored and extracted out to be
                    // more manageble and more reusable

                    if (Challenge == AcmeProtocol.CHALLENGE_TYPE_DNS
                            || Challenge == AcmeProtocol.CHALLENGE_TYPE_LEGACY_DNS)
                    {
                        if (string.IsNullOrEmpty(pc.DnsProvider))
                            throw new InvalidOperationException("Referenced Provider Configuration does not support the selected Challenge");

                        var dnsName = challenge.ChallengeAnswer.Key;
                        var dnsValue = Regex.Replace(challenge.ChallengeAnswer.Value, "\\s", "");
                        var dnsValues = Regex.Replace(dnsValue, "(.{100,100})", "$1\n").Split('\n');

                        using (var s = vp.LoadAsset(pcAsset)) // new FileStream(pcFilePath, FileMode.Open))
                        {
                            var dnsInfo = DnsInfo.Load(s);
                            dnsInfo.Provider.EditTxtRecord(dnsName, dnsValues);
                            ii.ChallengeCompleted[Challenge] = DateTime.Now;
                        }
                    }
                    else if (Challenge == AcmeProtocol.CHALLENGE_TYPE_HTTP
                            || Challenge == AcmeProtocol.CHALLENGE_TYPE_LEGACY_HTTP)
                    {
                        if (string.IsNullOrEmpty(pc.WebServerProvider))
                            throw new InvalidOperationException("Referenced Provider Configuration does not support the selected Challenge");

                        var wsFilePath = challenge.ChallengeAnswer.Key;
                        var wsFileBody = challenge.ChallengeAnswer.Value;
                        var wsFileUrl = new Uri($"http://{authzState.Identifier}/{wsFilePath}");



                        using (var s = vp.LoadAsset(pcAsset)) // new FileStream(pcFilePath, FileMode.Open))
                        {
                            var webServerInfo = WebServerInfo.Load(s);
                            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(wsFileBody)))
                            {
                                webServerInfo.Provider.UploadFile(wsFileUrl, ms);
                                ii.ChallengeCompleted[Challenge] = DateTime.Now;
                            }
                        }
                    }
                }

                vp.SaveVault(v);

                WriteObject(authzState);
            }
        }
    }
}
