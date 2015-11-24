using System;
using System.Collections.Generic;
using System.IO;

namespace ACMESharp.POSH.Vault
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

        IEnumerable<VaultAsset> ListAssets(string nameRegex = null, params VaultAssetType[] type);

        VaultAsset CreateAsset(VaultAssetType type, string name, bool isSensitive = false);

        VaultAsset GetAsset(VaultAssetType type, string name);

        Stream SaveAsset(VaultAsset asset);

        Stream LoadAsset(VaultAsset asset);
    }

    public enum VaultAssetType
    {
        Other = 0,

        /// <summary>
        /// A DnsInfo or WebServerInfo file to instantiate and
        /// configure a Provider for handling a Challenge.
        /// </summary>
        ProviderConfigInfo,

        /// <summary>
        /// Stores intermediate details when generating a CSR.
        /// </summary>
        CsrDetails,

        /// <summary>
        /// Imported or generated private key PEM file.
        /// </summary>
        KeyPem,
        /// <summary>
        /// Imported or generated CSR PEM file.
        /// </summary>
        CsrPem,

        /// <summary>
        /// Generated private key full details.
        /// </summary>
        KeyGen,
        /// <summary>
        /// Generated CSR full details.
        /// </summary>
        CsrGen,

        /// <summary>
        /// DER-encoded form of CSR (used directly in the ACME protocol).
        /// </summary>
        CsrDer,

        /// <summary>
        /// DER-encoded form of the issued cert (returned from CA as per ACME spec).
        /// </summary>
        CrtDer,
        /// <summary>
        /// PEM-encoded form of the issued cert.
        /// </summary>
        CrtPem,

        IssuerDer,
        IssuerPem,
    }

    public class VaultAsset
    {
        public virtual string Name
        { get; protected set; }

        public virtual VaultAssetType Type
        { get; protected set; }

        public virtual bool IsSensitive
        { get; protected set; }
    }
}
