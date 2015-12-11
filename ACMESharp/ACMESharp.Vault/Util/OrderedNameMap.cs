using System;
using System.Collections;
using System.Collections.Generic;

namespace ACMESharp.Vault.Util
{
    public class OrderedNameMap<TValue> :
        IReadOnlyDictionary<string, TValue>,
        IReadOnlyDictionary<int, TValue>
    {
        private readonly IndexedDictionary<string, TValue> _dict = new IndexedDictionary<string, TValue>();

        public OrderedNameMap()
        { }

        public OrderedNameMap(IEnumerable<KeyValuePair<string, TValue>> kvs)
        {
            foreach (var kv in kvs)
                _dict.Add(kv);
        }

        public TValue this[string key]
        {
            get
            {
                return _dict[key];
            }
            set
            {
                _dict[key] = value;
            }
        }

        public TValue this[int key]
        {
            get
            {
                return (TValue)_dict[key];
            }
            set
            {
                _dict[key] = (TValue)value;
            }
        }

        public int Count
        {
            get
            {
                return _dict.Count;
            }
        }

        public IEnumerable<string> Keys
        {
            get
            {
                return _dict.Keys;
            }
        }

        public IEnumerable<TValue> Values
        {
            get
            {
                return _dict.Values;
            }
        }

        int IReadOnlyCollection<KeyValuePair<int, TValue>>.Count
        {
            get
            {
                return _dict.Count;
            }
        }

        IEnumerable<int> IReadOnlyDictionary<int, TValue>.Keys
        {
            get
            {
                for (int i = 0; i < _dict.Count; ++i)
                    yield return i;
            }
        }

        IEnumerable<TValue> IReadOnlyDictionary<int, TValue>.Values
        {
            get
            {
                return _dict.Values;
            }
        }

        public bool ContainsKey(string key)
        {
            return _dict.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        public bool TryGetValue(string key, out TValue value)
        {
            return _dict.TryGetValue(key, out value);
        }

        bool IReadOnlyDictionary<int, TValue>.ContainsKey(int key)
        {
            return key < _dict.Count;
        }

        IEnumerator<KeyValuePair<int, TValue>> IEnumerable<KeyValuePair<int, TValue>>.GetEnumerator()
        {
            var index = 0;
            foreach (var item in _dict)
                yield return new KeyValuePair<int, TValue>(index++, item.Value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        bool IReadOnlyDictionary<int, TValue>.TryGetValue(int key, out TValue value)
        {
            try
            {
                value = (TValue)_dict[key];
                return true;
            }
            catch (Exception)
            {
                value = default(TValue);
                return false;
            }
        }
    }
}
