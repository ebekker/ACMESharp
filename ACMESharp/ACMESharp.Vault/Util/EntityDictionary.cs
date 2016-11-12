using ACMESharp.Util;
using ACMESharp.Vault.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ACMESharp.Vault.Util
{
    public class EntityDictionary<TEntity> : IEnumerable<TEntity>,
        IReadOnlyDictionary<Guid, TEntity>,
        IReadOnlyDictionary<int, TEntity>,
        IReadOnlyDictionary<string, TEntity>
        where TEntity : IIdentifiable
    {
        private readonly IndexedDictionary<Guid, TEntity> _dictById = new IndexedDictionary<Guid, TEntity>();
        private readonly Dictionary<string, TEntity> _dictByAlias = new Dictionary<string, TEntity>();

        public EntityDictionary()
        { }

        public EntityDictionary(IDictionary<Guid, TEntity> dict)
        {
            foreach (var item in dict)
                Add(item.Value);
        }

        public IEnumerable<Guid> Keys
                => _dictById.Keys;

        public IEnumerable<TEntity> Values
                => _dictById.Values;

        public int Count
                => _dictById.Count;

        IEnumerable<int> IReadOnlyDictionary<int, TEntity>.Keys
        {
            get
            {
                for (var i = 0; i < _dictById.Count; ++i)
                    yield return i;
            }
        }

        IEnumerable<string> IReadOnlyDictionary<string, TEntity>.Keys
                => _dictByAlias.Keys;

        public TEntity this[string key]
                => _dictByAlias[key];

        public TEntity this[int key]
                => (TEntity)_dictById[key];

        public TEntity this[Guid key]
                => _dictById[key];

        public void Add(TEntity item)
        {
            _dictById.Add(item.Id, item);
            if (!string.IsNullOrEmpty(item.Alias))
                _dictByAlias.Add(item.Alias, item);
        }

        /// <summary>
        /// Renames the alias under which an existing entity is stored.
        /// </summary>
        /// <param name="entityRef">
        ///     Entity reference for an existing
        ///     entity.  This may include an entity which does not
        ///     currently have an alias, in which case it would only
        ///     include an index or ID.
        /// </param>
        /// <param name="newAlias">
        ///     New alias under which to store
        ///     the resolved entity.  This may specify <c>null</c>
        ///     which would remove an existing entity alias.
        /// </param>
        public void Rename(string entityRef, string newAlias)
        {
            // Do some validations ot make sure we can do this first

            if (string.IsNullOrEmpty(entityRef))
                throw new ArgumentNullException("ref", "invalid or missing reference");

            var ent = GetByRef(entityRef);
            if (ent == null)
                throw new KeyNotFoundException("unresolved existing entity reference")
                        .With(nameof(entityRef), entityRef)
                        .With(nameof(newAlias), newAlias);

            if (_dictByAlias.ContainsKey(newAlias))
            {
                if (object.Equals(_dictByAlias[newAlias], ent))
                    // No need to do anything
                    return;

                throw new System.InvalidOperationException("new alias conflicts with existing entity")
                        .With(nameof(entityRef), entityRef)
                        .With(nameof(newAlias), newAlias);
            }

            // Remove existing old alias(es) if there are any
            var existingAliases = _dictByAlias.Where(
                    _ => _.Value.Id == ent.Id).ToArray();
            foreach (var kv in existingAliases)
                _dictByAlias.Remove(kv.Key);

            if (!string.IsNullOrEmpty(newAlias))
                _dictByAlias.Add(newAlias, ent);
        }

        public void Remove(Guid id)
        {
            var x = this[id];
            if (x != null)
            {
                _dictByAlias.Remove(x.Alias);
                _dictById.Remove(x.Id);
            }
        }

        public TEntity GetByRef(string entityRef, bool throwOnMissing = true,
                TEntity def = default(TEntity))
        {
            if (string.IsNullOrEmpty(entityRef))
                throw new ArgumentNullException("ref", "invalid or missing reference");

            if (entityRef.StartsWith("="))
            {
                // Ref by ID
                var id = Guid.Parse(entityRef.Substring(1));
                if (throwOnMissing || ContainsKey(id))
                    return this[id];
            }
            else if (char.IsDigit(entityRef, 0)
                || (entityRef.Length > 1 && entityRef[0] == '-' && char.IsDigit(entityRef, 1)))
            {
                // Ref by Index
                var index = int.Parse(entityRef);
                if (index < 0)
                    // Index is relative from the end
                    index = Count + index;
                if (throwOnMissing || ContainsKey(index))
                    return this[index];
            }
            else
            {
                // Ref by Alias
                if (throwOnMissing || ContainsKey(entityRef))
                    return this[entityRef];
            }

            return def;
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
            return key >= 0 && _dictById.Count > key;
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
