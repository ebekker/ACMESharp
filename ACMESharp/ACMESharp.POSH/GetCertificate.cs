using ACMESharp.Vault;
using ACMESharp.Vault.Model;
using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using ACMESharp.PKI;
using ACMESharp.Util;
using ACMESharp.PKI.RSA;

namespace ACMESharp.POSH
{
    /// <summary>
    /// <para type="synopsis">
    ///   Gets the status and details of a Certificate stored in the Vault.
    /// </para>
    /// <para type="description">
    ///   This cmdlet retrieves the details of a Certificate defined in the Vault.
    ///   It is also used to export various artificates associated with the Certificate
    ///   to various formats.
    /// </para>
    /// <para type="link">New-Certificate</para>
    /// <para type="link">Submit-Certificate</para>
    /// <para type="link">Update-Certificate</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Certificate", DefaultParameterSetName = PSET_LIST)]
    [OutputType(typeof(CertificateInfo))]
    public class GetCertificate : Cmdlet
    {
        public const string PSET_LIST = "List";
        public const string PSET_GET = "Get";

        /// <summary>
        /// <para type="description">
        ///     A reference (ID or alias) to a previously defined Certificate request.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = PSET_GET)]
        [Alias("Ref")]
        public string CertificateRef
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     Optionally, specifies a file path where the private key associated
        ///     with the referenced Certificate will be saved in PEM format.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_GET)]
        public string ExportKeyPEM
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     Optionally, specifies a file path where the CSR associated
        ///     with the referenced Certificate will be saved in PEM format.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_GET)]
        public string ExportCsrPEM
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     Optionally, specifies a file path where the referenced Certificate
        ///     will be saved in PEM format.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_GET)]
        public string ExportCertificatePEM
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     Optionally, specifies a file path where the referenced Certificate
        ///     will be saved in DER format.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_GET)]
        public string ExportCertificateDER
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     Optionally, specifies a file path where the referenced Issuer
        ///     Certificate will be saved in PEM format.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_GET)]
        public string ExportIssuerPEM
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     Optionally, specifies a file path where the referenced Issuer
        ///     Certificate will be saved in DER format.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_GET)]
        public string ExportIssuerDER
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     Optionally, specifies a file path where the referenced Certificate
        ///     and related artifacts will be saved into a PKCS#12 archive format.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_GET)]
        public string ExportPkcs12
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     Optionally, specifies a password to use to secure an exported
        ///     PKCS#12 archive file.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_GET)]
        public string CertificatePassword
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     This flag indicates that any existing files matching any of the
        ///     requested export parameter paths will be overwritten.  If not
        ///     specified, existing files will cause this cmdlet to error.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_GET)]
        public SwitchParameter Overwrite
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

        /// <summary>
        /// <para type="description">
        ///     Specifies a PKI tool provider (i.e. CertificateProvider) to be used in
        ///     all PKI related operations such as private key generation, CSR generation
        ///     and certificate importing and exporting.  If left unspecified a default
        ///     PKI tool provider will be used.
        /// </para>
        /// </summary>
        [Parameter]
        public string PkiTool
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

                if (string.IsNullOrEmpty(CertificateRef))
                {
                    int seq = 0;
                    WriteObject(v.Certificates.Values.Select(x => new
                    {
                        Seq = seq++,
                        x.Id,
                        x.Alias,
                        x.Label,
                        x.IdentifierDns,
                        x.Thumbprint,
                        x.SerialNumber,
                        x.IssuerSerialNumber,
                        x.CertificateRequest,
                        x.CertificateRequest?.StatusCode,
                    }), true);
                }
                else
                {
                    if (v.Certificates == null || v.Certificates.Count < 1)
                        throw new InvalidOperationException("No certificates found");

                    var ci = v.Certificates.GetByRef(CertificateRef, throwOnMissing: false);
                    if (ci == null)
                        throw new ItemNotFoundException("Unable to find a Certificate for the given reference");

                    var mode = Overwrite ? FileMode.Create : FileMode.CreateNew;

                    if (!string.IsNullOrEmpty(ExportKeyPEM))
                    {
                        if (string.IsNullOrEmpty(ci.KeyPemFile))
                            throw new InvalidOperationException("Cannot export private key; it hasn't been imported or generated");
                        CopyTo(vlt, VaultAssetType.KeyPem, ci.KeyPemFile, ExportKeyPEM, mode);
                    }

                    if (!string.IsNullOrEmpty(ExportCsrPEM))
                    {
                        if (string.IsNullOrEmpty(ci.CsrPemFile))
                            throw new InvalidOperationException("Cannot export CSR; it hasn't been imported or generated");
                        CopyTo(vlt, VaultAssetType.CsrPem, ci.CsrPemFile, ExportCsrPEM, mode);
                    }

                    if (!string.IsNullOrEmpty(ExportCertificatePEM))
                    {
                        if (ci.CertificateRequest == null || string.IsNullOrEmpty(ci.CrtPemFile))
                            throw new InvalidOperationException("Cannot export CRT; CSR hasn't been submitted or CRT hasn't been retrieved");
                        CopyTo(vlt, VaultAssetType.CrtPem, ci.CrtPemFile, ExportCertificatePEM, mode);
                    }

                    if (!string.IsNullOrEmpty(ExportCertificateDER))
                    {
                        if (ci.CertificateRequest == null || string.IsNullOrEmpty(ci.CrtDerFile))
                            throw new InvalidOperationException("Cannot export CRT; CSR hasn't been submitted or CRT hasn't been retrieved");
                        CopyTo(vlt, VaultAssetType.CrtDer, ci.CrtDerFile, ExportCertificateDER, mode);
                    }

                    if (!string.IsNullOrEmpty(ExportIssuerPEM))
                    {
                        if (ci.CertificateRequest == null || string.IsNullOrEmpty(ci.CrtPemFile))
                            throw new InvalidOperationException("Cannot export CRT; CSR hasn't been submitted or CRT hasn't been retrieved");
                        if (string.IsNullOrEmpty(ci.IssuerSerialNumber) || !v.IssuerCertificates.ContainsKey(ci.IssuerSerialNumber))
                            throw new InvalidOperationException("Issuer certificate hasn't been resolved");
                        CopyTo(vlt, VaultAssetType.IssuerPem,
                                v.IssuerCertificates[ci.IssuerSerialNumber].CrtPemFile,
                                ExportIssuerPEM, mode);
                    }

                    if (!string.IsNullOrEmpty(ExportIssuerDER))
                    {
                        if (ci.CertificateRequest == null || string.IsNullOrEmpty(ci.CrtDerFile))
                            throw new InvalidOperationException("Cannot export CRT; CSR hasn't been submitted or CRT hasn't been retrieved");
                        if (string.IsNullOrEmpty(ci.IssuerSerialNumber) || !v.IssuerCertificates.ContainsKey(ci.IssuerSerialNumber))
                            throw new InvalidOperationException("Issuer certificate hasn't been resolved");
                        CopyTo(vlt, VaultAssetType.IssuerDer,
                                v.IssuerCertificates[ci.IssuerSerialNumber].CrtDerFile,
                                ExportIssuerDER, mode);
                    }

                    if (!string.IsNullOrEmpty(ExportPkcs12))
                    {
                        if (string.IsNullOrEmpty(ci.KeyPemFile))
                            throw new InvalidOperationException("Cannot export PKCS12; private hasn't been imported or generated");
                        if (string.IsNullOrEmpty(ci.CrtPemFile))
                            throw new InvalidOperationException("Cannot export PKCS12; CSR hasn't been submitted or CRT hasn't been retrieved");
                        if (string.IsNullOrEmpty(ci.IssuerSerialNumber) || !v.IssuerCertificates.ContainsKey(ci.IssuerSerialNumber))
                            throw new InvalidOperationException("Cannot export PKCS12; Issuer certificate hasn't been resolved");

                        var keyPemAsset = vlt.GetAsset(VaultAssetType.KeyPem, ci.KeyPemFile);
                        var crtPemAsset = vlt.GetAsset(VaultAssetType.CrtPem, ci.CrtPemFile);
                        var isuPemAsset = vlt.GetAsset(VaultAssetType.IssuerPem,
                                v.IssuerCertificates[ci.IssuerSerialNumber].CrtPemFile);

                        using (var cp = Util.PkiHelper.GetPkiTool(
                            StringHelper.IfNullOrEmpty(PkiTool, v.PkiTool)))
                        {

                            using (Stream keyStream = vlt.LoadAsset(keyPemAsset),
                                crtStream = vlt.LoadAsset(crtPemAsset),
                                isuStream = vlt.LoadAsset(isuPemAsset),
                                fs = new FileStream(ExportPkcs12, mode))
                            {
                                var pk = cp.ImportPrivateKey<RsaPrivateKey>(EncodingFormat.PEM, keyStream);
                                var crt = cp.ImportCertificate(EncodingFormat.PEM, crtStream);
                                var isu = cp.ImportCertificate(EncodingFormat.PEM, isuStream);

                                var certs = new[] { crt, isu };

                                var password = string.IsNullOrWhiteSpace(CertificatePassword)
                                    ? string.Empty
                                    : CertificatePassword;

                                cp.ExportArchive(pk, certs, ArchiveFormat.PKCS12, fs, password);
                            }
                        }
                    }

                    WriteObject(ci);
                }
            }
        }

        public static void CopyTo(IVault vlt, VaultAssetType vat, string van, string target, FileMode mode)
        {
            var asset = vlt.GetAsset(vat, van);
            using (Stream s = vlt.LoadAsset(asset),
                    fs = new FileStream(target, mode))
            {
                s.CopyTo(fs);
            }
        }
    }
}
