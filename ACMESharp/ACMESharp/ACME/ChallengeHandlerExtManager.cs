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
    public interface IChallengeHandlerProviderInfo : IAliasesSupported
    {
        string Name
        { get; }

        ChallengeTypeKind SupportedTypes
        { get; }

        string Label
        { get; }

        string Description
        { get; }
    }

    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ChallengeHandlerProviderAttribute : ExportAttribute
    {
        public ChallengeHandlerProviderAttribute(string name,
                ChallengeTypeKind supportedTypes) : base(typeof(IChallengeHandlerProvider))
        {
            Name = name;
            SupportedTypes = supportedTypes;
        }

        public string Name
        { get; private set; }

        public ChallengeTypeKind SupportedTypes
        { get; private set; }

        public string[] Aliases
        { get; set; }

        public string Label
        { get; set; }

        public string Description
        { get; set; }
    }

    [ExtManager]
    public static class ChallengeHandlerExtManager
    {
        private static Config _config;

        public static IEnumerable<NamedInfo<IChallengeHandlerProviderInfo>> GetProviderInfos()
        {
            AssertInit();
            foreach (var pi in _config)
                yield return new NamedInfo<IChallengeHandlerProviderInfo>(
                        pi.Key, pi.Value.Metadata);
        }

        public static IChallengeHandlerProviderInfo GetProviderInfo(string name)
        {
            AssertInit();
            return _config.Get(name)?.Metadata;
        }

        public static IEnumerable<string> GetAliases()
        {
            AssertInit();
            return _config.Aliases.Keys;
        }

        public static IChallengeHandlerProvider GetProvider(string name,
            IReadOnlyDictionary<string, object> reservedLeaveNull = null)
        {
            AssertInit();
            return _config.Get(name).Value;
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

        class Config : ExtRegistry<IChallengeHandlerProvider, IChallengeHandlerProviderInfo>
        {
            public Config() : base(_ => _.Name)
            { }
        }
    }
}
