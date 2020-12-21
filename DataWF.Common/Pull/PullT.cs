using System;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public class Pull<T> : GenericPull<T>, IEnumerable<T>
    {
        private readonly List<T[]> array = new List<T[]>();
        private int maxIndex;

        public Pull(int blockSize) : base(blockSize)
        { }

        public override int Capacity => array.Count * blockSize;

        public int Count => maxIndex;

        public override void Clear()
        {
            foreach (var a in array)
            {
                if (a != null)
                {
                    Array.Clear(a, 0, a.Length);
                }
            }
            blockCount = 0;
            array.Clear();
        }

        public override bool EqualNull(object value)
        {
            return value == null;
        }

        public override object Get(int index)
        {
            return GetValue(PullHandler.FromSeqence(index, blockSize));
        }

        public override void Set(int index, object value)
        {
            SetValue(PullHandler.FromSeqence(index, blockSize), (T)value);
        }

        public override object Get(int block, int blockIndex)
        {
            return GetValue(new PullHandler(block, blockIndex));
        }

        public override void Set(int block, int blockIndex, object value)
        {
            SetValue(block, blockIndex, (T)value);
        }

        public override T GetValue(in PullHandler handler)
        {
            if ((uint)handler.Block >= (uint)blockCount)
            {
                return default(T);
            }
            var block = array[handler.Block];
            return block != null ? block[handler.BlockIndex] : default(T);
        }

        public override void SetValue(in PullHandler handler, T value)
        {
            if ((uint)handler.Block >= (uint)blockCount)
            {
                var blockAdd = (handler.Block + 1) - blockCount;
                array.AddRange(Enumerable.Repeat((T[])null, blockAdd));
                Interlocked.Add(ref blockCount, blockAdd);
            }
            var block = array[handler.Block];
            if (block == null)
            {
                array[handler.Block] = block = new T[blockSize];
            }
            block[handler.BlockIndex] = value;
            maxIndex = Math.Max(maxIndex, handler.GetSeqence(BlockSize));
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i <= maxIndex; i++)
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
                array.RemoveAt(blockCount - 1);
                Interlocked.Decrement(ref blockCount);
            }
            if (block < array.Count && blockIndex + 1 < BlockSize)
            {
                Memset<T>(array[block], default(T), blockIndex + 1);
            }
        }
    }
}
