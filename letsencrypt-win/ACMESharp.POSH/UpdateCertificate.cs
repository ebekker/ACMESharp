using LetsEncrypt.ACME.HTTP;
using LetsEncrypt.ACME.PKI;
using LetsEncrypt.ACME.POSH.Util;
using LetsEncrypt.ACME.POSH.Vault;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH
{
    [Cmdlet(VerbsData.Update, "Certificate", DefaultParameterSetName = PSET_DEFAULT)]
    [OutputType(typeof(CertificateInfo))]
    public class UpdateCertificate : Cmdlet
    {
        public const string PSET_DEFAULT = "Default";
        public const string PSET_LOCAL_ONLY = "LocalOnly";

        [Parameter(Mandatory = true)]
        public string Ref
        { get; set; }

        [Parameter(ParameterSetName = PSET_DEFAULT)]
        public SwitchParameter UseBaseURI
        { get; set; }

        [Parameter(ParameterSetName = PSET_DEFAULT)]
        public SwitchParameter Repeat
        { get; set; }

        [Parameter(ParameterSetName = PSET_LOCAL_ONLY)]
        public SwitchParameter LocalOnly
        { get; set; }

        [Parameter]
        public string Alias
        { get; set; }

        [Parameter]
        public string Label
        { get; set; }

        [Parameter]
        public string Memo
        { get; set; }

        [Parameter]
        public string VaultProfile
        { get; set; }

        protected override void ProcessRecord()
        {
            using (var vp = InitializeVault.GetVaultProvider(VaultProfile))
            {
                vp.OpenStorage();
                var v = vp.LoadVault();

                if (v.Registrations == null || v.Registrations.Count < 1)
                    throw new InvalidOperationException("No registrations found");

                var ri = v.Registrations[0];
                var r = ri.Registration;

                if (v.Certificates == null || v.Certificates.Count < 1)
                    throw new InvalidOperationException("No certificates found");

                var ci = v.Certificates.GetByRef(Ref);
                if (ci == null)
                    throw new Exception("Unable to find a Certificate for the given reference");

                if (!LocalOnly)
                {
                    if (ci.CertificateRequest == null)
                        throw new Exception("Certificate has not been submitted yet; cannot update status");

                    using (var c = ClientHelper.GetClient(v, ri))
                    {
                        c.Init();
                        c.GetDirectory(true);

                        c.RefreshCertificateRequest(ci.CertificateRequest, UseBaseURI);
                    }

                    if ((Repeat || string.IsNullOrEmpty(ci.CrtPemFile))
                            && !string.IsNullOrEmpty(ci.CertificateRequest.CertificateContent))
                    {
                        var crtDerFile = $"{ci.Id}-crt.der";
                        var crtPemFile = $"{ci.Id}-crt.pem";

                        var crtDerAsset = vp.ListAssets(crtDerFile, VaultAssetType.CrtDer).FirstOrDefault();
                        var crtPemAsset = vp.ListAssets(crtPemFile, VaultAssetType.CrtPem).FirstOrDefault();

                        if (crtDerAsset == null)
                            crtDerAsset = vp.CreateAsset(VaultAssetType.CrtDer, crtDerFile);
                        if (crtPemAsset == null)
                            crtPemAsset = vp.CreateAsset(VaultAssetType.CrtPem, crtPemFile);

                        using (var cp = CertificateProvider.GetProvider())
                        {
                            var bytes = ci.CertificateRequest.GetCertificateContent();

                            using (Stream source = new MemoryStream(bytes),
                                    derTarget = vp.SaveAsset(crtDerAsset),
                                    pemTarget = vp.SaveAsset(crtPemAsset))
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

                                        var uri = new Uri(new Uri(v.BaseURI), upLink.Uri);
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
                                        var issuerDerAsset = vp.ListAssets(cacertDerFile,
                                                VaultAssetType.IssuerDer).FirstOrDefault();
                                        var issuerPemAsset = vp.ListAssets(cacertPemFile,
                                                VaultAssetType.IssuerPem).FirstOrDefault();

                                        if (Repeat || issuerDerAsset == null)
                                        {
                                            if (issuerDerAsset == null)
                                            issuerDerAsset = vp.CreateAsset(VaultAssetType.IssuerDer, cacertDerFile);
                                                using (Stream fs = new FileStream(tmp, FileMode.Open),
                                                    s = vp.SaveAsset(issuerDerAsset))
                                            {
                                                fs.CopyTo(s);
                                            }
                                        }
                                        if (Repeat || issuerPemAsset == null)
                                        {
                                            if (issuerPemAsset == null)
                                                issuerPemAsset = vp.CreateAsset(VaultAssetType.IssuerPem, cacertPemFile);

                                            using (var cp = CertificateProvider.GetProvider())
                                            {

                                                using (Stream source = vp.LoadAsset(issuerDerAsset),
                                                    target = vp.SaveAsset(issuerPemAsset))
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

                v.Alias = StringHelper.IfNullOrEmpty(Alias);
                v.Label = StringHelper.IfNullOrEmpty(Label);
                v.Memo = StringHelper.IfNullOrEmpty(Memo);

                vp.SaveVault(v);

                WriteObject(ci);
            }
        }
    }
}
