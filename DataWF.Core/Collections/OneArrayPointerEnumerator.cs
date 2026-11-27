using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections
{
    [Serializable]
    public struct OneArrayPointerEnumerator<T> : IEnumerator<T>, IEnumerator, IDisposable
    {
        public static readonly OneArrayPointerEnumerator<T> Empty = new(null, 0);
        private int i;
        private uint count;
        private ArrayPointer<T>? items;
        private T single;

        public OneArrayPointerEnumerator(T single)
        {
            i = -1;
            this.count = 1;
            items = null;
            this.single = single;
        }

        public OneArrayPointerEnumerator(ArrayPointer<T>? items, int count)
        {
            i = -1;
            this.count = (uint)count;
            this.items = items;
            single = default!;
            items?.Subscribe();
        }

        public T Current => items != null ? items[i] : single;

        object IEnumerator.Current => Current!;

        public void Dispose() => items?.Unsubscribe();

        public bool MoveNext() => (uint)++i < count;

        public void Reset() => i = -1;

        public bool IsEmpty() => count == 0;
    }


}
