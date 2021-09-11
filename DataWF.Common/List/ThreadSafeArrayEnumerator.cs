using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    [Serializable]
    public struct ThreadSafeArrayEnumerator<T> : IEnumerator<T>, IEnumerator
    {
        public static readonly ThreadSafeArrayEnumerator<T> Empty = new ThreadSafeArrayEnumerator<T>(null, 0);
        private int i;
        private uint count;
        private T[] items;

        public ThreadSafeArrayEnumerator(T[] items, int count)
        {
            i = -1;
            this.count = (uint)count;
            this.items = items;
        }

        public T Current => items[i];

        object IEnumerator.Current => Current;

        public void Dispose()
        { }

        public bool MoveNext()
        {
            i++;
            return (uint)i < count;
        }

        public void Reset()
        {
            i = -1;
        }
    }

    
}
