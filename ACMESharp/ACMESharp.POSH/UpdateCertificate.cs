using ACMESharp.POSH.Util;
using ACMESharp.Vault;
using ACMESharp.Vault.Model;
using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using ACMESharp.HTTP;
using ACMESharp.PKI;
using ACMESharp.Vault.Util;
using ACMESharp.Util;

namespace ACMESharp.POSH
{
    /// <summary>
    /// <para type="synopsis">
    ///   Updates the status and details of a Certificate stored in the Vault.
    /// </para>
    /// <para type="description">
    ///   Use this cmdlet to update characteristics of an Identifier that are
    ///   defined locally, such as the Alias or Label.
    /// </para>
    /// <para type="description">
    ///   Also use this cmdlet to refresh the state and status of a Certificate
    ///   including retrieving the certificate and intermediate signing certificate
    ///   from the associated ACME CA Server.
    /// </para>
    /// <para type="link">New-Certificate</para>
    /// <para type="link">Get-Certificate</para>
    /// <para type="link">Submit-Certificate</para>
    /// </summary>
    [Cmdlet(VerbsData.Update, "Certificate", DefaultParameterSetName = PSET_DEFAULT)]
    [OutputType(typeof(CertificateInfo))]
    public class UpdateCertificate : Cmdlet
    {
        public const string PSET_DEFAULT = "Default";
        public const string PSET_LOCAL_ONLY = "LocalOnly";

        /// <summary>
        /// <para type="description">
        ///     A reference (ID or alias) to a previously defined Certificate request.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [Alias("Ref")]
        public string CertificateRef
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///     Overrides the base URI associated with the target Registration and used
        ///     for subsequent communication with the associated ACME CA Server.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_DEFAULT)]
        public SwitchParameter UseBaseUri
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   When specified, this flag instructs the cmdlet to repeat the retrieval of
        ///   the issued certificate and related artifacts (e.g. intermediate signing cert).
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_DEFAULT)]
        public SwitchParameter Repeat
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   Indicates that updates should be performed locally only, and no attempt
        ///   should be made to retrieve the current status from the ACME CA Server.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = PSET_LOCAL_ONLY)]
        public SwitchParameter LocalOnly
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   Optionaly, set or update the unique alias assigned to the Certificate
        ///   for future reference.  To remove the alias, use the empty string.
        /// </para>
        /// </summary>
        [Parameter]
        public string NewAlias
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   Optionally, set or update the human-friendly label to assigned to the
        ///   Certificate for easy recognition.
        /// </para>
        /// </summary>
        [Parameter]
        public string Label
        { get; set; }

        /// <summary>
        /// <para type="description">
        ///   Optionall, set or update the arbitrary text field used to capture any
        ///   notes or details associated with the Certificate.
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

                if (v.Certificates == null || v.Certificates.Count < 1)
                    throw new InvalidOperationException("No certificates found");

                var ci = v.Certificates.GetByRef(CertificateRef, throwOnMissing: false);
                if (ci == null)
                    throw new Exception("Unable to find a Certificate for the given reference");

                // If we're renaming the Alias, do that
                // first in case there are any problems
                if (NewAlias != null)
                {
                    v.Certificates.Rename(CertificateRef, NewAlias);
                    ci.Alias = NewAlias == "" ? null : NewAlias;
                }

                if (!LocalOnly)
                {
                    if (ci.CertificateRequest == null)
                        throw new Exception("Certificate has not been submitted yet; cannot update status");

                    using (var c = ClientHelper.GetClient(v, ri))
                    {
                        c.Init();
                        c.GetDirectory(true);

                        c.RefreshCertificateRequest(ci.CertificateRequest, UseBaseUri);
                    }

                    if ((Repeat || string.IsNullOrEmpty(ci.CrtPemFile))
                            && !string.IsNullOrEmpty(ci.CertificateRequest.CertificateContent))
                    {
                        var crtDerFile = $"{ci.Id}-crt.der";
                        var crtPemFile = $"{ci.Id}-crt.pem";

                        var crtDerAsset = vlt.ListAssets(crtDerFile, VaultAssetType.CrtDer).FirstOrDefault();
                        var crtPemAsset = vlt.ListAssets(crtPemFile, VaultAssetType.CrtPem).FirstOrDefault();

                        if (crtDerAsset == null)
                            crtDerAsset = vlt.CreateAsset(VaultAssetType.CrtDer, crtDerFile);
                        if (crtPemAsset == null)
                            crtPemAsset = vlt.CreateAsset(VaultAssetType.CrtPem, crtPemFile);

                        using (var cp = PkiHelper.GetPkiTool(
                            StringHelper.IfNullOrEmpty(PkiTool, v.PkiTool)))
                        {
                            var bytes = ci.CertificateRequest.GetCertificateContent();

                            using (Stream source = new MemoryStream(bytes),
                                    derTarget = vlt.SaveAsset(crtDerAsset),
                                    pemTarget = vlt.SaveAsset(crtPemAsset))
                            {
                                var crt = cp.ImportCertificate(EncodingFormat.DER, source);

                                // We're saving the DER format cert "through"
                                // the CP in order to validate its content
                                cp.ExportCertificate(crt, EncodingFormat.DER, derTarget);
                                ci.CrtDerFile = crtDerFile;

                                cp.ExportCertificate(crt, EncodingFormat.PEM, pemTarget);
                                ci.CrtPemFile = crtPemFile;
                            }
                        }

                        var x509 = new X509Certificate2(ci.CertificateRequest.GetCertificateContent());
                        ci.SerialNumber = x509.SerialNumber;
                        ci.Thumbprint = x509.Thumbprint;
                        ci.SignatureAlgorithm = x509.SignatureAlgorithm?.FriendlyName;
                        ci.Signature = x509.GetCertHashString();
                    }

                    if (Repeat || string.IsNullOrEmpty(ci.IssuerSerialNumber))
                    {
                        var linksEnum = ci.CertificateRequest.Links;
                        if (linksEnum != null)
                        {
                            var links = new LinkCollection(linksEnum);
                            var upLink = links.GetFirstOrDefault("up");
                            if (upLink != null)
                            {
                                // We need to save the ICA certificate to a local
                                // temp file so that we can read it in and store
                                // it properly as a vault asset through a stream
                                var tmp = Path.GetTempFileName();
                                try
                                {
                                    using (var web = new WebClient())
                                    {
                                        if (v.Proxy != null)
                                            web.Proxy = v.Proxy.GetWebProxy();

                                        var uri = new Uri(new Uri(v.BaseUri), upLink.Uri);
                                        web.DownloadFile(uri, tmp);
                                    }

                                    var cacert = new X509Certificate2(tmp);
                                    var sernum = cacert.GetSerialNumberString();
                                    var tprint = cacert.Thumbprint;
                                    var sigalg = cacert.SignatureAlgorithm?.FriendlyName;
                                    var sigval = cacert.GetCertHashString();

                                    if (v.IssuerCertificates == null)
                                        v.IssuerCertificates = new OrderedNameMap<IssuerCertificateInfo>();
                                    if (Repeat || !v.IssuerCertificates.ContainsKey(sernum))
                                    {
                                        var cacertDerFile = $"ca-{sernum}-crt.der";
                                        var cacertPemFile = $"ca-{sernum}-crt.pem";
                                        var issuerDerAsset = vlt.ListAssets(cacertDerFile,
                                                VaultAssetType.IssuerDer).FirstOrDefault();
                                        var issuerPemAsset = vlt.ListAssets(cacertPemFile,
                                                VaultAssetType.IssuerPem).FirstOrDefault();

                                        if (Repeat || issuerDerAsset == null)
                                        {
                                            if (issuerDerAsset == null)
                                            issuerDerAsset = vlt.CreateAsset(VaultAssetType.IssuerDer, cacertDerFile);
                                                using (Stream fs = new FileStream(tmp, FileMode.Open),
                                                    s = vlt.SaveAsset(issuerDerAsset))
                                            {
                                                fs.CopyTo(s);
                                            }
                                        }
                                        if (Repeat || issuerPemAsset == null)
                                        {
                                            if (issuerPemAsset == null)
                                                issuerPemAsset = vlt.CreateAsset(VaultAssetType.IssuerPem, cacertPemFile);

                                            using (var cp = PkiHelper.GetPkiTool(
                                                StringHelper.IfNullOrEmpty(PkiTool, v.PkiTool)))
                                            {

                                                using (Stream source = vlt.LoadAsset(issuerDerAsset),
                                                    target = vlt.SaveAsset(issuerPemAsset))
                                                {
                                                    var crt = cp.ImportCertificate(EncodingFormat.DER, source);
                                                    cp.ExportCertificate(crt, EncodingFormat.PEM, target);
                                                }
                                            }
                                        }

                                        v.IssuerCertificates[sernum] = new IssuerCertificateInfo
                                        {
                                            SerialNumber = sernum,
                                            Thumbprint  = tprint,
                                            SignatureAlgorithm = sigalg,
                                            Signature = sigval,
                                            CrtDerFile = cacertDerFile,
                                            CrtPemFile = cacertPemFile,
                                        };
                                    }

                                    ci.IssuerSerialNumber = sernum;
                                }
                                finally
                                {
                                    if (File.Exists(tmp))
                                        File.Delete(tmp);
                                }
                            }
                        }
                    }
                }

                ci.Label = StringHelper.IfNullOrEmpty(Label);
                ci.Memo = StringHelper.IfNullOrEmpty(Memo);

                vlt.SaveVault(v);

                WriteObject(ci);
            }
        }
    }
}
