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
    public interface IChallengeParserProviderInfo
    {
        string Type
        { get; }

        ChallengeTypeKind SupportedType
        { get; }

        string Label
        { get; }

        string Description
        { get; }
    }

    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ChallengeParserProviderAttribute : ExportAttribute
    {
        public ChallengeParserProviderAttribute(string type,
                ChallengeTypeKind supportedType) : base(typeof(IChallengeParserProvider))
        {
            Type = type;
            SupportedType = supportedType;
        }

        public ChallengeTypeKind SupportedType
        { get; private set; }

        public string Type
        { get; private set; }

        public string Label
        { get; set; }

        public string Description
        { get; set; }
    }

    public static class ChallengeParserExtManager
    {
        private static Config _config;

        public static IEnumerable<NamedInfo<IChallengeParserProviderInfo>> GetProviders()
        {
            AssertInit();
            foreach (var pi in _config)
                yield return new NamedInfo<IChallengeParserProviderInfo>(
                        pi.Key, pi.Value.Metadata);
        }

        public static IChallengeParserProvider GetProvider(string type,
            IDictionary<string, object> reservedLeaveNull = null)
        {
            AssertInit();
            return _config[type]?.Value;
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

        class Config : Dictionary<string, Lazy<IChallengeParserProvider,
                IChallengeParserProviderInfo>>, IExtDetail
        {
            private IEnumerable<Lazy<IChallengeParserProvider,
                    IChallengeParserProviderInfo>> _Providers;

            public CompositionContainer CompositionContainer
            { get; set; }

            [ImportMany]
            public IEnumerable<Lazy<IChallengeParserProvider,
                    IChallengeParserProviderInfo>> Providers
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

                        // ...if the type is not missing...
                        if (!string.IsNullOrEmpty(m?.Type))
                            // ...and the name is not already taken
                            if (!this.ContainsKey(m.Type))
                                this[m.Type] = x;
                    }
                }
            }
        }
    }
}
