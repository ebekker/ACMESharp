using ACMESharp.ACME;
using ACMESharp.Util;
using ACMESharp.Vault.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsCommon.Get, "ChallengeHandlerProfile", DefaultParameterSetName = PSET_GET_HANDLER_PROFILE)]
    public class GetChallengeHandlerProfile : Cmdlet
    {
        public const string PSET_GET_CHALLENGE_TYPE = "GetChallengeType";
        public const string PSET_GET_CHALLENGE_HANDLER = "GetChallengeHandler";
        public const string PSET_LIST_HANDLER_PROFILES = "ListHandlerProfiles";
        public const string PSET_GET_HANDLER_PROFILE = "GetHandlerProfile";
        public const string PSET_RELOAD_PROVIDERS = "ReloadProviders";

        [Parameter(ParameterSetName = PSET_GET_CHALLENGE_TYPE)]
        public SwitchParameter ListChallengeTypes
        { get; set; }

        [Parameter(ParameterSetName = PSET_GET_CHALLENGE_TYPE)]
        public string GetChallengeType
        { get; set; }

        [Parameter(ParameterSetName = PSET_GET_CHALLENGE_HANDLER)]
        public SwitchParameter ListChallengeHandlers
        { get; set; }

        [Parameter(ParameterSetName = PSET_GET_CHALLENGE_HANDLER)]
        public string GetChallengeHandler
        { get; set; }

        [Parameter(ParameterSetName = PSET_GET_CHALLENGE_HANDLER)]
        public SwitchParameter ParametersOnly
        { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = PSET_LIST_HANDLER_PROFILES)]
        public SwitchParameter ListProfiles
        { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = PSET_GET_HANDLER_PROFILE)]
        public string ProfileRef
        { get; set; }

        [Parameter(ParameterSetName = PSET_LIST_HANDLER_PROFILES)]
        [Parameter(ParameterSetName = PSET_GET_HANDLER_PROFILE)]
        public string VaultProfile
        { get; set; }

        [Parameter(ParameterSetName = PSET_RELOAD_PROVIDERS)]
        public SwitchParameter ReloadProviders
        { get; set; }

        protected override void ProcessRecord()
        {
            // We have to invoke this here because we *may not* invoke
            // any Vault access but we do rely on Ext mechanism access.
            Util.PoshHelper.BeforeExtAccess();

            if (ReloadProviders)
            {
                ChallengeDecoderExtManager.Reload();
                ChallengeHandlerExtManager.Reload();
            }
            else if (!string.IsNullOrEmpty(GetChallengeType))
            {
                WriteVerbose("Getting details of Challenge Type Decoder");
                var tInfo = ChallengeDecoderExtManager.GetProviderInfo(GetChallengeType);
                var t = ChallengeDecoderExtManager.GetProvider(GetChallengeType);
                WriteObject(new {
                        ChallengeType = tInfo.Type,
                        tInfo.Label,
                        tInfo.SupportedType,
                        tInfo.Description,
                    });
            }
            else if (ListChallengeTypes)
            {
                WriteVerbose("Listing all Challenge Type Decoders");
                WriteObject(ChallengeDecoderExtManager.GetProviderInfos().Select(_ => _.Name), true);
            }
            else if (!string.IsNullOrEmpty(GetChallengeHandler))
            {
                WriteVerbose("Getting details of Challenge Type Handler");
                var pInfo = ChallengeHandlerExtManager.GetProviderInfo(GetChallengeHandler);
                var p = ChallengeHandlerExtManager.GetProvider(GetChallengeHandler);
                if (ParametersOnly)
                {
                    WriteVerbose("Showing parameter details only");
                    WriteObject(p.DescribeParameters().Select(_ => new {
                            _.Name,
                            _.Label,
                            _.Type,
                            _.IsRequired,
                            _.IsMultiValued,
                            _.Description,
                        }), true);
                }
                else
                {
                    WriteObject(new {
                            pInfo.Name,
                            pInfo.Label,
                            pInfo.SupportedTypes,
                            pInfo.Description,
                            Parameters = p.DescribeParameters().Select(_ => new {
                                    _.Name,
                                    _.Label,
                                    _.Type,
                                    _.IsRequired,
                                    _.IsMultiValued,
                                    _.Description,
                                }),
                        });
                }
            }
            else if (ListChallengeHandlers)
            {
                WriteVerbose("Listing all Challenge Type Handlers");
                WriteObject(ChallengeHandlerExtManager.GetProviderInfos().Select(_ => _.Name), true);
            }
            else
            {
                WriteVerbose("Getting details of preconfigured Challenge Handler Profile");
                using (var vlt = Util.VaultHelper.GetVault(VaultProfile))
                {
                    vlt.OpenStorage();
                    var v = vlt.LoadVault();

                    if (ListProfiles)
                    {
                        WriteObject(v.ProviderProfiles?.Values, true);
                    }
                    else
                    {
                        var ppi = v.ProviderProfiles?.GetByRef(ProfileRef, throwOnMissing: false);
                        if (ppi == null)
                        {
                            WriteObject(ppi);
                        }
                        else
                        {
                            var asset = vlt.GetAsset(Vault.VaultAssetType.ProviderConfigInfo,
                                    ppi.Id.ToString());
                            using (var s = vlt.LoadAsset(asset))
                            {
                                WriteObject(JsonHelper.Load<ProviderProfile>(s), false);
                            }
                        }
                    }
                }
            }
        }
    }
}
