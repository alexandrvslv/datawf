using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace DataWF.Collections.Pulls
{
    public class PullArray<T> : GenericPull<T>, IEnumerable<T>
    {
        protected T[]?[] array = new T[16][];
        private PullHandler maxIndex;

        public PullArray(int blockSize) : base(blockSize)
        { }

        public override int Capacity => array.Length * blockSize;

        public int Count => maxIndex.GetSeqence(blockSize);

        public override void Clear()
        {
            for (int i = 0; i < blockCount; i++)
            {
                var item = array[i];
                if (item != null)
                {
                    item.AsSpan(0, blockSize).Clear();
                    array[i] = null;
                }
            }
            blockCount = 0;
            array.AsSpan(0, array.Length).Clear();
        }

        public override bool EqualNull(object value)
        {
            return value == null;
        }

        public override T GetValue(in PullHandler handler)
        {
            if ((uint)handler.Block >= (uint)blockCount)
            {
                return default!;
            }
            var arrayBlock = array[handler.Block];
            return arrayBlock != null ? arrayBlock[handler.BlockIndex] : default!;
        }

        public override void SetValue(in PullHandler handler, T value)
        {
            if ((uint)handler.Block >= (uint)blockCount)
            {
                var blockAdd = (handler.Block + 1) - blockCount;
                Interlocked.Add(ref blockCount, blockAdd);
                if ((uint)blockCount >= (uint)array.Length)
                {
                    Reallocate(blockCount);
                }
            }
            var arrayBlock = array[handler.Block];
            if (arrayBlock == null)
            {
                array[handler.Block] = arrayBlock = new T[blockSize];
            }
            arrayBlock[handler.BlockIndex] = value;
            maxIndex = PullHandler.Max(maxIndex, handler);
        }

        private void Reallocate(int minCount)
        {
            var size = array.Length;
            while (size < minCount)
            {
                size += 32;
            }
            var temp = new T[size][];
            array.AsSpan().CopyTo(temp.AsSpan());
            array = temp;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var sequence = maxIndex.GetSeqence(blockSize);
            for (int i = 0; i <= sequence; i++)
            {
                yield return GetValue(i);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override void Trunc(int maxIndex)
        {
            (short block, short blockIndex) = Helper.OneToTwoShift(maxIndex);
            while (block < blockCount - 1)
            {
                array[blockCount - 1].AsSpan(0, blockSize).Clear();
                array[blockCount - 1] = null;
                Interlocked.Decrement(ref blockCount);
            }
            if (block < blockCount && blockIndex + 1 < BlockSize)
            {
                Memset<T>(array[block], default(T), blockIndex + 1);
            }
        }
    }

}
