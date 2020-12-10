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

    public struct ThreadSafeEnumerator<T> : IEnumerator<T>
    {
        private int i;
        private readonly int count;
        private readonly IList<T> items;
        private T current;

        public ThreadSafeEnumerator(IList<T> items)
        {
            i = -1;
            count = items.Count;
            this.items = items;
            current = default(T);
        }

        public T Current
        {
            get => current;
            private set => current = value;
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            Current = default(T);
            i = -1;
        }

        public bool MoveNext()
        {
            if (++i >= count)
            {
                current = default(T);
                return false;
            }
            try
            {
                current = items[i];
                return true;
            }
            catch (Exception e)
            {
                Helper.OnException(e);
                return false;
            }
        }

        public void Reset()
        {
            i = -1;
        }
    }
}
