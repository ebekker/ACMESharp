using LetsEncrypt.ACME.JOSE;
using LetsEncrypt.ACME.POSH.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH.Vault
{
    public class VaultConfig
    {
        public Guid Id
        { get; set; }

        public string Alias
        { get; set; }

        public string Label
        { get; set; }

        public string Memo
        { get; set; }

        public string BaseURI
        { get; set; }

        public bool GetInitialDirectory
        { get; set; } = true;

        public bool UseRelativeInitialDirectory
        { get; set; } = true;

        public AcmeServerDirectory ServerDirectory
        { get; set; }

        public ProxyConfig Proxy
        { get; set; }

        public EntityDictionary<ProviderConfig> ProviderConfigs
        { get; set; }

        public EntityDictionary<RegistrationInfo> Registrations
        { get; set; }

        public EntityDictionary<IdentifierInfo> Identifiers
        { get; set; }

        public EntityDictionary<CertificateInfo> Certificates
        { get; set; }

        public IndexedDictionary<string, IssuerCertificateInfo> IssuerCertificates
        { get; set; }
    }
}
