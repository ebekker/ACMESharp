using ACMESharp.Ext;
using ACMESharp.Util;
using ACMESharp.Vault;
using ACMESharp.Vault.Profile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.POSH.Util
{
    public static class VaultHelper
    {
        static VaultHelper()
        {
            // We have to override the base directory used to search for
            // Extension assemblies because under PowerShell, the base
            // directory happens to be where the PowerShell binary is
            // running from, not where the ACMESharp PS Module lives

            var baseUri = new Uri(typeof(VaultExtManager).Assembly.CodeBase);
            var baseDir = Path.GetDirectoryName(baseUri.AbsolutePath);

            ExtCommon.BaseDirectoryOverride = baseDir;
            ExtCommon.RelativeSearchPathOverride = string.Empty;
        }

        public static IVault GetVault(string profileName = null)
        {
            profileName = VaultProfileManager.ResolveProfileName(profileName);
            if (string.IsNullOrEmpty(profileName))
                throw new InvalidOperationException("unable to resolve effective profile name");

            var profile = VaultProfileManager.GetProfile(profileName);
            if (profile == null)
                throw new InvalidOperationException("unable to resolve effective profile")
                        .With(nameof(profileName), profileName);

            var provider = VaultExtManager.GetProvider(profile.ProviderName, null);
            if (provider == null)
                throw new InvalidOperationException("unable to resolve Vault Provider")
                        .With(nameof(profileName), profileName)
                        .With(nameof(profile.ProviderName), profile.ProviderName);

            return provider.GetVault(profile.VaultParameters);
        }
    }
}
