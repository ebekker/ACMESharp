using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ACMESharp.POSH.Util;
using ACMESharp.POSH.Vault;
using System.Management.Automation;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsData.Initialize, "Vault", DefaultParameterSetName = PSET_BASE_SERVICE)]
    public class InitializeVault : Cmdlet
    {
        public const string PSET_BASE_SERVICE = "BaseService";
        public const string PSET_BASE_URI = "BaseURI";

        public const string WELL_KNOWN_LE = "LetsEncrypt";
        public const string WELL_KNOWN_LESTAGE = "LetsEncrypt-STAGING";

        public static readonly IReadOnlyDictionary<string, string> WELL_KNOWN_BASE_SERVICES =
                new ReadOnlyDictionary<string, string>(new IndexedDictionary<string, string>
                {
                    { WELL_KNOWN_LE, "https://acme-v01.api.letsencrypt.org/" },
                    { WELL_KNOWN_LESTAGE, "https://acme-staging.api.letsencrypt.org/"},
                });

        [Parameter(ParameterSetName = PSET_BASE_SERVICE)]
        [ValidateSet(
                WELL_KNOWN_LE,
                WELL_KNOWN_LESTAGE,
                IgnoreCase = true)]
        public string BaseService
        { get; set; } = WELL_KNOWN_LE;

        [Parameter(ParameterSetName = PSET_BASE_URI, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string BaseUri
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
            var baseUri = BaseUri;
            if (string.IsNullOrEmpty(baseUri))
                if (!string.IsNullOrEmpty(BaseService)
                        && WELL_KNOWN_BASE_SERVICES.ContainsKey(BaseService))
                    baseUri = WELL_KNOWN_BASE_SERVICES[BaseService];
                else
                    throw new PSInvalidOperationException("either a base service or URI is required");

            using (var vp = GetVaultProvider(VaultProfile))
            {
                vp.InitStorage(Force);
                var v = new VaultConfig
                {
                    Id = EntityHelper.NewId(),
                    Alias = Alias,
                    Label = Label,
                    Memo = Memo,
                    BaseURI = baseUri,
                    ServerDirectory = new AcmeServerDirectory()
                };

                vp.SaveVault(v);
            }
        }

        // TODO:  this routine doesn't belong here
        public static IVaultProvider GetVaultProvider(string profile, string provider = null)
        {
            // TODO: implement provider resolution
            var vp = new FileVaultProvider();
            vp.VaultProfile = profile;
            vp.Init();
            return vp;
        }
    }
}
