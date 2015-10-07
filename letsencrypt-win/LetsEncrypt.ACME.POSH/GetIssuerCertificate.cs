using System;
using System.Collections.Generic;
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

        protected override void ProcessRecord()
        {
            using (var vp = InitializeVault.GetVaultProvider())
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

                    WriteObject(v.IssuerCertificates[SerialNumber]);
                }
            }
        }
    }
}
