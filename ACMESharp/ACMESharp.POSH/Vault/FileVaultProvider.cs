﻿using ACMESharp.POSH.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace ACMESharp.POSH.Vault
{
    public class FileVaultProvider : IVaultProvider
    {
        public const string TAG_VERS = "1.0";
        public const string TAG_FILE = ".acme.vault";

        public const string VAULT  /**/ = "00-VAULT";
        public const string PRVDR  /**/ = "10-PRVDR";
        public const string CSRDT  /**/ = "30-CSRDT";
        public const string KEYGN  /**/ = "40-KEYGN";
        public const string KEYPM  /**/ = "45-KEYPM";
        public const string CSRGN  /**/ = "50-CSRGN";
        public const string CSRPM  /**/ = "55-CSRPM";
        public const string CSRDR  /**/ = "56-CSRDR";
        public const string CRTPM  /**/ = "65-CRTPM";
        public const string CRTDR  /**/ = "66-CRTDR";
        public const string ISUPM  /**/ = "75-ISUPM";
        public const string ISUDR  /**/ = "76-ISUDR";
        public const string ASSET  /**/ = "99-ASSET";

        public static readonly IReadOnlyDictionary<VaultAssetType, string> TYPE_PATHS =
                new ReadOnlyDictionary<VaultAssetType, string>(
                        new Dictionary<VaultAssetType, string>
                        {
                            { VaultAssetType.Other,               /**/ ASSET },
                            { VaultAssetType.ProviderConfigInfo,  /**/ PRVDR },
                            { VaultAssetType.CsrDetails,          /**/ CSRDT },
                            { VaultAssetType.KeyGen,              /**/ KEYGN },
                            { VaultAssetType.CsrGen,              /**/ CSRGN },
                            { VaultAssetType.KeyPem,              /**/ KEYPM },
                            { VaultAssetType.CsrPem,              /**/ CSRPM },
                            { VaultAssetType.CsrDer,              /**/ CSRDR },
                            { VaultAssetType.CrtPem,              /**/ CRTPM },
                            { VaultAssetType.CrtDer,              /**/ CRTDR },
                            { VaultAssetType.IssuerPem,           /**/ ISUPM },
                            { VaultAssetType.IssuerDer,           /**/ ISUDR },
                        });

        private string _origCwd;

        private string _tagFile;
        private string _vaultFile;
        private EntityMeta<VaultConfig> _vaultMeta;

        public string VaultProfile
        { get; set; }

        public string RootPath
        { get; set; }

        public bool IsDisposed
        { get; private set; }

        public bool IsOpen
        { get; private set; }

        public void Init()
        {
            // TODO:  the Vault path resolution will require a little more thought
            // and investigation, such as:
            //    http://stackoverflow.com/a/8506768
            //
            // NOTE:  THIS HAS SIDE EFFECTS UNTIL WE DISPOSE!!!
            // We're obviously changing the CWD here so this is altering the current
            // user's session state during the life of this VaultProvider instance
            // until we dispose and restore the CWD to the original.
            //
            // A better approach will be to have all the cmdlets
            // interact directly with the VaultProvider for any "file I/O" which we'll
            // need to do anyway in order to support non-file-based Vaults, such as ones
            // that are stored in SQL Server or some other server storage (NoSQL).

            var ss = new SessionState();
            var psCwd = ss.Path.CurrentFileSystemLocation.Path;

            if (string.IsNullOrEmpty(RootPath))
                RootPath = VaultProfile;
            if (string.IsNullOrEmpty(RootPath))
                RootPath = psCwd;

            // Optional Fix , Allows VaultProfile to be a full path and not relative to the current working path.
            if (Path.IsPathRooted(RootPath) == false)
                RootPath = Path.GetFullPath(Path.Combine(psCwd, RootPath));

            // If the Root and CWD aren't the same, we need to
            // temporarily move the CWD while we're working
            var cwd = Environment.CurrentDirectory;
            if (cwd != RootPath)
            {
                _origCwd = cwd;
                Environment.CurrentDirectory = RootPath;
            }

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

            // Create a backup just in case there's any fault
            if (File.Exists(_vaultFile))
                File.Copy(_vaultFile, $"{_vaultFile}.bak", true);
            // Sort of a 2-phase commit
            var tmp = $"{_vaultFile}.tmp{DateTime.Now.ToFileTime()}";
            using (var s = new FileStream(tmp, FileMode.Create))
            {
                JsonHelper.Save(s, _vaultMeta);
            }
            // Now commit the changes
            File.Copy(tmp, _vaultFile, true);
            File.Delete(tmp);
        }

        public IEnumerable<VaultAsset> ListAssets(string nameRegex = null, params VaultAssetType[] types)
        {
            AssertOpen();

            if (types?.Length == 0)
                types = Enum.GetValues(typeof(VaultAssetType)).Cast<VaultAssetType>().ToArray();

            Regex regex = null;
            if (!string.IsNullOrEmpty(nameRegex))
                regex = new Regex(nameRegex);

            var assets = new List<VaultAsset>();
            foreach (var vat in types)
            {
                var vatPath = Path.Combine(RootPath, TYPE_PATHS[vat]);
                if (Directory.Exists(vatPath))
                {
                    var vatFiles = (IEnumerable<string>)Directory.GetFiles(vatPath);
                    if (regex != null)
                        vatFiles = vatFiles.Where(x => regex.IsMatch(x));

                    assets.AddRange(vatFiles.Select(x => new FileVaultAsset(
                            x, Path.GetFileName(x), vat,
                            File.GetAttributes(x).HasFlag(FileAttributes.Encrypted))));
                }
            }

            return assets;
        }

        public VaultAsset CreateAsset(VaultAssetType type, string name, bool isSensitive = false)
        {
            var path = Path.Combine(RootPath, TYPE_PATHS[type], name);

            if (File.Exists(path))
                throw new IOException("asset file already exists");

            // Make sure the asset root dir is there
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            // Create a placeholder file to reserve and represent the created file
            using (var fs = File.Create(path, 100,
                    isSensitive ? FileOptions.Encrypted : FileOptions.None))
            { }

            return new FileVaultAsset(path, name, type, isSensitive);
        }

        public VaultAsset GetAsset(VaultAssetType type, string name)
        {
            var path = Path.Combine(RootPath, TYPE_PATHS[type], name);

            if (!File.Exists(path))
                throw new FileNotFoundException("asset file does not exist");

            return new FileVaultAsset(path, name, type,
                    File.GetAttributes(path).HasFlag(FileAttributes.Encrypted));
        }

        public Stream SaveAsset(VaultAsset asset)
        {
            var va = (FileVaultAsset)asset;

            return new FileStream(va.Path, FileMode.Create);
        }

        public Stream LoadAsset(VaultAsset asset)
        {
            var va = (FileVaultAsset)asset;

            return new FileStream(va.Path, FileMode.Open);
        }

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(_origCwd))
                Environment.CurrentDirectory = _origCwd;

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
}
