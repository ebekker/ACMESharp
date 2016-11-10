using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Ext;
using ACMESharp.Vault.Model;

namespace ACMESharp.Vault
{
    public interface IVault : IDisposable
    {
        #region -- Properties --

        bool IsDisposed
        { get; }
    
        bool IsStorageOpen
        { get; }

        #endregion -- Properties --

        #region -- Methods --

        bool TestStorage();

        void InitStorage(bool force = false);
    
        void OpenStorage(bool initOrOpen = false);
    
        VaultInfo LoadVault(bool required = true);
    
        void SaveVault(VaultInfo vault);
    
        IEnumerable<VaultAsset> ListAssets(string nameRegex = null, params VaultAssetType[] type);
    
        VaultAsset CreateAsset(VaultAssetType type, string name, bool isSensitive = false,
                bool getOrCreate = false);
    
        VaultAsset GetAsset(VaultAssetType type, string name);
    
        Stream SaveAsset(VaultAsset asset);
    
        Stream LoadAsset(VaultAsset asset);

        #endregion -- Methods --
    }
}
