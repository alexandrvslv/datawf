using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public class ThreadSafeList<T> : ICollection<T>
    {
        private List<T> list = new List<T>();

        public int Count => list.Count;

        public bool IsSynchronized => true;

        public object SyncRoot => null;

        public bool IsReadOnly => false;

        public void Add(T item) => list.Add(item);

        public void Clear() => list.Clear();

        public bool Contains(T item) => list.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => list.CopyTo(array);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Remove(T item) => list.Remove(item);

        public IEnumerator<T> GetEnumerator() => new ThreadSafeEnumerator<T>(list);
    }
}

