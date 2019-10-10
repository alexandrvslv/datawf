using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public class ThreadSafeList<T> : ICollection<T>
    {
        private readonly List<T> list;

        public ThreadSafeList() : this(2)
        { }

        public ThreadSafeList(int capacity)
        {
            list = new List<T>(capacity);
        }

        public int Count => list.Count;

        public bool IsSynchronized => true;

        public object SyncRoot => null;

        public bool IsReadOnly => false;

        public T this[int index]
        {
            get => list[index];
        }

        public void Add(T item) => list.Add(item);

        public void Clear() => list.Clear();

        public bool Contains(T item) => list.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => list.CopyTo(array);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Remove(T item) => list.Remove(item);

        public IEnumerator<T> GetEnumerator() => new ThreadSafeEnumerator<T>(list);

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return list.BinarySearch(item, comparer);
        }

        public void Insert(int index, T item)
        {
            list.Insert(index, item);
        }

        public int IndexOf(T item)
        {
            return list.IndexOf(item);
        }

        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
        }

        public void Sort(IComparer<T> comparer)
        {
            list.Sort(comparer);
        }

        public void AddRange(IEnumerable<T> enumerable)
        {
            list.AddRange(enumerable);
        }
    }
}

