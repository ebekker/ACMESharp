using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.POSH.ProviderConfigSamples
{
    public class Loader
    {
        public static Stream LoadDnsProviderConfig(string name)
        {
            var t = typeof(Loader);
            var a = t.Assembly;
            var n = t.Namespace;

            return a.GetManifestResourceStream(n
                    + $".dnsInfo.json.sample-{name}DnsProvider");
        }

        public static Stream LoadWebServerProviderConfig(string name)
        {
            var t = typeof(Loader);
            var a = t.Assembly;
            var n = t.Namespace;

            return a.GetManifestResourceStream(n
                    + $".webServerInfo.json.sample-{name}WebServerProvider");
        }
    }
}
