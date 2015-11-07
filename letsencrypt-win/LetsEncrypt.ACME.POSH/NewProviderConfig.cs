using LetsEncrypt.ACME.POSH.Util;
using LetsEncrypt.ACME.POSH.Vault;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH
{
    [Cmdlet(VerbsCommon.New, "ProviderConfig")]
    public class NewProviderConfig : Cmdlet
    {
        public const string PSET_DNS = "dns";
        public const string PSET_HTTP = "http";

        [Parameter]
        public string Alias
        { get; set; }

        [Parameter]
        public string Label
        { get; set; }

        [Parameter]
        public string Memo
        { get; set; }

        [Parameter(ParameterSetName = PSET_DNS, Mandatory = true)]
        [ValidateSet("Manual", "AwsRoute53")]
        public string DnsProvider
        { get; set; }

        [Parameter(ParameterSetName = PSET_HTTP, Mandatory = true)]
        [ValidateSet("Manual", "AwsS3")]
        public string WebServerProvider
        { get; set; }

        [Parameter]
        public string VaultProfile
        { get; set; }

        protected override void ProcessRecord()
        {
            var pc = new ProviderConfig
            {
                Id = EntityHelper.NewId(),
                Alias = Alias,
                Label = Label,
                Memo = Memo,
                DnsProvider = DnsProvider,
                WebServerProvider = WebServerProvider,
            };

            var pcFilePath = Path.GetFullPath($"{pc.Id}.json");

            using (var vp = InitializeVault.GetVaultProvider(VaultProfile))
            {
                vp.OpenStorage();
                var v = vp.LoadVault();

                if (v.ProviderConfigs == null)
                    v.ProviderConfigs = new EntityDictionary<ProviderConfig>();
                v.ProviderConfigs.Add(pc);

                vp.SaveVault(v);

                // TODO: this is *so* hardcoded, clean
                // up this provider resolution mechanism
                Stream s = null;
                if (!string.IsNullOrEmpty(DnsProvider))
                {
                    s = typeof(ProviderConfig).Assembly.GetManifestResourceStream(
                            "LetsEncrypt.ACME.POSH.ProviderConfigSamples."
                            + $"dnsInfo.json.sample-{DnsProvider}DnsProvider");
                }
                if (!string.IsNullOrEmpty(WebServerProvider))
                {
                    s = typeof(ProviderConfig).Assembly.GetManifestResourceStream(
                            "LetsEncrypt.ACME.POSH.ProviderConfigSamples."
                            + $"webServerInfo.json.sample-{WebServerProvider}WebServerProvider");
                }

                var temp = Path.GetTempFileName();
                using (var fs = new FileStream(temp, FileMode.Create))
                {
                    s.CopyTo(fs);
                }

                var pcAsset = vp.CreateAsset(VaultAssetType.ProviderConfigInfo, $"{pc.Id}.json");
                using (Stream fs = new FileStream(temp, FileMode.Open),
                        assetStream = vp.SaveAsset(pcAsset))
                {
                    fs.CopyTo(assetStream);
                }
                File.Delete(temp);

                //!!!using (var fs = new FileStream(pcFilePath, FileMode.CreateNew))
                //!!!{
                //!!!    s.CopyTo(fs);
                //!!!}

                s.Close();
                s.Dispose();
            }

            WriteObject(pcFilePath);
        }
    }
}
