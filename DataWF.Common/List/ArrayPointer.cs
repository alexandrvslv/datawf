using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace DataWF.Common
{
    public class ArrayPointer<T>
    {
        private int subscriptions;

        public ArrayPointer(T[] array)
        {
            Array = array;
            subscriptions = 1;
        }

        public ArrayPointer(int minSize)
            : this(SmallArrayPool<T>.Instance.Rent(minSize))
        { }

        public readonly T[] Array;

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Array[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Array[index] = value;
        }
        
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Array.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int start, int length) => Array.AsSpan(start, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    }
}

