using LetsEncrypt.ACME.PKI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH
{
    [Cmdlet(VerbsCommon.Get, "Certificate")]
    public class GetCertificate : Cmdlet
    {
        [Parameter(Mandatory = true)]
        public string Ref
        { get; set; }

        [Parameter]
        public string ExportKeyPEM
        { get; set; }

        [Parameter]
        public string ExportCsrPEM
        { get; set; }

        [Parameter]
        public string ExportCertificatePEM
        { get; set; }

        [Parameter]
        public string ExportCertificateDER
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
                    throw new ItemNotFoundException("Unable to find a Certificate for the given reference");

                if (!string.IsNullOrEmpty(ExportKeyPEM))
                {
                    if (string.IsNullOrEmpty(ci.KeyPemFile))
                        throw new InvalidOperationException("Cannot export private key; it hasn't been imported or generated");
                    File.Copy(ci.KeyPemFile, ExportKeyPEM);
                }

                if (!string.IsNullOrEmpty(ExportCsrPEM))
                {
                    if (string.IsNullOrEmpty(ci.CsrPemFile))
                        throw new InvalidOperationException("Cannot export CSR; it hasn't been imported or generated");
                    File.Copy(ci.CsrPemFile, ExportCsrPEM);
                }

                if (!string.IsNullOrEmpty(ExportCertificatePEM))
                {
                    if (ci.CertificateRequest == null || string.IsNullOrEmpty(ci.CrtPemFile))
                        throw new InvalidOperationException("Cannot export CRT; CSR hasn't been submitted or CRT hasn't been retrieved");
                    File.Copy(ci.CrtPemFile, ExportCertificatePEM);
                }

                if (!string.IsNullOrEmpty(ExportCertificateDER))
                {
                    if (ci.CertificateRequest == null || string.IsNullOrEmpty(ci.CrtDerFile))
                        throw new InvalidOperationException("Cannot export CRT; CSR hasn't been submitted or CRT hasn't been retrieved");
                    File.Copy(ci.CrtDerFile, ExportCertificateDER);
                }

                WriteObject(ci);
            }
        }
    }
}
