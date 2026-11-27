using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections;

[Serializable]
public struct OneArrayEnumerator<T> : IEnumerator<T>, IEnumerator
{
    public static readonly OneArrayEnumerator<T> Empty = new(null, 0);
    private int i;
    private uint count;
    private T[]? items;
    private T single;

    public OneArrayEnumerator(T single)
    {
        i = -1;
        this.count = 1;
        items = null;
        this.single = single;
    }

    public OneArrayEnumerator(T[]? items, int count)
    {
        i = -1;
        this.count = (uint)count;
        this.items = items;
        single = default!;
    }

    public T Current => items != null ? items[i] : single;

    object IEnumerator.Current => Current!;

    public void Dispose() { }

    public bool MoveNext() => (uint)++i < count;

    public void Reset() => i = -1;
}
