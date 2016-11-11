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
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace ACMESharp.POSH
{
    /// <summary>
    /// <para type="synopsis">
    ///   Initiates a request to issue a request for previously authorized Identifier.
    /// </para>
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
        ///   A reference (ID or alias) to a previously defined and authorized
        ///   Identifier verified by the ACME CA Server.
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
        ///       *  AlternativeNames; // X509 SAN Extension (manually overridden)
        /// </para>
        /// <para type="description">
        ///   For any elements that except multiple values (such as SAN), specify
        ///   a string of values separated by space, comma or semicolon
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_GENERATE, Mandatory = false)]
        public Hashtable CsrDetails
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   A collection of one or more references (ID or alias) to previously
        ///   defined and authorized Identifiers verified by the ACME CA Server
        ///   which will be included in the X509 extension for the list of
        ///   Subject Alternative Names (SAN).
        /// </para>
        /// <para type="description">
        ///   There is no need to repeat the reference to the primary common name
        ///   Identifier as it will be automatically included at the start of this list.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_GENERATE, Mandatory = false)]
        [Alias("AltIdentifiers", "AltRefs")]
        public IEnumerable<object> AlternativeIdentifierRefs
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

                var ii = v.Identifiers.GetByRef(IdentifierRef, throwOnMissing: false);
                if (ii == null)
                    throw new Exception("Unable to find an Identifier for the given reference");

                var ci = new CertificateInfo
                {
                    Id = EntityHelper.NewId(),
                    Alias = Alias,
                    Label = Label,
                    Memo = Memo,
                    IdentifierRef = ii.Id,
                    IdentifierDns = ii.Dns,
                };

                if (Generate)
                {
                    Func<string, string> csrDtlValue = x => null;
                    Func<string, IEnumerable<string>> csrDtlValues = x => null;

                    if (CsrDetails != null)
                    {
                        csrDtlValue = x => CsrDetails.ContainsKey(x)
                                ? CsrDetails[x] as string : null;
                        csrDtlValues = x => !string.IsNullOrEmpty(csrDtlValue(x))
                                ? Regex.Split(csrDtlValue(x).Trim(), "[\\s,;]+") : null;
                    }

                    var csrDetails = new CsrDetails
                    {
                        // Common Name is always pulled from associated Identifier
                        CommonName = ii.Dns,

                        // Remaining elements will be used if defined
                        AlternativeNames /**/ = csrDtlValues(nameof(PKI.CsrDetails.AlternativeNames)),
                        Country          /**/ = csrDtlValue(nameof(PKI.CsrDetails.Country          )),
                        Description      /**/ = csrDtlValue(nameof(PKI.CsrDetails.Description      )),
                        Email            /**/ = csrDtlValue(nameof(PKI.CsrDetails.Email            )),
                        GivenName        /**/ = csrDtlValue(nameof(PKI.CsrDetails.GivenName        )),
                        Initials         /**/ = csrDtlValue(nameof(PKI.CsrDetails.Initials         )),
                        Locality         /**/ = csrDtlValue(nameof(PKI.CsrDetails.Locality         )),
                        Organization     /**/ = csrDtlValue(nameof(PKI.CsrDetails.Organization     )),
                        OrganizationUnit /**/ = csrDtlValue(nameof(PKI.CsrDetails.OrganizationUnit )),
                        SerialNumber     /**/ = csrDtlValue(nameof(PKI.CsrDetails.SerialNumber     )),
                        StateOrProvince  /**/ = csrDtlValue(nameof(PKI.CsrDetails.StateOrProvince  )),
                        Surname          /**/ = csrDtlValue(nameof(PKI.CsrDetails.Surname          )),
                        Title            /**/ = csrDtlValue(nameof(PKI.CsrDetails.Title            )),
                        UniqueIdentifier /**/ = csrDtlValue(nameof(PKI.CsrDetails.UniqueIdentifier )),
                    };

                    if (AlternativeIdentifierRefs != null)
                    {
                        if (csrDetails.AlternativeNames != null)
                            throw new Exception("Alternative names already specified manually")
                                    .With(nameof(csrDetails.AlternativeNames),
                                            string.Join(",", csrDetails.AlternativeNames));

                        csrDetails.AlternativeNames = AlternativeIdentifierRefs.Select(alternativeIdentifierRef =>
                        {
                            var altId = v.Identifiers.GetByRef($"{alternativeIdentifierRef}", throwOnMissing: false);
                            if (altId == null)
                                throw new Exception("Unable to find an Identifier for the given Alternative Identifier reference")
                                        .With(nameof(alternativeIdentifierRef), alternativeIdentifierRef)
                                        .With(nameof(AlternativeIdentifierRefs),
                                                string.Join(",", AlternativeIdentifierRefs));
                            return altId.Dns;
                        });

                        ci.AlternativeIdentifierDns = csrDetails.AlternativeNames.ToArray();
                    }

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
                    using (Stream fs = new FileStream(CsrPemFile, FileMode.Open),
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
