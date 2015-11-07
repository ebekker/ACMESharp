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
                    var csrDetailsAsset = vp.GetAsset(VaultAssetType.CsrDetails, ci.GenerateDetailsFile);
                    using (var s = vp.LoadAsset(csrDetailsAsset))
                    {
                        csrDetails = JsonHelper.Load<CsrHelper.CsrDetails>(s);
                    }

                    var keyGenFile = $"{ci.Id}-gen-key.json";
                    var keyPemFile = $"{ci.Id}-key.pem";
                    var csrGenFile = $"{ci.Id}-gen-csr.json";
                    var csrPemFile = $"{ci.Id}-csr.pem";

                    var keyGenAsset = vp.CreateAsset(VaultAssetType.KeyGen, keyGenFile);
                    var keyPemAsset = vp.CreateAsset(VaultAssetType.KeyPem, keyPemFile);
                    var csrGenAsset = vp.CreateAsset(VaultAssetType.CsrGen, csrGenFile);
                    var csrPemAsset = vp.CreateAsset(VaultAssetType.CsrPem, csrPemFile);

                    var genKey = CsrHelper.GenerateRsaPrivateKey();
                    using (var s = vp.SaveAsset(keyGenAsset))
                    {
                        genKey.Save(s);
                    }
                    using (var w = new StreamWriter(vp.SaveAsset(keyPemAsset)))
                    {
                        w.Write(genKey.Pem);
                    }

                    var genCsr = CsrHelper.GenerateCsr(csrDetails, genKey);
                    using (var s = vp.SaveAsset(csrGenAsset))
                    {
                        genCsr.Save(s);
                    }
                    using (var w = new StreamWriter(vp.SaveAsset(csrPemAsset)))
                    {
                        w.Write(genCsr.Pem);
                    }

                    ci.KeyPemFile = keyPemFile;
                    ci.CsrPemFile = csrPemFile;
                }

                var asset = vp.GetAsset(VaultAssetType.CsrPem, ci.CsrPemFile);

                byte[] derRaw;
                using (var s = vp.LoadAsset(asset))
                {
                    using (var ms = new MemoryStream())
                    {
                        CsrHelper.Csr.ConvertPemToDer(s, ms);
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

                    var crtDerAsset = vp.CreateAsset(VaultAssetType.CrtDer, crtDerFile);
                    var crtPemAsset = vp.CreateAsset(VaultAssetType.CrtPem, crtPemFile);

                    using (var s = vp.SaveAsset(crtDerAsset))
                    {
                        ci.CertificateRequest.SaveCertificate(s);
                        ci.CrtDerFile = crtDerFile;
                    }

                    using (Stream source = vp.LoadAsset(crtDerAsset), target = vp.SaveAsset(crtPemAsset))
                    {
                        CsrHelper.Crt.ConvertDerToPem(source, target);
                        ci.CrtPemFile = crtPemFile;
                    }

                    var crt = new X509Certificate2(crtDerFile);

                    ci.SerialNumber = crt.SerialNumber;
                    ci.Thumbprint = crt.Thumbprint;
                    ci.SignatureAlgorithm = crt.SignatureAlgorithm?.FriendlyName;
                    ci.Signature = crt.GetCertHashString();
                }

                vp.SaveVault(v);

                WriteObject(ci);
            }
        }
    }
}
