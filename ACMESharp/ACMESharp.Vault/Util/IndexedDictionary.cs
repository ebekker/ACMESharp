using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace ACMESharp.Vault.Util
{
    public class IndexedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IOrderedDictionary
    {
        private List<TKey> _keyList = new List<TKey>();
        private Dictionary<TKey, TValue> _entDict = new Dictionary<TKey, TValue>();

        public object this[int index]
        {
            get
            {
                return Get(_keyList[index]);
            }

            set
            {
                Set(_keyList[index], (TValue)value);
            }
        }

        public object this[object key]
        {
            get
            {
                return Get((TKey)key);
            }

            set
            {
                Set((TKey)key, (TValue)value);
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                return Get(key);
            }

            set
            {
                Set(key, value);
            }
        }

        private TValue Get(TKey key)
        {
            return ((IDictionary<TKey, TValue>)_entDict)[key];
        }

        private void Set(TKey key, TValue value)
        {
            ((IDictionary<TKey, TValue>)_entDict)[key] = value;
        }

        public int Count
        {
            get
            {
                return ((IDictionary<TKey, TValue>)_entDict).Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return ((IDictionary<TKey, TValue>)_entDict).IsReadOnly;
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                return ((IDictionary<TKey, TValue>)_entDict).Keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                return ((IDictionary<TKey, TValue>)_entDict).Values;
            }
        }

        ICollection IDictionary.Keys
        {
            get
            {
                return _entDict.Keys;
            }
        }

        ICollection IDictionary.Values
        {
            get
            {
                return _entDict.Values;
            }
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        public object SyncRoot
        {
            get
            {
                return this;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public void Insert(int index, object key, object value)
        {
            if (_entDict.ContainsKey((TKey)key))
                throw new ArgumentException("An element with the same key already exists", "key");
            _entDict[(TKey)key] = (TValue)value;
            _keyList.Insert(index, (TKey)key);
        }

        public void RemoveAt(int index)
        {
            Remove(_keyList[index]);
        }

        public bool Contains(object key)
        {
            return _entDict.ContainsKey((TKey)key);
        }

        public void Add(object key, object value)
        {
            // Assuming this succeeds...
            _entDict.Add((TKey)key, (TValue)value);
            // ...add the key to our ordered list
            _keyList.Add((TKey)key);
        }

        public void Remove(object key)
        {
            if (_entDict.Remove((TKey)key))
                _keyList.Remove((TKey)key);
        }

        public void CopyTo(Array array, int index)
        {
            foreach (var item in this)
                array.SetValue(item, index++);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            // Assuming this succeeds...
            ((IDictionary<TKey, TValue>)_entDict).Add(item);
            // ...add the key to our ordered list
            _keyList.Add(item.Key);
        }

        public void Add(TKey key, TValue value)
        {
            // Assuming this succeeds...
            ((IDictionary<TKey, TValue>)_entDict).Add(key, value);
            // ...add the key to our ordered list
            _keyList.Add(key);
        }

        public void Clear()
        {
            _keyList.Clear();
            ((IDictionary<TKey, TValue>)_entDict).Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>)_entDict).Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return ((IDictionary<TKey, TValue>)_entDict).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<TKey, TValue>)_entDict).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ((IDictionary<TKey, TValue>)_entDict).GetEnumerator();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return _keyList.Remove(item.Key)
                    && ((IDictionary<TKey, TValue>)_entDict).Remove(item);
        }

        public bool Remove(TKey key)
        {
            return _keyList.Remove(key)
                    && ((IDictionary<TKey, TValue>)_entDict).Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return ((IDictionary<TKey, TValue>)_entDict).TryGetValue(key, out value);
        }

        IDictionaryEnumerator IOrderedDictionary.GetEnumerator()
        {
            return new IndexedDictionaryEnumerator(_entDict.GetEnumerator());
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return ((IOrderedDictionary)this).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary<TKey, TValue>)_entDict).GetEnumerator();
        }
        public class IndexedDictionaryEnumerator : IDictionaryEnumerator
        {
            private Dictionary<TKey, TValue>.Enumerator _baseEnum;
#pragma warning disable RECS0092 // Convert field to readonly
            private DictionaryEntry _Entry;
#pragma warning restore RECS0092 // Convert field to readonly

            public IndexedDictionaryEnumerator(Dictionary<TKey, TValue>.Enumerator baseEnum)
            {
                _baseEnum = baseEnum;
            }

            public bool MoveNext()
            {
                var ret = _baseEnum.MoveNext();
                _Entry.Key = _baseEnum.Current.Key;
                _Entry.Value = _baseEnum.Current.Value;
                return ret;
            }

            /// <summary>
            /// Throws NotImplementedException!
            /// This method of the contract is not implemented.
            /// </summary>
            public void Reset()
            {
                throw new NotImplementedException();
            }

            public object Current
            {
                get
                {
                    return _baseEnum.Current;
                }
            }

            public DictionaryEntry Entry
            {
                get
                {
                    return _Entry;
                }
            }

            public object Key
            {
                get
                {
                    return _Entry.Key;
                }
            }

            public object Value
            {
                get
                {
                    return _Entry.Value;
                }
            }
        }
    }
}
