using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Vault.Profile
{
    public enum ProviderType
    {
        CUSTOM = 0,

        VAULT = 1,
        CHALLENGE_DECODER = 2,
        CHALLENGE_HANDLER = 3,
        PKI = 4,
        INSTALLER = 5,
    }

    public class ProviderProfile
    {
        public ProviderType ProviderType
        { get; set; }

        public string ProviderCustomType
        { get; set; }

        public string ProviderName
        { get; set; }

        public IReadOnlyDictionary<string, object> ProviderParameters
        { get; set; }

        public IReadOnlyDictionary<string, object> InstanceParameters
        { get; set; }

        public IReadOnlyDictionary<string, object> ProfileParameters
        { get; set; }
    }
}
