using System.Collections.Generic;

namespace DataWF.Common
{
    public class IndexedList<T, K> : SelectableList<T>
    {
        private readonly Dictionary<K, int> cache;

        public IndexedList(IEqualityComparer<K> comparer, IInvoker<T, K> keyInvoker)
        {
            CheckUnique = false;
            cache = new Dictionary<K, int>(items.Capacity, comparer);
            KeyInvoker = keyInvoker;
        }

        public IInvoker<T, K> KeyInvoker { get; }

        public override int AddInternal(T item)
        {
            return cache[KeyInvoker.GetValue(item)] = base.AddInternal(item);
        }

        public override bool Remove(T item)
        {
            return base.Remove(item) && cache.Remove(KeyInvoker.GetValue(item));
        }

        public override bool Contains(T item)
        {
            return cache.ContainsKey(KeyInvoker.GetValue(item));
        }

        public bool TryGetIndex(K key, out int index)
        {
            return cache.TryGetValue(key, out index);
        }
    }
}

