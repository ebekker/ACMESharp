using ACMESharp.POSH.Util;
using ACMESharp.POSH.Vault;
using System;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Text;
using Newtonsoft.Json;

namespace ACMESharp.POSH
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
        [ValidateSet("Manual", "AwsS3", "IisSitePath")]
        public string WebServerProvider
        { get; set; }


        [Parameter]
        public string EditWith
        { get; set; }

        [Parameter]
        public string VaultProfile
        { get; set; }

        [Parameter]
        public string FilePath
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
                FilePath = FilePath
            };

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
                    s = ProviderConfigSamples.Loader.LoadDnsProviderConfig(
                            DnsProvider);
                }
                if (!string.IsNullOrEmpty(WebServerProvider))
                {
                    s = ProviderConfigSamples.Loader.LoadWebServerProviderConfig(
                            WebServerProvider);
                }

                var temp = Path.GetTempFileName();
                if (string.IsNullOrWhiteSpace(FilePath))
                {
                    using (var fs = new FileStream(temp, FileMode.Create))
                    {
                        s.CopyTo(fs);
                    }
                    EditFile(temp, EditWith);
                }
                else
                {
                    var config = new ProviderConfigDto
                    {
                        Provider = new Provider
                        {
                            Type = "ACMESharp.WebServer.ManualWebServerProvider, ACMESharp",
                            FilePath = FilePath
                        }
                    };

                    var output = JsonConvert.SerializeObject(config);

                    s = new MemoryStream(Encoding.UTF8.GetBytes(output));
                    using (var fs = new FileStream(temp, FileMode.Create))
                    {
                        s.CopyTo(fs);
                    }
                }

                var pcAsset = vp.CreateAsset(VaultAssetType.ProviderConfigInfo, $"{pc.Id}.json");
                using (Stream fs = new FileStream(temp, FileMode.Open),
                        assetStream = vp.SaveAsset(pcAsset))
                {
                    fs.CopyTo(assetStream);
                }
                File.Delete(temp);

                s.Close();
                s.Dispose();
            }
        }

        public static void EditFile(string path, string editWith = null)
        {
            // TODO: For now we hard code the path to the default editor (Notepad)
            //       but we should compute this from the registry:
            //           HKEY_CLASSES_ROOT\txtfile\shell\open\command
            if (editWith == null)
                editWith = @"%SystemRoot%\system32\NOTEPAD.EXE";

            // Expand any potential envvars
            editWith = Environment.ExpandEnvironmentVariables(editWith);

            var p = Process.Start(editWith, path);
            p.WaitForExit();
        }
    }
}
