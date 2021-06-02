using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public readonly struct EmptyEnumerator<T> : IEnumerator<T>
    {
        public static readonly EmptyEnumerator<T> Default = new EmptyEnumerator<T>();

        public T Current => default(T);

        object IEnumerator.Current => null;

        public void Dispose() { }
        public bool MoveNext() => false;
        public void Reset() { }
    }
}
