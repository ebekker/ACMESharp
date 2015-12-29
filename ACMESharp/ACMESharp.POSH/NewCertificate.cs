using ACMESharp.POSH.Util;
using ACMESharp.Vault;
using ACMESharp.Vault.Model;
using System;
using System.IO;
using System.Management.Automation;
using ACMESharp.PKI;
using ACMESharp.Vault.Util;
using ACMESharp.Util;

namespace ACMESharp.POSH
{
    [Cmdlet(VerbsCommon.New, "Certificate", DefaultParameterSetName = PSET_DEFAULT)]
    [OutputType(typeof(CertificateInfo))]
    public class NewCertificate : Cmdlet
    {
        public const string PSET_DEFAULT = "Default";
        public const string PSET_GENERATE = "Generate";

        [Parameter(Mandatory = true)]
        public string Identifier
        { get; set; }

        [Parameter(ParameterSetName = PSET_DEFAULT, Mandatory = true)]
        public string KeyPemFile
        { get; set; }

        [Parameter(ParameterSetName = PSET_DEFAULT, Mandatory = true)]
        public string CsrPemFile
        { get; set; }

        [Parameter(ParameterSetName = PSET_GENERATE, Mandatory = true)]
        public SwitchParameter Generate
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

                if (v.Identifiers == null || v.Identifiers.Count < 1)
                    throw new InvalidOperationException("No identifiers found");

                var ii = v.Identifiers.GetByRef(Identifier);
                if (ii == null)
                    throw new Exception("Unable to find an Identifier for the given reference");

                var ci = new CertificateInfo
                {
                    Id = EntityHelper.NewId(),
                    Alias = Alias,
                    Label = Label,
                    Memo = Memo,
                    IdentifierRef = ii.Id,
                };

                if (Generate)
                {
                    var csrDetails = new CsrDetails
                    {
                        CommonName = ii.Dns,
                    };

                    ci.GenerateDetailsFile = $"{ci.Id}-gen.json";
                    var asset = vlt.CreateAsset(VaultAssetType.CsrDetails, ci.GenerateDetailsFile);
                    using (var s = vlt.SaveAsset(asset))
                    {
                        JsonHelper.Save(s, csrDetails);
                    }
                }
                else
                {
                    if (!File.Exists(KeyPemFile))
                        throw new FileNotFoundException("Missing specified RSA Key file path");
                    if (!File.Exists(CsrPemFile))
                        throw new FileNotFoundException("Missing specified CSR details file path");

                    var keyPemFile = $"{ci.Id}-key.pem";
                    var csrPemFile = $"{ci.Id}-csr.pem";

                    var keyAsset = vlt.CreateAsset(VaultAssetType.KeyPem, keyPemFile, true);
                    var csrAsset = vlt.CreateAsset(VaultAssetType.CsrPem, csrPemFile);

                    using (Stream fs = new FileStream(KeyPemFile, FileMode.Open),
                            s = vlt.SaveAsset(keyAsset))
                    {
                        fs.CopyTo(s);
                    }
                    using (Stream fs = new FileStream(KeyPemFile, FileMode.Open),
                            s = vlt.SaveAsset(csrAsset))
                    {
                        fs.CopyTo(s);
                    }

                    ci.KeyPemFile = keyPemFile;
                    ci.CsrPemFile = csrPemFile;
                }

                if (v.Certificates == null)
                    v.Certificates = new EntityDictionary<CertificateInfo>();

                v.Certificates.Add(ci);

                vlt.SaveVault(v);

                WriteObject(ci);
            }
        }
    }
}
