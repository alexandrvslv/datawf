using System.Collections.Generic;

namespace DataWF.Common
{
    public class IndexedList<T> : SelectableList<T>
    {
        private Dictionary<T, int> cache = new Dictionary<T, int>();
        public IndexedList(IEqualityComparer<T> comparer)
        {
            cache = new Dictionary<T, int>(items.Capacity, comparer);
        }

        public override int Add(T item)
        {
            return cache[item] = base.Add(item);
        }

        public override bool Remove(T item)
        {
            return base.Remove(item) && cache.Remove(item);
        }

        public override bool Contains(T item)
        {
            return cache.ContainsKey(item);
        }

        public bool TryGetIndex(T value, out int index)
        {
            return cache.TryGetValue(value, out index);
        }
    }
}

