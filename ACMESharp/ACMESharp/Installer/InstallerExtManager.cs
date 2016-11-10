using ACMESharp.ACME;
using ACMESharp.Ext;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Installer
{
    [ExtManager]
    public static class InstallerExtManager
    {
        private static Config _config;
        private static Dictionary<string, string> _aliases;

        public static IEnumerable<NamedInfo<IInstallerProviderInfo>> GetProviderInfos()
        {
            AssertInit();
            foreach (var pi in _config)
                yield return new NamedInfo<IInstallerProviderInfo>(
                        pi.Key, pi.Value.Metadata);
        }

        public static IInstallerProviderInfo GetProviderInfo(string name)
        {
            var pi = _config[name];
            if (pi == null && _aliases.ContainsKey(name))
                pi = _config[_aliases[name]];
            return pi?.Metadata;
        }

        public static IEnumerable<string> GetAliases()
        {
            return _aliases.Keys;
        }

        public static IInstallerProvider GetProvider(string name,
            IReadOnlyDictionary<string, object> reservedLeaveNull = null)
        {
            AssertInit();
            var p = _config[name]?.Value;
            if (p == null && _aliases.ContainsKey(name))
                p = _config[_aliases[name]]?.Value;
            return p;
        }

        static void AssertInit()
        {
            if (_config == null)
            {
                lock (typeof(Config))
                {
                    if (_config == null)
                    {
                        InitConfig();
                    }
                }
            }
            if (_config == null)
                throw new InvalidOperationException("could not initialize provider configuration");
        }

        static void InitConfig()
        {
            _config = ExtCommon.InitExtConfig<Config>();
            _aliases = new Dictionary<string, string>();
            foreach (var pi in _config)
            {
                var name = pi.Key;
                var aliases = pi.Value.Metadata.Aliases;
                if (aliases != null)
                    foreach (var al in aliases)
                        _aliases[al] = name;
            }
        }

        class Config : Dictionary<string, Lazy<IInstallerProvider,
                IInstallerProviderInfo>>, IExtDetail
        {
            private IEnumerable<Lazy<IInstallerProvider,
                    IInstallerProviderInfo>> _Providers;

            public Config()
                : base(StringComparer.InvariantCultureIgnoreCase)
            { }

            public CompositionContainer CompositionContainer
            { get; set; }

            [ImportMany]
            public IEnumerable<Lazy<IInstallerProvider,
                    IInstallerProviderInfo>> Providers
            {
                get
                {
                    return _Providers;
                }

                set
                {
                    _Providers = value;
                    Clear();
                    foreach (var x in Providers)
                    {
                        var m = x.Metadata;

                        // We can register the provider to the suggested name...

                        // ...if the name is not missing...
                        if (!string.IsNullOrEmpty(m?.Name))
                            // ...and the name is not already taken
                            if (!this.ContainsKey(m.Name))
                                this[m.Name] = x;
                    }
                }
            }
        }
    }
}
