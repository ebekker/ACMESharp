using ACMESharp.Ext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.PKI
{
    [ExtManager]
    public static class PkiToolExtManager
    {
        private static Config _config;
        private static string _DefaultProvider;

        public static string DefaultProvider
        {
            get
            {
                if (_DefaultProvider == null)
                    _DefaultProvider = GetProviderInfos().First().Name;
                return _DefaultProvider;
            }

            set
            {
                _DefaultProvider = value;
            }
        }

        public static IEnumerable<NamedInfo<IPkiToolProviderInfo>> GetProviderInfos()
        {
            AssertInit();
            foreach (var pi in _config)
                yield return new NamedInfo<IPkiToolProviderInfo>(
                        pi.Key, pi.Value.Metadata);
        }

        public static IPkiToolProviderInfo GetProviderInfo(string name = null)
        {
            AssertInit();
            if (string.IsNullOrEmpty(name))
                name = DefaultProvider;
            return _config.Get(name)?.Metadata;
        }

        public static IEnumerable<string> GetAliases()
        {
            AssertInit();
            return _config.Aliases.Keys;
        }

        public static IPkiToolProvider GetProvider(string name = null,
            IReadOnlyDictionary<string, object> reservedLeaveNull = null)
        {
            AssertInit();
            if (string.IsNullOrEmpty(name))
                name = DefaultProvider;
            return _config.Get(name)?.Value;
        }

        /// <summary>
        /// Release existing configuration and registry and
        /// tries to rediscover and reload any providers.
        /// </summary>
        public static void Reload()
        {
            _config = ExtCommon.ReloadExtConfig<Config>(_config);
        }

        /// <summary>
        /// Provides a single method to resolve the provider and return an instance of the PKI tool.
        /// </summary>
        /// <returns>
        /// This is primarily provided as a convenience to mimic the previous
        /// CertificateProvider mechanims for resolving PKI tool providers.
        /// </returns>
        public static IPkiTool GetPkiTool(string name = null,
                IReadOnlyDictionary<string, object> initParams = null)
        {
            if (initParams == null)
                initParams = new Dictionary<string, object>();
            return GetProvider(name).GetPkiTool(initParams);
        }

        static void AssertInit()
        {
            if (_config == null)
            {
                lock (typeof(Config))
                {
                    if (_config == null)
                    {
                        Reload();
                    }
                }
            }
            if (_config == null)
                throw new InvalidOperationException("could not initialize provider configuration");
        }
        class Config : ExtRegistry<IPkiToolProvider, IPkiToolProviderInfo>
        {
            public Config() : base(_ => _.Name)
            { }
        }
    }
}
