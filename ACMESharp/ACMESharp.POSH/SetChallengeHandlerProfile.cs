using ACMESharp.ACME;
using ACMESharp.POSH.Util;
using ACMESharp.Util;
using ACMESharp.Vault.Profile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsCommon.Set, "ChallengeHandlerProfile", DefaultParameterSetName = PSET_SET)]
    public class SetChallengeHandlerProfile : Cmdlet
    {
        public const string PSET_SET = "Set";
        public const string PSET_RENAME = "Rename";
        public const string PSET_REMOVE = "Remove";

        [Parameter(Mandatory = true, Position = 0)]
        public string ProfileName
        { get; set; }

        [Parameter(Mandatory = true, Position = 1, ParameterSetName = PSET_SET)]
        [ValidateSet(
                AcmeProtocol.CHALLENGE_TYPE_DNS,
                AcmeProtocol.CHALLENGE_TYPE_HTTP,
                IgnoreCase = true)]
        public string ChallengeType
        { get; set; }

        [Parameter(Mandatory = true, Position = 2, ParameterSetName = PSET_SET)]
        public string Handler
        { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = PSET_SET)]
        public string Label
        { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = PSET_SET)]
        public string Memo
        { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = PSET_SET)]
        public Hashtable HandlerParameters
        { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = PSET_RENAME)]
        public string Rename
        { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = PSET_REMOVE)]
        public SwitchParameter Remove
        { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force
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

                if (v.ProviderProfiles == null)
                {
                    WriteVerbose("Initializing Provider Profile collection");
                    v.ProviderProfiles = new Vault.Util.EntityDictionary<Vault.Model.ProviderProfileInfo>();
                }

                WriteVerbose($"Searching for existing Provider Profile for reference [{ProfileName}]");
                var ppi = v.ProviderProfiles.GetByRef(ProfileName, throwOnMissing: false);
                if (ppi == null)
                    WriteVerbose("No existing Profile found");
                else
                    WriteVerbose($"Existing Profile found [{ppi.Id}][{ppi.Alias}]");

                if (!string.IsNullOrEmpty(Rename))
                {
                    if (ppi == null)
                        throw new KeyNotFoundException("no existing profile found that can be renamed");

                    v.ProviderProfiles.Rename(ProfileName, Rename);
                    ppi.Alias = Rename;
                }
                else if (Remove)
                {
                    WriteVerbose($"Removing named Provider Profile for name [{ProfileName}]");
                    if (ppi == null)
                    {
                        WriteVerbose("No Provider Profile found for given name");
                        return;
                    }
                    else
                    {
                        v.ProviderProfiles.Remove(ppi.Id);
                        WriteVerbose("Provider Profile removed");
                    }
                }
                else
                {
                    if (ppi != null)
                    {
                        if (!Force)
                            throw new InvalidOperationException("existing profile found;"
                                    + " specify -Force to overwrite");

                        WriteVerbose("Removing existing Profile");
                        v.ProviderProfiles.Remove(ppi.Id);
                    }

                    if (ChallengeHandlerExtManager.GetProviderInfo(Handler) == null)
                    {
                        throw new ArgumentException("Unknown or invalid Handler provider name")
                                .With(nameof(Handler), Handler);
                    }

                    WriteVerbose("Adding new Provider Profile Info");
                    ppi = new Vault.Model.ProviderProfileInfo
                    {
                        Id = Guid.NewGuid(),
                        Alias = ProfileName,
                        Label = Label,
                        Memo = Memo,
                    };
                    var pp = new ProviderProfile
                    {
                        ProfileParameters = new Dictionary<string, object>
                                { [nameof(ChallengeType)] = ChallengeType, },
                        ProviderType = ProviderType.CHALLENGE_HANDLER,
                        ProviderName = Handler,
                        InstanceParameters = (IReadOnlyDictionary<string, object>
                                )HandlerParameters.Convert<string, object>(),
                    };

                    var asset = vlt.CreateAsset(Vault.VaultAssetType.ProviderConfigInfo, ppi.Id.ToString());
                    using (var s = vlt.SaveAsset(asset))
                    {
                        JsonHelper.Save(s, pp);
                    }
                    v.ProviderProfiles.Add(ppi);
                }

                vlt.SaveVault(v);
            }
        }
    }
}
