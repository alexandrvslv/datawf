using System;
using System.Buffers;
using System.Collections.Concurrent;

namespace DataWF.Common
{
    public class SmallArrayPool<T> : ArrayPool<T>
    {
        public static SmallArrayPool<T> Instance { get; } = new SmallArrayPool<T>();
        private ConcurrentQueue<T[]> eight = new ConcurrentQueue<T[]>();
        private ConcurrentQueue<T[]> four = new ConcurrentQueue<T[]>();
        private ConcurrentQueue<T[]> two = new ConcurrentQueue<T[]>();

        public override T[] Rent(int minimumLength)
        {
            if (minimumLength >= 16)
                return Shared.Rent(minimumLength);
            if (minimumLength >= 8)
                return eight.TryDequeue(out var eightArray) ? eightArray : new T[8];
            if (minimumLength >= 4)
                return four.TryDequeue(out var fourArray) ? fourArray : new T[4];
            return two.TryDequeue(out var twoArray) ? twoArray : new T[2];
        }

        public override void Return(T[] array, bool clearArray = false)
        {
            if (clearArray)
                array.AsSpan().Clear();
            switch (array.Length)
            {
                case 2: two.Enqueue(array); break;
                case 4: four.Enqueue(array); break;
                case 8: eight.Enqueue(array); break;
                default: Shared.Return(array, false); break;
            }
        }
    }
}

