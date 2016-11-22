using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Vault.Providers
{
    public class FileVaultAsset : VaultAsset
    {
        public FileVaultAsset(string path, string name, VaultAssetType type, bool isSensitive)
        {
            Path = path;
            Name = name;
            Type = type;
            IsSensitive = isSensitive;
        }

        public string Path
        { get; set; }
    }
}
