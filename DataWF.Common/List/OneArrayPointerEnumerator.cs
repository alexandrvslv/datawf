using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    [Serializable]
    public struct OneArrayPointerEnumerator<T> : IEnumerator<T>, IEnumerator, IDisposable
    {
        public static readonly OneArrayPointerEnumerator<T> Empty = new OneArrayPointerEnumerator<T>(null, 0);
        private int i;
        private uint count;
        private ArrayPointer<T> items;
        private T single;

        public OneArrayPointerEnumerator
            (T single, int count)
        {
            i = -1;
            this.count = (uint)count;
            items = null;
            this.single = single;
        }

        public OneArrayPointerEnumerator(ArrayPointer<T> items, int count)
        {
            i = -1;
            this.count = (uint)count;
            this.items = items;
            single = default(T);
            items?.Subscribe();
        }

        public T Current => items != null ? items[i] : single;

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            items?.Unsubscribe();
        }

        public bool MoveNext()
        {
            return (uint)++i < count;
        }

        public void Reset()
        {
            i = -1;
        }
    }


}
