using ACMESharp.Vault.Model;
using ACMESharp.POSH.Util;
using System;
using System.Linq;
using System.Management.Automation;

namespace ACMESharp.POSH
{
    [Cmdlet("Revoke", "Certificate")]
    public class RevokeCertificate : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        [Alias("Ref")]
        public string CertificateRef
        { get; set; }

        [Parameter]
        [ValidateSet("unspecified", "keyCompromise", "superseded")]
        public string Reason
        { get; set; } = "unspecified";

        [Parameter]
        public string VaultProfile
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
                    throw new ItemNotFoundException("Unable to find a Certificate for the given reference");

                if (ci.CertificateRequest == null)
                    throw new Exception("Certificate has not been submitted yet; cannot revoke the certificate");

                // Revoke ACME certificate
                try
                {
                    using (var c = ClientHelper.GetClient(v, ri))
                    {
                        c.Init();
                        c.GetDirectory(true);

                        var reasonCode = 0;
                        switch (Reason)
                        {
                            case "keyCompromise":
                                reasonCode = 1;
                                break;
                            case "superseded":
                                reasonCode = 4;
                                break;
                            default:
                                reasonCode = 0;
                                break;
                        }
                        c.RevokeCertificate(ci.CertificateRequest.CertificateContent, reasonCode);
                    }
                }
                catch (AcmeClient.AcmeWebException ex)
                {
                    ThrowTerminatingError(PoshHelper.CreateErrorRecord(ex, ci));
                    return;
                }

                WriteObject(null);
            }
        }
    }
}
