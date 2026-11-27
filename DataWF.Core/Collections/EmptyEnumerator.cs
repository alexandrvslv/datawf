using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections;

public readonly struct EmptyEnumerator<T> : IEnumerator<T>
{
    public static readonly EmptyEnumerator<T> Default = new();

    public T Current => default!;

    object IEnumerator.Current => Current!;

    public void Dispose() { }
    public bool MoveNext() => false;
    public void Reset() { }
}
