using LetsEncrypt.ACME.JOSE;
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

        public OrderedDictionary Registrations
        { get; set; }

        public OrderedDictionary Identifiers
        { get; set; }

        public OrderedDictionary Certificates
        { get; set; }
    }
}
