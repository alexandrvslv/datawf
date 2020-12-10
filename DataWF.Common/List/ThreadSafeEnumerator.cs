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
        private readonly uint count;
        private readonly IList<T> items;
        private T current;

        public ThreadSafeEnumerator(IList<T> items)
        {
            i = 0;
            count = (uint)items.Count;
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
        }
    }
}
