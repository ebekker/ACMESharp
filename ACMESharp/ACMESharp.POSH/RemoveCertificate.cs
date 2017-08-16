using ACMESharp.Vault;
using System;
using System.Linq;
using System.Management.Automation;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsCommon.Remove, "Certificate")]
    public class RemoveCertificate : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        [Alias("Ref")]
        public string CertificateRef
        { get; set; }

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
                {
                    throw new ItemNotFoundException("Unable to find a Certificate for the given reference");
                }
                else
                {
                    v.Certificates.Remove(ci.Id);

                    // remove files
                    var keyGenFile = $"{ci.Id}-gen-key.json";
                    var csrGenFile = $"{ci.Id}-gen-csr.json";

                    vlt.RemoveAsset(VaultAssetType.CsrDetails, ci.GenerateDetailsFile);
                    vlt.RemoveAsset(VaultAssetType.KeyGen, keyGenFile);
                    vlt.RemoveAsset(VaultAssetType.KeyPem, ci.KeyPemFile);
                    vlt.RemoveAsset(VaultAssetType.CsrGen, csrGenFile);
                    vlt.RemoveAsset(VaultAssetType.CsrPem, ci.CsrPemFile);
                    vlt.RemoveAsset(VaultAssetType.CrtPem, ci.CrtPemFile);
                    vlt.RemoveAsset(VaultAssetType.CrtDer, ci.CrtDerFile);
                }

                vlt.SaveVault(v);
            }
        }
    }
}
