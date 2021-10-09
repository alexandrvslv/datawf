using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public class ThreadSafeList<T> : ICollection<T>
    {
        private T single;
        private T[] items;
        private int _count;
        private int _capacity;//TODO REMOVE
        public ThreadSafeList()
        {
            //items = SmallArrayPool<T>.Instance.Rent(capacity);// new List<T>(capacity);
            _capacity = 1;
        }

        public ThreadSafeList(T item) : this()
        {
            single = item;
            _count = 1;
        }

        ~ThreadSafeList()
        {
            if (items != null)
                SmallArrayPool<T>.Instance.Return(items);
        }

        public int Count => _count;

        public int Capacity => _capacity;

        public bool IsSynchronized => true;

        public object SyncRoot => null;

        public bool IsReadOnly => false;

        public T this[int index]
        {
            get => items != null ? items[index] : single;
            set
            {
                if (items != null)
                    items[index] = value;
                else
                    single = value;
            }
        }

        public void Add(T item)
        {
            if ((uint)_count >= (uint)_capacity)
            {
                Reallock();
            }
            if (_count == 0)
            {
                single = item;
                _count++;
            }
            else
                items[_count++] = item;
        }

        private void Reallock()
        {
            var temp = SmallArrayPool<T>.Instance.Rent(Math.Max(_count, 2) * 2);
            if (items != null)
                items.AsSpan(0, _count).CopyTo(temp.AsSpan());
            else if (_count > 0)
                temp[0] = single;
            var swap = items;
            items = temp;
            _capacity = items.Length;
            if (swap != null)
                SmallArrayPool<T>.Instance.Return(swap);
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
            _count--;
            if ((uint)index < (uint)_count)
            {
                items.AsSpan(index + 1, _count - index).CopyTo(items.AsSpan(index, _count - index));
            }
            //items[_count] = default(T);
        }

        public void Insert(int index, T item)
        {
            if ((uint)_count >= (uint)_capacity)
            {
                Reallock();
            }
            if ((uint)index < (uint)_count)
            {
                var copyCount = _count - index;
                items.AsSpan(index, copyCount).CopyTo(items.AsSpan(index + 1, copyCount));
            }
            _count++;
            items[index] = item;
        }

        public int IndexOf(T item)
        {
            return items != null
                ? Array.IndexOf(items, item, 0, (int)_count)
                : EqualityComparer<T>.Default.Equals(item, single) ? 0 : -1;
        }

        public void Clear()
        {
            if (items != null)
                Array.Clear(items, 0, _count);
            _count = 0;
            single = default(T);
        }

        public bool Contains(T item) => IndexOf(item) > -1;

        public void CopyTo(T[] array, int arrayIndex) => items.AsSpan(0, _count).CopyTo(array.AsSpan(arrayIndex));

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        public ThreadSafeArrayEnumerator<T> GetEnumerator() => _count == 0
            ? ThreadSafeArrayEnumerator<T>.Empty
            : items == null
                ? new ThreadSafeArrayEnumerator<T>(single, _count)
                : new ThreadSafeArrayEnumerator<T>(items, _count);

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            if (items != null)
            {
                return Array.BinarySearch(items, 0, _count, item, comparer);
                //ListHelper.BinarySearch(items, 0, _count - 1, item, comparer, false);//
            }

            var result = comparer.Compare(item, single);

            return result == 0 ? 0 : result > 0 ? -2 : -1;
        }

        public void Sort(IComparer<T> comparer)
        {
            if (items != null)
                Array.Sort(items, 0, _count, comparer);
        }

        public void AddRange(IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
                Add(item);
        }

        public T FirstOrDefault()
        {
            return items == null
                ? single
                : _count > 0 ? items[0] : default(T);
        }

        public ReadOnlySpan<T> AsSpan()
        {
            if (items == null) Reallock();
            return items.AsSpan(0, _count);
        }

        public ReadOnlyList<T> AsReadOnly() => new ReadOnlyList<T>(items, _count);
    }

    public struct ReadOnlyList<T> : IReadOnlyList<T>
    {
        public static IReadOnlyList<T> Empty = new ReadOnlyList<T>(null, 0);

        private readonly T[] array;
        private readonly int count;

        public ReadOnlyList(T[] array, int count)
        {
            this.array = array;
            this.count = count;
        }
        public T this[int index] => array[index];

        public int Count => count;


        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        public ThreadSafeArrayEnumerator<T> GetEnumerator() => count == 0 ? ThreadSafeArrayEnumerator<T>.Empty : new ThreadSafeArrayEnumerator<T>(array, count);
    }
}

