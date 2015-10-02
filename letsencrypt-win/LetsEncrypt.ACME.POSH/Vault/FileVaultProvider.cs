using LetsEncrypt.ACME.POSH.Util;
using System;
using System.IO;

namespace LetsEncrypt.ACME.POSH.Vault
{
    public class FileVaultProvider : IVaultProvider
    {
        public const string TAG_VERS = "1.0";
        public const string TAG_FILE = ".acme.vault";

        public const string VAULT  /**/ = "00-VAULT";
        public const string REGS   /**/ = "10-REGS";
        public const string IDENTS /**/ = "20-IDENTS";
        public const string CERTS  /**/ = "30-CERTS";

        private string _tagFile;
        private string _vaultFile;
        private EntityMeta<VaultConfig> _vaultMeta;

        public string RootPath
        { get; set; }

        public bool IsDisposed
        { get; private set; }

        public bool IsOpen
        { get; private set; }

        public void Init()
        {
            if (string.IsNullOrEmpty(RootPath))
                RootPath = Environment.CurrentDirectory;
            RootPath = Path.GetFullPath(RootPath);

            _tagFile = Path.Combine(RootPath, TAG_FILE);
            _vaultFile = Path.Combine(RootPath, VAULT);
        }

        public void InitStorage(bool force)
        {
            AssertNotDisposed();

            if (!force)
            {
                if (File.Exists(_tagFile) || File.Exists(_vaultFile))
                    throw new Exception("Vault root path contains existing vault data");

                var existingDir = Directory.GetFileSystemEntries(RootPath);
                if (existingDir != null && existingDir.Length > 0)
                    throw new Exception("Vault root path is not empty");
            }

            File.WriteAllText(_tagFile, TAG_VERS);

            IsOpen = true;
        }

        public void OpenStorage(bool initOrOpen)
        {
            AssertNotDisposed();

            if (!File.Exists(_tagFile))
            {
                if (!initOrOpen)
                    throw new Exception("Vault root path does not contain vault data");

                InitStorage(false);
            }

            var tagVers = File.ReadAllText(_tagFile);
            if (TAG_VERS != tagVers)
                throw new Exception("Vault version mismatch");

            if (File.Exists(_vaultFile))
            {
                using (var s = new FileStream(_vaultFile, FileMode.Open))
                {
                    _vaultMeta = JsonHelper.Load<EntityMeta<VaultConfig>>(s);
                }
            }

            IsOpen = true;
        }

        public VaultConfig LoadVault(bool required)
        {
            AssertOpen();

            if (required && (_vaultMeta == null || _vaultMeta.Entity == null))
                throw new InvalidOperationException("Vault has not been initialized");

            return _vaultMeta?.Entity;
        }

        public void SaveVault(VaultConfig vault)
        {
            AssertOpen();

            var now = DateTime.Now;
            var who = $"{Environment.UserDomainName}\\{Environment.UserName}";
            if (_vaultMeta == null)
                _vaultMeta = new EntityMeta<VaultConfig>
                {
                    CreateDate = now,
                    CreateUser = who,
                    CreateHost = Environment.MachineName,
                };

            _vaultMeta.UpdateDate = now;
            _vaultMeta.UpdateUser = who;
            _vaultMeta.Entity = vault;

            using (var s = new FileStream(_vaultFile, FileMode.Create))
            {
                JsonHelper.Save(s, _vaultMeta);
            }
        }

        public void Dispose()
        {
            IsDisposed = true;
            IsOpen = false;
            RootPath = null;
        }

        private void AssertNotDisposed()
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException("Vault provider is disposed");
            }
        }

        private void AssertOpen()
        {
            AssertNotDisposed();

            if (!IsOpen)
            {
                throw new InvalidOperationException("Vault is not open");
            }
        }

        /// <summary>
        /// Basic wrapper around any entity that we save using this file-based
        /// provider in order to track common meta data about the entity.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class EntityMeta<T>
        {
            public DateTime CreateDate
            { get; set; }

            public string CreateUser
            { get; set; }

            public string CreateHost
            { get; set; }

            public DateTime UpdateDate
            { get; set; }

            public string UpdateUser
            { get; set; }

            public T Entity
            { get; set; }
        }
    }
}
