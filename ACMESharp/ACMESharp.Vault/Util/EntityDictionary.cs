using System;
using System.Collections;
using System.Collections.Generic;

namespace ACMESharp.Vault.Util
{
    public class EntityDictionary<TEntity> : IEnumerable<TEntity>,
        IReadOnlyDictionary<Guid, TEntity>,
        IReadOnlyDictionary<int, TEntity>,
        IReadOnlyDictionary<string, TEntity>
        where TEntity : IIdentifiable
    {
        private IndexedDictionary<Guid, TEntity> _dictById = new IndexedDictionary<Guid, TEntity>();
        private Dictionary<string, TEntity> _dictByAlias = new Dictionary<string, TEntity>();

        public EntityDictionary()
        { }

        public EntityDictionary(IDictionary<Guid, TEntity> dict)
        {
            foreach (var item in dict)
                Add(item.Value);
        }

        public IEnumerable<Guid> Keys
        {
            get
            {
                return _dictById.Keys;
            }
        }

        public IEnumerable<TEntity> Values
        {
            get
            {
                return _dictById.Values;
            }
        }

        public int Count
        {
            get
            {
                return _dictById.Count;
            }
        }

        IEnumerable<int> IReadOnlyDictionary<int, TEntity>.Keys
        {
            get
            {
                for (int i = 0; i < _dictById.Count; ++i)
                    yield return i;
            }
        }

        IEnumerable<string> IReadOnlyDictionary<string, TEntity>.Keys
        {
            get
            {
                return _dictByAlias.Keys;
            }
        }

        public TEntity this[string key]
        {
            get
            {
                return _dictByAlias[key];
            }
        }

        public TEntity this[int key]
        {
            get
            {
                return (TEntity)_dictById[key];
            }
        }

        public TEntity this[Guid key]
        {
            get
            {
                return _dictById[key];
            }
        }

        public void Add(TEntity item)
        {
            _dictById.Add(item.Id, item);
            if (!string.IsNullOrEmpty(item.Alias))
                _dictByAlias.Add(item.Alias, item);
        }

        public TEntity GetByRef(string entityRef)
        {
            if (string.IsNullOrEmpty(entityRef))
                throw new ArgumentNullException("ref", "Invalid or missing reference");

            if (entityRef.StartsWith("="))
            {
                // Ref by ID
                var id = Guid.Parse(entityRef.Substring(1));
                return this[id];
            }
            else if (char.IsNumber(entityRef, 0))
            {
                // Ref by Index
                return this[int.Parse(entityRef)];
            }
            else
            {
                // Ref by Alias
                return this[entityRef];
            }
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            foreach (var item in _dictById)
                yield return item.Value;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public bool ContainsKey(Guid key)
        {
            return _dictById.ContainsKey(key);
        }

        public bool TryGetValue(Guid key, out TEntity value)
        {
            return _dictById.TryGetValue(key, out value);
        }

        IEnumerator<KeyValuePair<Guid, TEntity>> IEnumerable<KeyValuePair<Guid, TEntity>>.GetEnumerator()
        {
            return _dictById.GetEnumerator();
        }

        public bool ContainsKey(int key)
        {
            return _dictById.Count > key;
        }

        public bool TryGetValue(int key, out TEntity value)
        {
            try
            {
                value = (TEntity)_dictById[key];
                return true;
            }
            catch (Exception)
            {
                value = default(TEntity);
                return false;
            }
        }

        IEnumerator<KeyValuePair<int, TEntity>> IEnumerable<KeyValuePair<int, TEntity>>.GetEnumerator()
        {
            var index = 0;
            foreach (var item in _dictById)
                yield return new KeyValuePair<int, TEntity>(index++, item.Value);
        }

        public bool ContainsKey(string key)
        {
            return _dictByAlias.ContainsKey(key);
        }

        public bool TryGetValue(string key, out TEntity value)
        {
            return _dictByAlias.TryGetValue(key, out value);
        }

        IEnumerator<KeyValuePair<string, TEntity>> IEnumerable<KeyValuePair<string, TEntity>>.GetEnumerator()
        {
            return _dictByAlias.GetEnumerator();
        }
    }
}
