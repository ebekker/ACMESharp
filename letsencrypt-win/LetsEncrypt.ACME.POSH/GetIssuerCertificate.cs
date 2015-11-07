using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH
{
    [Cmdlet(VerbsCommon.Get, "IssuerCertificate")]
    public class GetIssuerCertificate : Cmdlet
    {
        [Parameter]
        public string SerialNumber
        { get; set; }

        [Parameter]
        public string ExportCertificatePEM
        { get; set; }

        [Parameter]
        public string ExportCertificateDER
        { get; set; }

        [Parameter]
        public SwitchParameter Overwrite
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

                if (v.IssuerCertificates == null || v.IssuerCertificates.Count < 1)
                    throw new InvalidOperationException("No issuer certificates found");

                if (string.IsNullOrEmpty(SerialNumber))
                {
                    WriteObject(v.IssuerCertificates.Values, true);
                }
                else
                {
                    if (!v.IssuerCertificates.ContainsKey(SerialNumber))
                        throw new ItemNotFoundException("Unable to find an Issuer Certificate for the given serial number");

                    var ic = v.IssuerCertificates[SerialNumber];
                    var mode = Overwrite ? FileMode.Create : FileMode.CreateNew;

                    if (!string.IsNullOrEmpty(ExportCertificatePEM))
                    {
                        if (string.IsNullOrEmpty(ic.CrtPemFile))
                            throw new InvalidOperationException("Cannot export CRT; CRT hasn't been retrieved");
                        GetCertificate.CopyTo(vp, Vault.VaultAssetType.IssuerPem, ic.CrtPemFile,
                                ExportCertificatePEM, mode);
                    }

                    if (!string.IsNullOrEmpty(ExportCertificateDER))
                    {
                        if (string.IsNullOrEmpty(ic.CrtDerFile))
                            throw new InvalidOperationException("Cannot export CRT; CRT hasn't been retrieved");

                        GetCertificate.CopyTo(vp, Vault.VaultAssetType.IssuerDer, ic.CrtDerFile,
                                ExportCertificateDER, mode);
                    }

                    WriteObject(v.IssuerCertificates[SerialNumber]);
                }
            }
        }
    }
}
