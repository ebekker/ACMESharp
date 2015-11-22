using LetsEncrypt.ACME.JOSE;
using LetsEncrypt.ACME.PKI;
using LetsEncrypt.ACME.POSH.Util;
using LetsEncrypt.ACME.POSH.Vault;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security.Cryptography.X509Certificates;
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
        [ValidateSet("Rsa", "Ec")]
        public string KeyType
        { get; set; } = "Rsa";

        [Parameter]
        public System.Collections.Hashtable KeyParams
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

                using (var cp = CertificateProvider.GetProvider())
                {

                    if (!string.IsNullOrEmpty(ci.GenerateDetailsFile))
                    {
                        // Generate a private key and CSR:
                        //    Key:  RSA 2048-bit
                        //    MD:   SHA 256
                        //    CSR:  Details pulled from CSR Details JSON file

                        CsrDetails csrDetails;
                        var csrDetailsAsset = vp.GetAsset(VaultAssetType.CsrDetails, ci.GenerateDetailsFile);
                        using (var s = vp.LoadAsset(csrDetailsAsset))
                        {
                            csrDetails = JsonHelper.Load<CsrDetails>(s);
                        }

                        var keyGenFile = $"{ci.Id}-gen-key.json";
                        var keyPemFile = $"{ci.Id}-key.pem";
                        var csrGenFile = $"{ci.Id}-gen-csr.json";
                        var csrPemFile = $"{ci.Id}-csr.pem";

                        var keyGenAsset = vp.CreateAsset(VaultAssetType.KeyGen, keyGenFile);
                        var keyPemAsset = vp.CreateAsset(VaultAssetType.KeyPem, keyPemFile);
                        var csrGenAsset = vp.CreateAsset(VaultAssetType.CsrGen, csrGenFile);
                        var csrPemAsset = vp.CreateAsset(VaultAssetType.CsrPem, csrPemFile);

                        var genKeyParams = new RsaPrivateKeyParams();

                        var genKey = cp.GeneratePrivateKey(genKeyParams);
                        using (var s = vp.SaveAsset(keyGenAsset))
                        {
                            cp.SavePrivateKey(genKey, s);
                        }
                        using (var s = vp.SaveAsset(keyPemAsset))
                        {
                            cp.ExportPrivateKey(genKey, EncodingFormat.PEM, s);
                        }

                        // TODO: need to surface details of the CSR params up higher
                        var csrParams = new CsrParams
                        {
                            Details = csrDetails
                        };
                        var genCsr = cp.GenerateCsr(csrParams, genKey, Crt.MessageDigest.SHA256);
                        using (var s = vp.SaveAsset(csrGenAsset))
                        {
                            cp.SaveCsr(genCsr, s);
                        }
                        using (var s = vp.SaveAsset(csrPemAsset))
                        {
                            cp.ExportCsr(genCsr, EncodingFormat.PEM, s);
                        }

                        ci.KeyPemFile = keyPemFile;
                        ci.CsrPemFile = csrPemFile;
                    }



                    byte[] derRaw;

                    var asset = vp.GetAsset(VaultAssetType.CsrPem, ci.CsrPemFile);
                    // Convert the stored CSR in PEM format to DER
                    using (var source = vp.LoadAsset(asset))
                    {
                        var csr = cp.ImportCsr(EncodingFormat.PEM, source);
                        using (var target = new MemoryStream())
                        {
                            cp.ExportCsr(csr, EncodingFormat.DER, target);
                            derRaw = target.ToArray();
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

                        var crtDerBytes = ci.CertificateRequest.GetCertificateContent();

                        var crtDerAsset = vp.CreateAsset(VaultAssetType.CrtDer, crtDerFile);
                        var crtPemAsset = vp.CreateAsset(VaultAssetType.CrtPem, crtPemFile);

                        using (Stream source = new MemoryStream(crtDerBytes),
                                derTarget = vp.SaveAsset(crtDerAsset),
                                pemTarget = vp.SaveAsset(crtPemAsset))
                        {
                            var crt = cp.ImportCertificate(EncodingFormat.DER, source);

                            cp.ExportCertificate(crt, EncodingFormat.DER, derTarget);
                            ci.CrtDerFile = crtDerFile;

                            cp.ExportCertificate(crt, EncodingFormat.PEM, pemTarget);
                            ci.CrtPemFile = crtPemFile;
                        }

                        // Extract a few pieces of info from the issued
                        // cert that we like to have quick access to
                        var x509 = new X509Certificate2(ci.CertificateRequest.GetCertificateContent());
                        ci.SerialNumber = x509.SerialNumber;
                        ci.Thumbprint = x509.Thumbprint;
                        ci.SignatureAlgorithm = x509.SignatureAlgorithm?.FriendlyName;
                        ci.Signature = x509.GetCertHashString();
                    }
                }

                vp.SaveVault(v);

                WriteObject(ci);
            }
        }
    }
}
