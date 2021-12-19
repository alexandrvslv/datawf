using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace DataWF.Common
{
    public class PullArray<T> : GenericPull<T>, IEnumerable<T>
    {
        protected T[][] array = new T[16][];
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
                    Array.Clear(item, 0, blockSize);
                    array[i] = null;
                }
            }
            blockCount = 0;
            Array.Clear(array, 0, array.Length);
        }

        public override bool EqualNull(object value)
        {
            return value == null;
        }

        public override object Get(int index)
        {
            return GetValue(PullHandler.FromSeqence(index, blockSize));
        }

        public override object Get(int block, int blockIndex)
        {
            return GetValue(new PullHandler(block, blockIndex));
        }

        public override void Set(int index, object value)
        {
            SetValue(PullHandler.FromSeqence(index, blockSize), (T)value);
        }

        public override void Set(int block, int blockIndex, object value)
        {
            SetValue(new PullHandler(block, blockIndex), (T)value);
        }

        public override T GetValue(in PullHandler handler)
        {
            if ((uint)handler.Block >= (uint)blockCount)
            {
                return default(T);
            }
            var arrayBlock = array[handler.Block];
            return arrayBlock != null ? arrayBlock[handler.BlockIndex] : default(T);
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override void Trunc(int maxIndex)
        {
            (short block, short blockIndex) = Helper.OneToTwoShift(maxIndex);
            while (block < blockCount - 1)
            {
                Array.Clear(array[blockCount - 1], 0, blockSize);
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
