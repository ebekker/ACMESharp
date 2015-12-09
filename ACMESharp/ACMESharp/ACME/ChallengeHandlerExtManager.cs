using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Ext;
using System.ComponentModel.Composition.Hosting;

namespace ACMESharp.ACME
{
    public interface IChallengeHandlerProviderInfo
    {
        string Name
        { get; }

        string Label
        { get; }

        string Description
        { get; }

        ChallengeTypeKind SupportedTypes
        { get; }
    }

    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ChallengeHandlerProviderAttribute : ExportAttribute
    {
        public ChallengeHandlerProviderAttribute(string name)
            : base(typeof(IChallengeHandlerProvider))
        {
            Name = name;
        }

        public string Name
        { get; private set; }

        public string Label
        { get; set; }

        public string Description
        { get; set; }

        public ChallengeTypeKind SupportedTypes
        { get; set; }
    }

    public static class ChallengeHandlerExtManager
    {
        private static Config _config;

        public static IEnumerable<NamedInfo<IChallengeHandlerProviderInfo>> GetProviders()
        {
            AssertInit();
            foreach (var pi in _config)
                yield return new NamedInfo<IChallengeHandlerProviderInfo>(
                        pi.Key, pi.Value.Metadata);
        }

        public static IChallengeHandlerProvider GetProvider(string name,
            IDictionary<string, object> reservedLeaveNull = null)
        {
            AssertInit();
            return _config[name]?.Value;
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

        class Config : Dictionary<string, Lazy<IChallengeHandlerProvider,
                IChallengeHandlerProviderInfo>>, IExtDetail
        {
            private IEnumerable<Lazy<IChallengeHandlerProvider,
                    IChallengeHandlerProviderInfo>> _Providers;

            public CompositionContainer CompositionContainer
            { get; set; }

            [ImportMany]
            public IEnumerable<Lazy<IChallengeHandlerProvider,
                    IChallengeHandlerProviderInfo>> Providers
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
