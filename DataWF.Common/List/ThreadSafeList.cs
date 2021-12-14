using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace DataWF.Common
{
    public class ThreadSafeList<T> : ICollection<T>
    {
        private T single;
        private ArrayPointer<T> items;
        private int _count;

        public ThreadSafeList()
        {
            //items = SmallArrayPool<T>.Instance.Rent(capacity);// new List<T>(capacity);
        }

        public ThreadSafeList(T item) : this()
        {
            single = item;
            _count = 1;
        }

        ~ThreadSafeList()
        {
            items?.Unsubscribe();
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }
        
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => items?.Length ?? 1;
        }

        public bool IsSynchronized => true;

        public object SyncRoot => null;

        public bool IsReadOnly => false;

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            if ((uint)_count >= (uint)Capacity)
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
            var temp = new ArrayPointer<T>(Math.Max(_count, 2) * 2);
            if (items != null)
                items.AsSpan(0, _count).CopyTo(temp.AsSpan());
            else if (_count > 0)
                temp[0] = single;
            var swap = items;
            items = temp;

            swap?.Unsubscribe();
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
            if ((uint)_count >= (uint)Capacity)
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
                ? Array.IndexOf(items.Array, item, 0, (int)_count)
                : EqualityComparer<T>.Default.Equals(item, single) ? 0 : -1;
        }

        public void Clear()
        {
            if (items != null)
                Array.Clear(items.Array, 0, _count);
            _count = 0;
            single = default(T);
        }

        public bool Contains(T item) => IndexOf(item) > -1;

        public void CopyTo(T[] array, int arrayIndex) => items.AsSpan(0, _count).CopyTo(array.AsSpan(arrayIndex));

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        public OneArrayPointerEnumerator<T> GetEnumerator() => _count == 0
            ? OneArrayPointerEnumerator<T>.Empty
            : items == null
                ? new OneArrayPointerEnumerator<T>(single, _count)
                : new OneArrayPointerEnumerator<T>(items, _count);

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            if (items != null)
            {
                return Array.BinarySearch(items.Array, 0, _count, item, comparer);
                //ListHelper.BinarySearch(items, 0, _count - 1, item, comparer, false);//
            }

            var result = comparer.Compare(item, single);

            return result == 0 ? 0 : result > 0 ? -2 : -1;
        }

        public void Sort(IComparer<T> comparer)
        {
            if (items != null)
                Array.Sort(items.Array, 0, _count, comparer);
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

        public ReadOnlyList<T> AsReadOnly() => new ReadOnlyList<T>(items.Array, _count);
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

        public OneArrayEnumerator<T> GetEnumerator() => count == 0 
            ? OneArrayEnumerator<T>.Empty 
            : new OneArrayEnumerator<T>(array, count);
        
    }
}

