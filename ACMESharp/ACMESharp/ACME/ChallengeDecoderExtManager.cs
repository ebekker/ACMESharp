using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Ext;

namespace ACMESharp.ACME
{
    [ExtManager]
    public static class ChallengeDecoderExtManager
    {
        private static Config _config;

        public static IEnumerable<NamedInfo<IChallengeDecoderProviderInfo>> GetProviderInfos()
        {
            AssertInit();
            foreach (var pi in _config)
                yield return new NamedInfo<IChallengeDecoderProviderInfo>(
                        pi.Key, pi.Value.Metadata);
        }

        public static IChallengeDecoderProviderInfo GetProviderInfo(string type)
        {
            AssertInit();
            return _config.Get(type)?.Metadata;
        }

        public static IChallengeDecoderProvider GetProvider(string type,
            IReadOnlyDictionary<string, object> reservedLeaveNull = null)
        {
            AssertInit();
            return _config.Get(type)?.Value;
        }

        /// <summary>
        /// Release existing configuration and registry and
        /// tries to rediscover and reload any providers.
        /// </summary>
        public static void Reload()
        {
            _config = ExtCommon.ReloadExtConfig<Config>(_config);
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

        class Config : ExtRegistry<IChallengeDecoderProvider, IChallengeDecoderProviderInfo>
        {
            public Config() : base(_ => _.Type)
            { }
        }
    }
}
