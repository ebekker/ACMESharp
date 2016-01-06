using ACMESharp.POSH.Util;
using ACMESharp.Vault;
using ACMESharp.Vault.Model;
using System;
using System.IO;
using System.Management.Automation;
using ACMESharp.PKI;
using ACMESharp.Vault.Util;
using ACMESharp.Util;
using System.Collections;

namespace ACMESharp.POSH
{
    /// <summary>
    /// <para type="synopsis">
    ///   Initiates a request to issue a request for previously authorized Identifier.
    /// </paratype>
    /// <para type="description">
    ///   This cmdlet is used to request a new certificate for a DNS Identifier
    ///   that has already been verified by the ACME CA Server.  It is also used
    ///   to import, generate or define the certificate parameters and artifacts
    ///   needed for the request, such as the private key and CSR details.
    /// </para>
    /// <para type="link">New-Identifier</para>
    /// <para type="link">Complete-Challenge</para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "Certificate", DefaultParameterSetName = PSET_DEFAULT)]
    [OutputType(typeof(CertificateInfo))]
    public class NewCertificate : Cmdlet
    {
        public const string PSET_DEFAULT = "Default";
        public const string PSET_GENERATE = "Generate";

        /// <summary>
        /// <para type="description">
        ///     A reference (ID or alias) to a previously defined and authorized
        ///     Identifier verified by the ACME CA Server.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [Alias("Identifier", "Ref")]
        public string IdentifierRef
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   Specifies an existing private key in PEM file format that should be
        ///   used to generate the Certificate Request.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_DEFAULT, Mandatory = true)]
        public string KeyPemFile
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   Specifies an existing CSR in PEM file format containing all the
        ///   details of the Certificate that should be used to generate the
        ///   Certificate Request.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_DEFAULT, Mandatory = true)]
        public string CsrPemFile
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   Indicates that new Certificate Request parameters and artificats
        ///   should be generated.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_GENERATE, Mandatory = true)]
        public SwitchParameter Generate
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   An optional set of certificate details to be included in the
        ///   generated CSR.
        /// </para>
        /// <para type="description">
        ///   The common name will be set based on the DNS name of the associated
        ///   Identifier, however all other details will be specified as set in
        ///   this parameter.  The following elements are defined, however not all
        ///   of these may be supported or honored by the target ACME CA Server:
        ///   
        ///       *  Country;          // C;
        ///       *  StateOrProvince;  // ST;
        ///       *  Locality;         // L;
        ///       *  Organization;     // O;
        ///       *  OrganizationUnit; // OU;
        ///       *  Description;      // D;
        ///       *  Surname;          // S;
        ///       *  GivenName;        // G;
        ///       *  Initials;         // I;
        ///       *  Title;            // T;
        ///       *  SerialNumber;     // SN;
        ///       *  UniqueIdentifier; // UID;
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_GENERATE, Mandatory = false)]
        public Hashtable CsrDetails
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   An optional, unique alias to assign to the Certificate for future
        ///   reference.
        /// </para>
        /// </summary>
        [Parameter]
        public string Alias
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   An optional, human-friendly label to assign to the Certificate for
        ///   easy recognition.
        /// </para>
        /// </summary>
        [Parameter]
        public string Label
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   An optional, arbitrary text field to capture any notes or details
        ///   associated with the Certificate.
        /// </para>
        /// </summary>
        [Parameter]
        public string Memo
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     Specifies a Vault profile name that will resolve to the Vault instance to be
        ///     used for all related operations and storage/retrieval of all related assets.
        /// </para>
        /// </summary>
        [Parameter]
        public string VaultProfile
        { get; set; }

        protected override void ProcessRecord()
        {
            using (var vlt = Util.VaultHelper.GetVault(VaultProfile))
            {
                vlt.OpenStorage();
                var v = vlt.LoadVault();

                if (v.Registrations == null || v.Registrations.Count < 1)
                    throw new InvalidOperationException("No registrations found");

                var ri = v.Registrations[0];
                var r = ri.Registration;

                if (v.Identifiers == null || v.Identifiers.Count < 1)
                    throw new InvalidOperationException("No identifiers found");

                var ii = v.Identifiers.GetByRef(IdentifierRef);
                if (ii == null)
                    throw new Exception("Unable to find an Identifier for the given reference");

                var ci = new CertificateInfo
                {
                    Id = EntityHelper.NewId(),
                    Alias = Alias,
                    Label = Label,
                    Memo = Memo,
                    IdentifierRef = ii.Id,
                };

                if (Generate)
                {
                    Func<string, string> csrDtl = x => null;
                    if (CsrDetails != null)
                        csrDtl = x => CsrDetails.ContainsKey(x) ? x as string : null;

                    var csrDetails = new CsrDetails
                    {
                        // Common Name is always pulled from associated Identifier
                        CommonName = ii.Dns,

                        // Remaining elements will be used if defined
                        Country          /**/ = csrDtl(nameof(PKI.CsrDetails.Country          )),
                        Description      /**/ = csrDtl(nameof(PKI.CsrDetails.Description      )),
                        Email            /**/ = csrDtl(nameof(PKI.CsrDetails.Email            )),
                        GivenName        /**/ = csrDtl(nameof(PKI.CsrDetails.GivenName        )),
                        Initials         /**/ = csrDtl(nameof(PKI.CsrDetails.Initials         )),
                        Locality         /**/ = csrDtl(nameof(PKI.CsrDetails.Locality         )),
                        Organization     /**/ = csrDtl(nameof(PKI.CsrDetails.Organization     )),
                        OrganizationUnit /**/ = csrDtl(nameof(PKI.CsrDetails.OrganizationUnit )),
                        SerialNumber     /**/ = csrDtl(nameof(PKI.CsrDetails.SerialNumber     )),
                        StateOrProvince  /**/ = csrDtl(nameof(PKI.CsrDetails.StateOrProvince  )),
                        Surname          /**/ = csrDtl(nameof(PKI.CsrDetails.Surname          )),
                        Title            /**/ = csrDtl(nameof(PKI.CsrDetails.Title            )),
                        UniqueIdentifier /**/ = csrDtl(nameof(PKI.CsrDetails.UniqueIdentifier )),
                    };

                    ci.GenerateDetailsFile = $"{ci.Id}-gen.json";
                    var asset = vlt.CreateAsset(VaultAssetType.CsrDetails, ci.GenerateDetailsFile);
                    using (var s = vlt.SaveAsset(asset))
                    {
                        JsonHelper.Save(s, csrDetails);
                    }
                }
                else
                {
                    if (!File.Exists(KeyPemFile))
                        throw new FileNotFoundException("Missing specified RSA Key file path");
                    if (!File.Exists(CsrPemFile))
                        throw new FileNotFoundException("Missing specified CSR details file path");

                    var keyPemFile = $"{ci.Id}-key.pem";
                    var csrPemFile = $"{ci.Id}-csr.pem";

                    var keyAsset = vlt.CreateAsset(VaultAssetType.KeyPem, keyPemFile, true);
                    var csrAsset = vlt.CreateAsset(VaultAssetType.CsrPem, csrPemFile);

                    using (Stream fs = new FileStream(KeyPemFile, FileMode.Open),
                            s = vlt.SaveAsset(keyAsset))
                    {
                        fs.CopyTo(s);
                    }
                    using (Stream fs = new FileStream(KeyPemFile, FileMode.Open),
                            s = vlt.SaveAsset(csrAsset))
                    {
                        fs.CopyTo(s);
                    }

                    ci.KeyPemFile = keyPemFile;
                    ci.CsrPemFile = csrPemFile;
                }

                if (v.Certificates == null)
                    v.Certificates = new EntityDictionary<CertificateInfo>();

                v.Certificates.Add(ci);

                vlt.SaveVault(v);

                WriteObject(ci);
            }
        }
    }
}
