using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Vault.Profile
{
    public class InstallerProfile
    {
        public string InstallerProvider
        { get; set; }

        public IReadOnlyDictionary<string, object> InstanceParameters
        { get; set; }
    }
}
