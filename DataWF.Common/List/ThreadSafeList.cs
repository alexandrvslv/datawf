using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public class ThreadSafeList<T> : ICollection<T>
    {
        private T[] items;
        private int _count;
        private int _capacity;
        public ThreadSafeList() : this(2)
        { }

        public ThreadSafeList(int capacity)
        {
            items = ArrayPool<T>.Shared.Rent(capacity);// new List<T>(capacity);
            _capacity = capacity;
        }

        public ThreadSafeList(T item) : this(1)
        {
            Add(item);
        }

        ~ThreadSafeList()
        {
            ArrayPool<T>.Shared.Return(items);
        }

        public int Count => _count;
        public int Capacity => _capacity;

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
            if ((uint)_count >= (uint)_capacity)
            {
                Reallock();
            }
            items[_count++] = item;
        }

        private void Reallock()
        {
            var temp = ArrayPool<T>.Shared.Rent(Math.Max(_count, 2) * 2);
            items.AsSpan(0, _count).CopyTo(temp.AsSpan());
            var swap = items;
            items = temp;
            _capacity = items.Length;
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
            if ((uint)index < (uint)--_count)
            {
                items.AsSpan(index + 1, _count - index).CopyTo(items.AsSpan(index, _count - index));
            }
            items[_count] = default(T);
        }

        public void Insert(int index, T item)
        {
            if ((uint)index >= (uint)_capacity)
            {
                Reallock();
            }
            if ((uint)index < (uint)_count++)
            {
                items.AsSpan(index, _count - index).CopyTo(items.AsSpan(index + 1, _count - index));
            }
            items[index] = item;
        }

        public void Clear()
        {
            Array.Clear(items, 0, _count);
            _count = 0;
        }

        public bool Contains(T item) => Array.IndexOf(items, item) > -1;

        public void CopyTo(T[] array, int arrayIndex) => items.AsSpan(0, _count).CopyTo(array.AsSpan(arrayIndex));

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<T> GetEnumerator() => _count == 0 ? (IEnumerator<T>)EmptyEnumerator<T>.Default : new ThreadSafeArrayEnumerator<T>(items, _count);

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return Array.BinarySearch(items, 0, _count, item, comparer); //ListHelper.BinarySearch(items, 0, count, item, comparer, false);
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf(items, item, 0, (int)_count);
        }

        public void Sort(IComparer<T> comparer)
        {
            Array.Sort(items, 0, _count, comparer);
        }

        public void AddRange(IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
                Add(item);
        }

        public T SelectOne()
        {
            return _count > 0 ? items[0] : default(T);
        }
    }
}

