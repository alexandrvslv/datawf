using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{

    [Serializable]
    public struct OneListEnumerator<T> : IEnumerator<T>, IEnumerator
    {
        public static readonly OneListEnumerator<T> Empty = new OneListEnumerator<T>(null, 0);

        private int i;
        private uint count;
        private IList<T> items;
        private T current;

        public OneListEnumerator(IList<T> items, int count)
        {
            i = -1;
            this.count = (uint)count;
            this.items = items;
            this.current = default(T);
        }

        public OneListEnumerator(IList<T> items)
            : this(items, items.Count)
        { }

        public T Current => current;

        object IEnumerator.Current => Current;

        public void Dispose()
        { }

        public bool MoveNext()
        {
            var moved = (uint)++i < count;
            if (moved)
            {
                try { current = items[i]; }
                catch (Exception e)
                {
                    Helper.OnException(e);
                    moved = false;
                }
            }
            return moved;
        }

        public void Reset()
        {
            i = -1;
        }
    }
}
