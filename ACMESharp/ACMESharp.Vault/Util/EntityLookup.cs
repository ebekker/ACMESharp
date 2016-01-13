using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ACMESharp.POSH.Util
{
    public class EntityLookup<TEntity> : IEnumerable<TEntity>, ILookup<Guid, TEntity>, ILookup<int, TEntity>, ILookup<string, TEntity>
        where TEntity : IIdentifiable
    {
        private IndexedDictionary<string, TEntity> _dict = new IndexedDictionary<string, TEntity>();

        IEnumerable<TEntity> ILookup<int, TEntity>.this[int key]
        {
            get
            {
                yield return (TEntity)_dict[key];
            }
        }

        public IEnumerable<TEntity> this[string key]
        {
            get
            {
                yield return _dict[key];
            }
        }

        public int Count
        {
            get
            {
                return _dict.Count;
            }
        }

        int ILookup<int, TEntity>.Count
        {
            get
            {
                return _dict.Count;
            }
        }

        public IEnumerable<TEntity> this[Guid key]
        {
            get
            {
                return _dict.Where(x => x.Value.Id == key).Select(x => x.Value);
            }
        }

        public void Add(TEntity item)
        {
            _dict.Add(item.Alias, item);
        }

        public bool Contains(string key)
        {
            return _dict.ContainsKey(key);
        }

        public IEnumerator<IGrouping<string, TEntity>> GetEnumerator()
        {
            foreach (var item in _dict)
                yield return new EntityGrouping<string>
                {
                    Key = item.Key,
                    Value = item.Value,
                };
        }

        bool ILookup<int, TEntity>.Contains(int key)
        {
            return _dict.Count <= key;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        IEnumerator<IGrouping<int, TEntity>> IEnumerable<IGrouping<int, TEntity>>.GetEnumerator()
        {
            int index = 0;
            foreach (var item in _dict)
            {
                yield return new EntityGrouping<int>
                {
                    Key = index++,
                    Value = item.Value,
                };
            }
        }

        public bool Contains(Guid key)
        {
            return _dict.Count(x => x.Value.Id == key) > 0;
        }

        IEnumerator<IGrouping<Guid, TEntity>> IEnumerable<IGrouping<Guid, TEntity>>.GetEnumerator()
        {
            foreach (var item in _dict)
            yield return new EntityGrouping<Guid>
            {
                Key = item.Value.Id,
                Value = item.Value,
            };
        }

        IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator()
        {
            foreach (var item in _dict)
                yield return item.Value;
        }

        public class EntityGrouping<TKey> : IGrouping<TKey, TEntity>
        {
            public TKey Key
            { get; set; }

            public TEntity Value
            { get; set; }

            public IEnumerator<TEntity> GetEnumerator()
            {
                yield return Value;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }
    }
}
