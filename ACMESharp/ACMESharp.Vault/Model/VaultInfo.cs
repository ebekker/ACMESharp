using System;
using ACMESharp.Vault.Util;

namespace ACMESharp.Vault.Model
{
    public class VaultInfo
    {
        public Guid Id
        { get; set; }

        public string Alias
        { get; set; }

        public string Label
        { get; set; }

        public string Memo
        { get; set; }

        public string BaseService
        { get; set; }

        public string BaseUri
        { get; set; }

        public string Signer
        { get; set; }

        public string PkiTool
        { get; set; }

        public bool GetInitialDirectory
        { get; set; } = true;

        public bool UseRelativeInitialDirectory
        { get; set; } = true;

        public AcmeServerDirectory ServerDirectory
        { get; set; }

        public ProxyConfig Proxy
        { get; set; }

        public EntityDictionary<ProviderProfileInfo> ProviderProfiles
        { get; set; }

        public EntityDictionary<InstallerProfileInfo> InstallerProfiles
        { get; set; }

        public EntityDictionary<RegistrationInfo> Registrations
        { get; set; }

        public EntityDictionary<IdentifierInfo> Identifiers
        { get; set; }

        public EntityDictionary<CertificateInfo> Certificates
        { get; set; }

        public OrderedNameMap<IssuerCertificateInfo> IssuerCertificates
        { get; set; }
    }
}
