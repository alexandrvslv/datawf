using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public class ThreadSafeList<T> : ICollection<T>
    {
        private T[] items;
        private int count;

        public ThreadSafeList() : this(2)
        { }

        public ThreadSafeList(int capacity)
        {
            items = ArrayPool<T>.Shared.Rent(capacity);// new List<T>(capacity);
        }

        public ThreadSafeList(T item) : this(1)
        {
            Add(item);
        }

        ~ThreadSafeList()
        {
            ArrayPool<T>.Shared.Return(items);
        }

        public int Count => count;

        public bool IsSynchronized => true;

        public object SyncRoot => null;

        public bool IsReadOnly => false;

        public T this[int index]
        {
            get => items[index];
            set => items[index] = value;
        }

        public void Add(T item)
        {
            if ((uint)count >= (uint)items.Length)
            {
                Reallock();
            }
            items[count++] = item;
        }

        private void Reallock()
        {
            var temp = ArrayPool<T>.Shared.Rent(count * 2);
            items.AsSpan(0, count).CopyTo(temp.AsSpan());
            var swap = items;
            items = temp;
            ArrayPool<T>.Shared.Return(swap);
        }

        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index > -1)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            if ((uint)index < (uint)--count)
            {
                items.AsSpan(index + 1, count - index).CopyTo(items.AsSpan(index, count - index));
            }
            items[count] = default(T);
        }

        public void Insert(int index, T item)
        {
            if ((uint)index >= (uint)items.Length)
            {
                Reallock();
            }
            if ((uint)index < (uint)count++)
            {
                items.AsSpan(index, count - index).CopyTo(items.AsSpan(index+1, count - index));
            }
            items[index] = item;
        }

        public void Clear()
        {
            Array.Clear(items, 0, count);
            count = 0;
        }

        public bool Contains(T item) => Array.IndexOf(items, item) > -1;

        public void CopyTo(T[] array, int arrayIndex) => items.AsSpan(0, count).CopyTo(array.AsSpan(arrayIndex));

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<T> GetEnumerator() => count == 0 ? (IEnumerator<T>)EmptyEnumerator<T>.Default : new ThreadSafeArrayEnumerator<T>(items, count);

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return items.AsSpan(0, count).BinarySearch(item, comparer);// ListHelper.BinarySearch(list, item, comparer, false);
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf(items, item, 0, (int)count);
        }

        public void Sort(IComparer<T> comparer)
        {
            Array.Sort(items, 0, count, comparer);
        }

        public void AddRange(IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
                Add(item);
        }

        public T SelectOne()
        {
            return count > 0 ? items[0] : default(T);
        }
    }
}

