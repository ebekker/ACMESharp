using ACMESharp.POSH.Util;
using ACMESharp.Vault;
using ACMESharp.Vault.Model;
using System;
using System.IO;
using System.Management.Automation;
using System.Security.Cryptography.X509Certificates;
using ACMESharp.JOSE;
using ACMESharp.PKI;
using ACMESharp.Util;
using System.Collections;
using ACMESharp.PKI.RSA;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsLifecycle.Submit, "Certificate")]
    [OutputType(typeof(CertificateInfo))]
    public class SubmitCertificate : Cmdlet
    {
        /// <summary>
        /// <para type="description">
        ///     A reference (ID or alias) to a previously defined Certificate request.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [Alias("Ref")]
        public string CertificateRef
        { get; set; }

        //[Parameter]
        //[ValidateSet("Rsa", "Ec")]
        //public string KeyType
        //{ get; set; } = "Rsa";
        //
        //[Parameter]
        //public Hashtable KeyParams
        //{ get; set; }

        [Parameter]
        public SwitchParameter Force
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

                using (var cp = PkiHelper.GetPkiTool(
                            StringHelper.IfNullOrEmpty(PkiTool, v.PkiTool)))
                {

                    if (!string.IsNullOrEmpty(ci.GenerateDetailsFile))
                    {
                        // Generate a private key and CSR:
                        //    Key:  RSA 2048-bit
                        //    MD:   SHA 256
                        //    CSR:  Details pulled from CSR Details JSON file

                        CsrDetails csrDetails;
                        var csrDetailsAsset = vlt.GetAsset(VaultAssetType.CsrDetails, ci.GenerateDetailsFile);
                        using (var s = vlt.LoadAsset(csrDetailsAsset))
                        {
                            csrDetails = JsonHelper.Load<CsrDetails>(s);
                        }

                        var keyGenFile = $"{ci.Id}-gen-key.json";
                        var keyPemFile = $"{ci.Id}-key.pem";
                        var csrGenFile = $"{ci.Id}-gen-csr.json";
                        var csrPemFile = $"{ci.Id}-csr.pem";

                        var keyGenAsset = vlt.CreateAsset(VaultAssetType.KeyGen, keyGenFile, getOrCreate: Force);
                        var keyPemAsset = vlt.CreateAsset(VaultAssetType.KeyPem, keyPemFile, isSensitive: true, getOrCreate: Force);
                        var csrGenAsset = vlt.CreateAsset(VaultAssetType.CsrGen, csrGenFile, getOrCreate: Force);
                        var csrPemAsset = vlt.CreateAsset(VaultAssetType.CsrPem, csrPemFile, getOrCreate: Force);

                        var genKeyParams = new RsaPrivateKeyParams();

                        var genKey = cp.GeneratePrivateKey(genKeyParams);
                        using (var s = vlt.SaveAsset(keyGenAsset))
                        {
                            cp.SavePrivateKey(genKey, s);
                        }
                        using (var s = vlt.SaveAsset(keyPemAsset))
                        {
                            cp.ExportPrivateKey(genKey, EncodingFormat.PEM, s);
                        }

                        // TODO: need to surface details of the CSR params up higher
                        var csrParams = new CsrParams
                        {
                            Details = csrDetails
                        };
                        var genCsr = cp.GenerateCsr(csrParams, genKey, Crt.MessageDigest.SHA256);
                        using (var s = vlt.SaveAsset(csrGenAsset))
                        {
                            cp.SaveCsr(genCsr, s);
                        }
                        using (var s = vlt.SaveAsset(csrPemAsset))
                        {
                            cp.ExportCsr(genCsr, EncodingFormat.PEM, s);
                        }

                        ci.KeyPemFile = keyPemFile;
                        ci.CsrPemFile = csrPemFile;
                    }



                    byte[] derRaw;

                    var asset = vlt.GetAsset(VaultAssetType.CsrPem, ci.CsrPemFile);
                    // Convert the stored CSR in PEM format to DER
                    using (var source = vlt.LoadAsset(asset))
                    {
                        var csr = cp.ImportCsr(EncodingFormat.PEM, source);
                        using (var target = new MemoryStream())
                        {
                            cp.ExportCsr(csr, EncodingFormat.DER, target);
                            derRaw = target.ToArray();
                        }
                    }

                    var derB64u = JwsHelper.Base64UrlEncode(derRaw);

                    try
                    {
                        using (var c = ClientHelper.GetClient(v, ri))
                        {
                            c.Init();
                            c.GetDirectory(true);

                            ci.CertificateRequest = c.RequestCertificate(derB64u);
                        }
                    }
                    catch (AcmeClient.AcmeWebException ex)
                    {
                        ThrowTerminatingError(PoshHelper.CreateErrorRecord(ex, ci));
                        return;
                    }

                    if (!string.IsNullOrEmpty(ci.CertificateRequest.CertificateContent))
                    {
                        var crtDerFile = $"{ci.Id}-crt.der";
                        var crtPemFile = $"{ci.Id}-crt.pem";

                        var crtDerBytes = ci.CertificateRequest.GetCertificateContent();

                        var crtDerAsset = vlt.CreateAsset(VaultAssetType.CrtDer, crtDerFile);
                        var crtPemAsset = vlt.CreateAsset(VaultAssetType.CrtPem, crtPemFile);

                        using (Stream source = new MemoryStream(crtDerBytes),
                                derTarget = vlt.SaveAsset(crtDerAsset),
                                pemTarget = vlt.SaveAsset(crtPemAsset))
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

                vlt.SaveVault(v);

                WriteObject(ci);
            }
        }
    }
}
