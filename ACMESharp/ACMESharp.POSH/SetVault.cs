using ACMESharp.POSH.Util;
using System.Management.Automation;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsCommon.Set, "Vault", DefaultParameterSetName = PSET_BASE_SERVICE)]
    public class SetVault : Cmdlet
    {
        public const string PSET_BASE_SERVICE = "BaseService";
        public const string PSET_BASE_URI = "BaseURI";

        [Parameter(ParameterSetName = PSET_BASE_SERVICE)]
        [ValidateSet(
                InitializeVault.WELL_KNOWN_LE,
                InitializeVault.WELL_KNOWN_LESTAGE,
                IgnoreCase = true)]
        public string BaseService
        { get; set; }

        [Parameter(ParameterSetName = PSET_BASE_URI, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string BaseUri
        { get; set; }

        [Parameter]
        [ValidateSet("RSA256")]
        public string Signer
        { get; set; }

        [Parameter]
        public bool Force
        { get; set; }

        [Parameter]
        public string Alias
        { get; set; }

        [Parameter]
        public string Label
        { get; set; }

        [Parameter]
        public string Memo
        { get; set; }

        [Parameter]
        public string VaultProfile
        { get; set; }

        protected override void ProcessRecord()
        {
            using (var vp = InitializeVault.GetVaultProvider(VaultProfile))
            {
                vp.OpenStorage(Force);
                var v = vp.LoadVault();

                var baseUri = BaseUri;
                if (string.IsNullOrEmpty(baseUri) && !string.IsNullOrEmpty(BaseService))
                    baseUri = InitializeVault.WELL_KNOWN_BASE_SERVICES[BaseService];

                v.Alias = StringHelper.IfNullOrEmpty(Alias, v.Alias);
                v.Label = StringHelper.IfNullOrEmpty(Label, v.Label);
                v.Memo = StringHelper.IfNullOrEmpty(Memo, v.Memo);
                v.BaseURI = StringHelper.IfNullOrEmpty(baseUri, v.BaseURI);

                vp.SaveVault(v);
            }
        }
    }
}
