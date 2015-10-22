using System;

namespace LetsEncrypt.ACME.POSH.Vault
{
    public interface IVaultProvider : IDisposable
    {
        bool IsDisposed
        { get; }

        bool IsOpen
        { get; }

        string VaultProfile
        { set; }

        void Init();

        void InitStorage(bool force = false);

        void OpenStorage(bool initOrOpen = false);

        VaultConfig LoadVault(bool required = true);

        void SaveVault(VaultConfig vault);
    }
}
