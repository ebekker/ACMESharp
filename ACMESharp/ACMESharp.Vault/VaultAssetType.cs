using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Vault
{

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

        /// <summary>
        /// An InstallerProfileInfofile to instantiate and
        /// configure a Provider for installing a certificate.
        /// </summary>
        InstallerConfigInfo,
    }
}
