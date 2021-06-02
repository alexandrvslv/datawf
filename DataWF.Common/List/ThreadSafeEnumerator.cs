using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{

    [Serializable]
    public struct ThreadSafeEnumerator<T> : IEnumerator<T>, IEnumerator
    {
        public static readonly ThreadSafeEnumerator<T> Empty = new ThreadSafeEnumerator<T>(null, 0);

        private int i;
        private uint count;
        private IList<T> items;
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

        public T Current => current;

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
}
