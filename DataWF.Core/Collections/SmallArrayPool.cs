using System;
using System.Buffers;
using System.Collections.Concurrent;

namespace DataWF.Collections;

public class SmallArrayPool<T> : ArrayPool<T>
{
    public static SmallArrayPool<T> Instance { get; } = new();
    private ConcurrentQueue<T[]> q16 = new();
    private ConcurrentQueue<T[]> q8 = new();
    private ConcurrentQueue<T[]> q4 = new();
    private ConcurrentQueue<T[]> q2 = new();

    public override T[] Rent(int minimumLength)
    {
        return minimumLength switch
        {
            > 16 => Shared.Rent(minimumLength),
            > 8 => q16.TryDequeue(out var array16) ? array16 : new T[16],
            > 4 => q8.TryDequeue(out var array8) ? array8 : new T[8],
            > 2 => q4.TryDequeue(out var array4) ? array4 : new T[4],
            > 0 => q2.TryDequeue(out var array2) ? array2 : new T[2],
            _ => throw new ArgumentOutOfRangeException(nameof(minimumLength)),
        };
    }

    public override void Return(T[] array, bool clearArray = false)
    {
        if (clearArray)
            array.AsSpan().Clear();
        switch (array.Length)
        {
            case 2: q2.Enqueue(array); break;
            case 4: q4.Enqueue(array); break;
            case 8: q8.Enqueue(array); break;
            case 16: q16.Enqueue(array); break;
            default: Shared.Return(array, false); break;
        }
    }
}

