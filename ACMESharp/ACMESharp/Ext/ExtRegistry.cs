using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Ext
{
    /// <summary>
    /// A registery and configuration base class for storing details
    /// for providers.
    /// </summary>
    /// <typeparam name="P">Provider type</typeparam>
    /// <typeparam name="PI">Provider Info type</typeparam>
    public class ExtRegistry<P, PI> : Dictionary<string, Lazy<P, PI>>, IExtDetail
    {
        private Func<PI, string> _keyGetter;
        // NOTE:  Even though we declare this for lazy evaluation, in reality
        // this will get evaluated and instantiated almost as soon as it's
        // configured because we'll need to inspect the value in order to
        // setup some additional details; therefore the Lazy<> type is
        // really just being used as a tuple container like KVPair
        private IEnumerable<Lazy<P, PI>> _Providers;
        private Dictionary<string, string> _Aliases;

        /// <param name="keyGetter">
        /// Specifies a mapping function that knows how to
        /// derive a unique identifer (key) from an instance
        /// of the associated PI type.
        /// </param>
        public ExtRegistry(Func<PI, string> keyGetter)
                : base(StringComparer.InvariantCultureIgnoreCase)
        {
            _keyGetter = keyGetter;
        }

        public CompositionContainer CompositionContainer
        { get; set; }

        [ImportMany]
        public IEnumerable<Lazy<P, PI>> Providers
        {
            get
            {
                return _Providers;
            }

            set
            {
                _Providers = value;
                Clear();

                // If the PI supports aliases, create a place to store them
                _Aliases = typeof(IAliasesSupported).IsAssignableFrom(typeof(PI))
                        ? new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
                        : null;

                foreach (var x in _Providers)
                {
                    var m = x.Metadata;
                    var registered = false;

                    // We can register the provider to the suggested name...

                    // ...if the name is not missing...
                    var name = _keyGetter(m);
                    if (!string.IsNullOrEmpty(name))
                    {
                        // ...and the name is not already taken
                        if (!this.ContainsKey(name))
                        {
                            this[name] = x;
                            registered = true;

                            // If we have a place to store them,
                            // extract and map the aliases
                            var als = (m as IAliasesSupported)?.Aliases;
                            if (_Aliases != null && als != null)
                            {
                                foreach (var al in als)
                                    _Aliases[al] = name;
                            }
                        }
                    }

                    PostRegisterProvider(x.Metadata, x.Value, registered);
                }
            }
        }

        public Dictionary<string, string> Aliases
        {
            get { return _Aliases; }
        }

        public Lazy<P, PI> Get(string key)
        {
            Lazy<P, PI> value = null;
            if (!base.TryGetValue(key, out value)
                    && _Aliases != null
                    && _Aliases.ContainsKey(key))
                base.TryGetValue(_Aliases[key], out value);

            if (value == null)
                throw new KeyNotFoundException("the given identifier was not found in the registry");

            return value;
        }

        /// <summary>
        /// Invoked during the processing of providers after each one
        /// is registered.
        /// </summary>
        /// <param name=""></param>
        /// <param name=""></param>
        protected virtual void PostRegisterProvider(PI providerInfo, P provider, bool registered)
        { }
    }
}
