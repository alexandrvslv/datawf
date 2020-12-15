using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public struct EmptyEnumerator<T> : IEnumerator<T>
    {
        public static readonly EmptyEnumerator<T> Default = new EmptyEnumerator<T>();

        public T Current => default(T);

        object IEnumerator.Current => null;

        public void Dispose() { }
        public bool MoveNext() => false;
        public void Reset() { }
    }

    [Serializable]
    public struct ThreadSafeEnumerator<T> : IEnumerator<T>, IEnumerator
    {
        private int i;
        private readonly uint count;
        private readonly IList<T> items;
        private T current;

        public ThreadSafeEnumerator(IList<T> items) : this(items, items.Count)
        { }

        public ThreadSafeEnumerator(IList<T> items, int count)
        {
            i = 0;
            this.count = (uint)count;
            this.items = items;
            current = default(T);
        }

        public T Current
        {
            get => current;
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        { }

        public bool MoveNext()
        {
            try
            {
                if ((uint)i < count)
                {
                    current = items[i++];
                    return true;
                }
                current = default(T);
                return false;
            }
            catch (Exception e)
            {
                Helper.OnException(e);
                return false;
            }
        }

        public void Reset()
        {
            i = 0;
            current = default(T);
        }
    }


    [Serializable]
    public struct ThreadSafeArrayEnumerator<T> : IEnumerator<T>, IEnumerator
    {
        private int i;
        private readonly uint count;
        private readonly T[] items;
        private T current;

        public ThreadSafeArrayEnumerator(T[] items, int count)
        {
            i = 0;
            this.count = (uint)count;
            this.items = items;
            current = default(T);
        }

        public T Current
        {
            get => current;
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        { }

        public bool MoveNext()
        {
            if ((uint)i < count)
            {
                current = items[i++];
                return true;
            }
            current = default(T);
            return false;
        }

        public void Reset()
        {
            i = 0;
            current = default(T);
        }
    }
}
