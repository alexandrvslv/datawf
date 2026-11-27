using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace DataWF.Collections;
// Подсчитывает все обращения к массиву, например для перечисления
// Нужен для того чтобы при реалокации Списков из пула задействованные в выборке не потеряли значения
public class ArrayPointer<T>
{
    public static ArrayPointer<T> Empty => new(System.Array.Empty<T>());
    private int subscriptions;

    private ArrayPointer(T[] array)
    {
        Array = array;
        subscriptions = 1;
    }

    public ArrayPointer(int minSize)
        : this(SmallArrayPool<T>.Instance.Rent(minSize))
    { }

    public readonly T[] Array;

    public ref T this[int index]
    {
        get => ref Array[index];
        //set => Array[index] = value;
    }

    public int Length
    {
        get => Array.Length;
    }

    public Span<T> AsSpan(int start, int length) => Array.AsSpan(start, length);

    public Span<T> AsSpan() => Array.AsSpan();

    public void Subscribe()
    {
        Interlocked.Increment(ref subscriptions);
    }

    public void Unsubscribe(bool clearArray = false)
    {
        if (Interlocked.Decrement(ref subscriptions) <= 0)
            SmallArrayPool<T>.Instance.Return(Array, clearArray);
    }

    public void Clear(int start, int length)
    {
        Array.AsSpan(start, length).Clear();
    }
}

