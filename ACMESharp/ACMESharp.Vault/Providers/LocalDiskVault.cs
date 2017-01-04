using ACMESharp.Util;
using ACMESharp.Vault.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ACMESharp.Vault.Providers
{
    public class LocalDiskVault : IVault
    {
        #region -- Constants --

        public const string TAG_VERS = "1.0";
        public const string TAG_FILE = ".acme.vault";

        public const string VAULT  /**/ = "00-VAULT"; // Vault Info
        public const string MTADT  /**/ = "01-MTADT"; // Asset Meta Data

        public const string PRVDR  /**/ = "10-PRVDR"; // Challenge Handler Provider Profile
        public const string INSTP  /**/ = "18-INSTP"; // Installer Provider Profile
        public const string CSRDT  /**/ = "30-CSRDT"; // CSR Generation Details
        public const string KEYGN  /**/ = "40-KEYGN"; // Private Key Generation Details
        public const string KEYPM  /**/ = "45-KEYPM"; // Private Key PEM Export
        public const string CSRGN  /**/ = "50-CSRGN"; // CSR Export
        public const string CSRPM  /**/ = "55-CSRPM"; // CSR PEM Export
        public const string CSRDR  /**/ = "56-CSRDR"; // CSR DER Export
        public const string CRTPM  /**/ = "65-CRTPM"; // Certificate PEM Export
        public const string CRTDR  /**/ = "66-CRTDR"; // Certificate DER Export
        public const string ISUPM  /**/ = "75-ISUPM"; // Issuer Certificate PEM Export
        public const string ISUDR  /**/ = "76-ISUDR"; // Issuer Certificate DER Export
        public const string ASSET  /**/ = "99-ASSET"; // Generic Asset

        public static readonly IReadOnlyDictionary<VaultAssetType, string> TYPE_PATHS =
                new ReadOnlyDictionary<VaultAssetType, string>(
                        new Dictionary<VaultAssetType, string>
                        {
                            [VaultAssetType.Other]               /**/ = ASSET,
                            [VaultAssetType.ProviderConfigInfo]  /**/ = PRVDR,
                            [VaultAssetType.CsrDetails]          /**/ = CSRDT,
                            [VaultAssetType.KeyGen]              /**/ = KEYGN,
                            [VaultAssetType.CsrGen]              /**/ = CSRGN,
                            [VaultAssetType.KeyPem]              /**/ = KEYPM,
                            [VaultAssetType.CsrPem]              /**/ = CSRPM,
                            [VaultAssetType.CsrDer]              /**/ = CSRDR,
                            [VaultAssetType.CrtPem]              /**/ = CRTPM,
                            [VaultAssetType.CrtDer]              /**/ = CRTDR,
                            [VaultAssetType.IssuerPem]           /**/ = ISUPM,
                            [VaultAssetType.IssuerDer]           /**/ = ISUDR,
                            [VaultAssetType.InstallerConfigInfo] /**/ = INSTP,
                        });

        #endregion -- Constants --

        #region -- Fields --

        private string _tagFile;
        private string _vaultFile;
        private EntityMeta<VaultInfo> _vaultMeta;

        #endregion -- Fields --

        #region -- Properties --

        public string RootPath
        { get; set; }

        public bool CreatePath
        { get; set; }

        public bool BypassEFS
        { get; set; }

        public bool IsDisposed
        { get; private set; }

        public bool IsStorageOpen
        { get; private set; }

        #endregion -- Properties --

        #region -- Methods --

        public void Init()
        {
            _tagFile = Path.Combine(RootPath, TAG_FILE);
            _vaultFile = Path.Combine(RootPath, VAULT);
        }

        public bool TestStorage()
        {
            return Directory.Exists(RootPath)
                    && File.Exists(_tagFile)
                    && File.Exists(_vaultFile);
        }

        public void InitStorage(bool force)
        {
            AssertNotDisposed();

            if (!Directory.Exists(RootPath))
            {
                if (CreatePath)
                    Directory.CreateDirectory(RootPath);
                else
                    throw new DirectoryNotFoundException("Root Path not found")
                            .With(nameof(RootPath), RootPath)
                            .With(nameof(CreatePath), CreatePath);
            }

            if (!force)
            {
                if (File.Exists(_tagFile) || File.Exists(_vaultFile))
                    throw new Exception("Vault root path contains existing vault data")
                            .With(nameof(RootPath), RootPath)
                            .With(nameof(force), force);

                var existingDir = Directory.GetFileSystemEntries(RootPath);
                if (existingDir?.Length > 0)
                    throw new Exception("Vault root path is not empty");
            }

            File.WriteAllText(_tagFile, TAG_VERS);

            IsStorageOpen = true;
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
                    _vaultMeta = JsonHelper.Load<EntityMeta<VaultInfo>>(s);
                }
            }

            IsStorageOpen = true;
        }

        public VaultInfo LoadVault(bool required)
        {
            AssertOpen();

            if (required && (_vaultMeta == null || _vaultMeta.Entity == null))
                throw new InvalidOperationException("Vault has not been initialized");

            return _vaultMeta?.Entity;
        }

        public void SaveVault(VaultInfo vault)
        {
            AssertOpen();

            var now = DateTime.Now;
            var who = $"{Environment.UserDomainName}\\{Environment.UserName}";
            if (_vaultMeta == null)
                _vaultMeta = new EntityMeta<VaultInfo>
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
                JsonHelper.Save(s, _vaultMeta, false);
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

        public VaultAsset CreateAsset(VaultAssetType type, string name, bool isSensitive = false,
                bool getOrCreate = false)
        {
            if (!TYPE_PATHS.ContainsKey(type))
                throw new NotSupportedException("unknown or unsupported asset type")
                        .With(nameof(VaultAssetType), type);

            var path = Path.Combine(RootPath, TYPE_PATHS[type], name);

            if (!File.Exists(path))
            {
                // Make sure the asset root dir is there
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                var fileOpts = FileOptions.None;
                if (isSensitive && !BypassEFS)
                    fileOpts = FileOptions.Encrypted;

                // Create a placeholder file to reserve and represent the created file
                using (var fs = File.Create(path, 100, fileOpts))
                { }
            }
            else if (!getOrCreate)
                throw new IOException("asset file already exists");

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
            IsDisposed = true;
            IsStorageOpen = false;
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

            if (!IsStorageOpen)
            {
                throw new InvalidOperationException("Vault is not open");
            }
        }

        #endregion -- Methods --
    }
}
