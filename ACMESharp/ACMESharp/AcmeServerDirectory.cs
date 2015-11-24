using System;
using System.Collections;
using System.Collections.Generic;

namespace ACMESharp
{
    public class AcmeServerDirectory : IDisposable, IEnumerable<KeyValuePair<string, string>>,
            IReadOnlyDictionary<string, string>//, ILookup<string, string>
    {
        /// <summary>
        /// Initial resource used to trieve the very first "nonce" header value before starting
        /// a dialogue with the ACME server.  Typically this may just be a one of the other
        /// resource paths, such as the directory instead of a dedicated resource.
        /// </summary>
        public const string RES_INIT = "init";
        public const string RES_DIRECTORY = "directory";
        public const string RES_NEW_REG = "new-reg";
        public const string RES_RECOVER_REG = "recover-reg";
        public const string RES_NEW_AUTHZ = "new-authz";
        public const string RES_NEW_CERT = "new-cert";
        public const string RES_REVOKE_CERT = "revoke-cert";

        /// <summary>
        /// Non-standard, as per the ACME spec, but defined in Boulder.
        /// </summary>
        public const string RES_ISSUER_CERT = "issuer-cert";

        protected const string DEFAULT_PATH_INIT = "/directory";
        protected const string DEFAULT_PATH_DIRECTORY = "/directory";
        protected const string DEFAULT_PATH_NEW_REG = "/new-reg";
        protected const string DEFAULT_PATH_RECOVER_REG = "/recover-reg";
        protected const string DEFAULT_PATH_NEW_AUTHZ = "/new-authz";
        protected const string DEFAULT_PATH_NEW_CERT = "/new-cert";
        protected const string DEFAULT_PATH_REVOKE_CERT = "/revoke-cert";

        protected const string DEFAULT_PATH_ISSUER_CERT = "/acme/issuer-cert";

        private Dictionary<string, string> _dirMap = new Dictionary<string, string>();

        public AcmeServerDirectory()
        {
            InitDirMap();
        }

        public AcmeServerDirectory(IDictionary<string, string> dict)
        {
            InitDirMap();
            foreach (var item in dict)
                this[item.Key] = item.Value;
        }

        public int Count
        {
            get { return _dirMap.Count; }
        }

        public IEnumerable<string> Keys
        {
            get
            {
                return _dirMap.Keys;
            }
        }

        public IEnumerable<string> Values
        {
            get
            {
                return _dirMap.Values;
            }
        }

        public string this[string key]
        {
            get
            {
                if (_dirMap.ContainsKey(key))
                    return _dirMap[key];
                throw new KeyNotFoundException("Resource key not found");
            }

            set
            {
                _dirMap[key] = value;
            }
        }

      //IEnumerable<string> ILookup<string, string>.this[string key]
      //{
      //    get
      //    {
      //        if (_dirMap.ContainsKey(key))
      //            yield return _dirMap[key];
      //    }
      //}

        private void InitDirMap()
        {
            // Populate the default path mappings
            _dirMap[RES_INIT] = DEFAULT_PATH_INIT;
            _dirMap[RES_DIRECTORY] = DEFAULT_PATH_DIRECTORY;
            _dirMap[RES_NEW_REG] = DEFAULT_PATH_NEW_REG;
            _dirMap[RES_RECOVER_REG] = DEFAULT_PATH_RECOVER_REG;
            _dirMap[RES_NEW_AUTHZ] = DEFAULT_PATH_NEW_AUTHZ;
            _dirMap[RES_NEW_CERT] = DEFAULT_PATH_NEW_CERT;
            _dirMap[RES_REVOKE_CERT] = DEFAULT_PATH_REVOKE_CERT;

            _dirMap[RES_ISSUER_CERT] = DEFAULT_PATH_ISSUER_CERT;
        }

        public void Dispose()
        {
            _dirMap.Clear();
            _dirMap = null;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _dirMap.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dirMap.GetEnumerator();
        }

        public bool Contains(string key)
        {
            return _dirMap.ContainsKey(key);
        }

      //IEnumerator<IGrouping<string, string>> IEnumerable<IGrouping<string, string>>.GetEnumerator()
      //{
      //    foreach (var item in _dirMap)
      //    {
      //        yield return new Grouping { KeyValuePair = item };
      //    }
      //}

        public bool ContainsKey(string key)
        {
            return _dirMap.ContainsKey(key);
        }

        public bool TryGetValue(string key, out string value)
        {
            return _dirMap.TryGetValue(key, out value);
        }

      //private class Grouping : IGrouping<string, string>
      //{
      //    public KeyValuePair<string, string> KeyValuePair
      //    { get; set; }
      //
      //    public string Key
      //    {
      //        get { return KeyValuePair.Key; }
      //    }
      //
      //    public IEnumerator<string> GetEnumerator()
      //    {
      //        yield return KeyValuePair.Value;
      //    }
      //
      //    IEnumerator IEnumerable.GetEnumerator()
      //    {
      //        return (this as IEnumerable<string>).GetEnumerator();
      //    }
      //}
    }
}
