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

        protected override void ProcessRecord()
        {
            using (var vp = InitializeVault.GetVaultProvider())
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
                        var fileMode = Repeat ? FileMode.Create : FileMode.CreateNew;

                        var crtDerFile = $"{ci.Id}-crt.der";
                        var crtPemFile = $"{ci.Id}-crt.pem";

                        using (var fs = new FileStream(crtDerFile, fileMode))
                        {
                            ci.CertificateRequest.SaveCertificate(fs);
                            ci.CrtDerFile = crtDerFile;
                        }

                        CsrHelper.Crt.ConvertDerToPem(crtDerFile, crtPemFile, fileMode);
                        ci.CrtPemFile = crtPemFile;
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

                                    var cacert = X509Certificate.CreateFromCertFile(tmp);
                                    var sernum = cacert.GetSerialNumberString();

                                    if (v.IssuerCertificates == null)
                                        v.IssuerCertificates = new IndexedDictionary<string, IssuerCertificateInfo>();
                                    if (!v.IssuerCertificates.ContainsKey(sernum))
                                    {
                                        var cacertDerFile = $"ca-{sernum}-crt.der";
                                        var cacertPemFile = $"ca-{sernum}-crt.pem";

                                        if (Repeat || !File.Exists(cacertDerFile))
                                            File.Copy(tmp, cacertDerFile);
                                        if (Repeat || !File.Exists(cacertPemFile))
                                            CsrHelper.Crt.ConvertDerToPem(cacertDerFile, cacertPemFile);

                                        v.IssuerCertificates[sernum] = new IssuerCertificateInfo
                                        {
                                            SerialNumber = sernum,
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
