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

        public static IEnumerable<NamedInfo<IInstallerProviderInfo>> GetProviderInfos()
        {
            AssertInit();
            foreach (var pi in _config)
                yield return new NamedInfo<IInstallerProviderInfo>(
                        pi.Key, pi.Value.Metadata);
        }

        public static IInstallerProviderInfo GetProviderInfo(string name)
        {
            AssertInit();
            return _config.Get(name)?.Metadata;
        }

        public static IEnumerable<string> GetAliases()
        {
            AssertInit();
            return _config.Aliases.Keys;
        }

        public static IInstallerProvider GetProvider(string name,
            IReadOnlyDictionary<string, object> reservedLeaveNull = null)
        {
            AssertInit();
            return _config.Get(name)?.Value;
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
        }

        class Config : ExtRegistry<IInstallerProvider, IInstallerProviderInfo>
        {
            public Config() : base(_ => _.Name)
            { }
        }
    }
}
