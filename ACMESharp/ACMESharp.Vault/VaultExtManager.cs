using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Ext;
using ACMESharp.Vault.Providers;
using AppDomain.FooArea;

namespace ACMESharp.Vault
{
    public interface IVaultProviderInfo
    {
        string Name
        { get; }

        string Label
        { get; set; }

        string Description
        { get; set; }
    }

    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class VaultProviderAttribute : ExportAttribute
    {
        public VaultProviderAttribute(string name)
            : base(typeof (IVaultProvider))
        {
            Name = name;
        }

        public string Name 
        { get; set; }

        public string Label
        { get; set; }

        public string Description
        { get; set; }
    }

    /// <summary>
    /// Extension Manager for the Vault subsystem.
    /// </summary>
    public static class VaultExtManager
    {
        public static readonly Type DEFAULT_PROVIDER_TYPE =
                typeof (LocalDiskVaultProvider);

        private static Config _config;
        private static string _defaultProviderName;

        public static string DefaultProviderName
        {
            get
            {
                AssertInit();
                return _defaultProviderName;
            }
        }

        public static IEnumerable<Tuple<string, IVaultProviderInfo>> GetProviders()
        {
            AssertInit();
            foreach (var pi in _config)
                yield return new Tuple<string, IVaultProviderInfo>(pi.Key, pi.Value.Metadata);
        } 

        /// <remarks>
        /// An optional name may be given to distinguish between different
        /// available provider implementations.  Additionally, an optional
        /// set of named initialization parameters may be provided to
        /// further configure or qualify the Provider instance that is
        /// returned and ultimately the Components that the Provider
        /// produces.
        /// </remarks>
        public static IVaultProvider GetProvider(string name = null,
            IDictionary<string, object> initParams = null)
        {
            AssertInit();
            if (name == null)
                name = _defaultProviderName;
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

        class Config : Dictionary<string, Lazy<IVaultProvider, IVaultProviderInfo>>,
                IExtDetail
        {
            // NOTE:  Even though we declare this for lazy evaluation, in reality
            // this will get evaluated and instantiated almost as soon as it's
            // configured because we'll need to inspect the value in order to
            // setup some additional details; therefore the Lazy<> type is
            // really just being used as a tuple container like KVPair
            private IEnumerable<Lazy<IVaultProvider, IVaultProviderInfo>> _Providers;

            public CompositionContainer CompositionContainer
            { get; set; }

            [ImportMany]
            public IEnumerable<Lazy<IVaultProvider, IVaultProviderInfo>> Providers
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

                        // Special case for the default provider
                        if (_defaultProviderName == null && DEFAULT_PROVIDER_TYPE == x.Value.GetType())
                            _defaultProviderName = m.Name;
                    }
                }
            }
        }
    }
}




