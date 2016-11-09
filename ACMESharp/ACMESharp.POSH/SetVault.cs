using ACMESharp.POSH.Util;
using ACMESharp.Util;
using System.Management.Automation;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsCommon.Set, "Vault", DefaultParameterSetName = PSET_BASE_SERVICE)]
    public class SetVault : Cmdlet
    {
        public const string PSET_BASE_SERVICE = "BaseService";
        public const string PSET_BASE_URI = "BaseUri";

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

        /// <summary>
        /// <para type="description">
        ///     Specifies a PKI tool provider (i.e. CertificateProvider) to be used by
        ///     default in all subsequent operations against this vault.  In most cases
        ///     this can be overridden on a call-by-call basis but typically, all
        ///     PKI-related operations should be performed by a single PKI Tool provider
        ///     because of the internal workings of the provider and interdependencies
        ///     of the operations.  Such operations include private key generation,
        ///     CSR generation and certificate importing and exporting.
        ///     If left unspecified a default PKI tool provider will be used.
        /// </para>
        /// </summary>
        [Parameter]
        public string PkiTool
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
            using (var vlt = Util.VaultHelper.GetVault(VaultProfile))
            {
                vlt.OpenStorage(Force);
                var v = vlt.LoadVault();

                var baseUri = BaseUri;
                if (string.IsNullOrEmpty(baseUri) && !string.IsNullOrEmpty(BaseService))
                {
                    baseUri = InitializeVault.WELL_KNOWN_BASE_SERVICES[BaseService];
                    WriteVerbose($"Updating Base URI from Well Known Base Service [{baseUri}]");
                }

                WriteVerbose("Updating Vault settings");
                v.Alias = StringHelper.IfNullOrEmpty(Alias, v.Alias);
                v.Label = StringHelper.IfNullOrEmpty(Label, v.Label);
                v.Memo = StringHelper.IfNullOrEmpty(Memo, v.Memo);
                v.BaseService = StringHelper.IfNullOrEmpty(BaseService, v.BaseService);
                v.BaseUri = StringHelper.IfNullOrEmpty(baseUri, v.BaseUri);
                v.Signer = StringHelper.IfNullOrEmpty(Signer, v.Signer);
                v.PkiTool = StringHelper.IfNullOrEmpty(PkiTool, v.PkiTool);

                vlt.SaveVault(v);
            }
        }
    }
}
