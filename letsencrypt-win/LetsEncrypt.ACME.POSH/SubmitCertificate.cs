using LetsEncrypt.ACME.JOSE;
using LetsEncrypt.ACME.PKI;
using LetsEncrypt.ACME.POSH.Util;
using LetsEncrypt.ACME.POSH.Vault;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH
{
    [Cmdlet(VerbsLifecycle.Submit, "Certificate")]
    [OutputType(typeof(CertificateInfo))]
    public class SubmitCertificate : Cmdlet
    {
        [Parameter(Mandatory = true)]
        public string Ref
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

                if (!string.IsNullOrEmpty(ci.GenerateDetailsFile))
                {
                    // Generate a private key and CSR:
                    //    Key:  RSA 2048-bit
                    //    MD:   SHA 256
                    //    CSR:  Details pulled from CSR Details JSON file

                    CsrHelper.CsrDetails csrDetails;
                    using (var fs = new FileStream(Path.GetFullPath(ci.GenerateDetailsFile),
                            FileMode.Open))
                    {
                        csrDetails = JsonHelper.Load<CsrHelper.CsrDetails>(fs);
                    }

                    var keyGenFile = $"{ci.Id}-gen-key.json";
                    var keyPemFile = $"{ci.Id}-key.pem";
                    var csrGenFile = $"{ci.Id}-gen-csr.json";
                    var csrPemFile = $"{ci.Id}-csr.pem";

                    var genKey = CsrHelper.GenerateRsaPrivateKey();
                    using (var fs = new FileStream(keyGenFile, FileMode.CreateNew))
                    {
                        genKey.Save(fs);
                        File.WriteAllText(keyPemFile, genKey.Pem);
                    }

                    var genCsr = CsrHelper.GenerateCsr(csrDetails, genKey);
                    using (var fs = new FileStream(csrGenFile, FileMode.CreateNew))
                    {
                        genCsr.Save(fs);
                        File.WriteAllText(csrPemFile, genCsr.Pem);
                    }

                    ci.KeyPemFile = keyPemFile;
                    ci.CsrPemFile = csrPemFile;
                }

                byte[] derRaw;
                using (var fs = new FileStream(ci.CsrPemFile, FileMode.Open))
                {
                    using (var ms = new MemoryStream())
                    {
                        CsrHelper.Csr.ConvertPemToDer(fs, ms);
                        derRaw = ms.ToArray();
                    }
                }

                var derB64u = JwsHelper.Base64UrlEncode(derRaw);

                using (var c = ClientHelper.GetClient(v, ri))
                {
                    c.Init();
                    c.GetDirectory(true);

                    ci.CertificateRequest = c.RequestCertificate(derB64u);
                }

                if (!string.IsNullOrEmpty(ci.CertificateRequest.CertificateContent))
                {
                    var crtDerFile = $"{ci.Id}-crt.der";
                    var crtPemFile = $"{ci.Id}-crt.pem";

                    using (var fs = new FileStream(crtDerFile, FileMode.CreateNew))
                    {
                        ci.CertificateRequest.SaveCertificate(fs);
                        ci.CrtDerFile = crtDerFile;
                    }

                    using (FileStream source = new FileStream(crtDerFile, FileMode.Open),
                            target = new FileStream(crtPemFile, FileMode.CreateNew))
                    {
                        CsrHelper.Crt.ConvertDerToPem(source, target);
                        ci.CrtPemFile = crtPemFile;
                    }
                }

                vp.SaveVault(v);

                WriteObject(ci);
            }
        }
    }
}
