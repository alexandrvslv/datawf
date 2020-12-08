using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace DataWF.Common
{
    public class PullArray<T> : GenericPull<T>, IEnumerable<T>
    {
        protected T[][] array = new T[16][];
        private int maxIndex;

        public PullArray(int blockSize) : base(blockSize)
        {
            ItemType = typeof(T);
        }

        public override int Capacity => array.Length * blockSize;

        public int Count => ((blockCount - 1) * blockSize) + maxIndex;

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
            Helper.OneToTwoShift(index, out short block, out short blockIndex);
            return GetValue(block, blockIndex);
        }

        public override object Get(short block, short blockIndex)
        {
            return GetValue(block, blockIndex);
        }

        public override void Set(int index, object value)
        {
            Helper.OneToTwoShift(index, out short block, out short blockIndex);
            SetValue(block, blockIndex, (T)value);
        }

        public override void Set(short block, short blockIndex, object value)
        {
            SetValue(block, blockIndex, (T)value);
        }

        public override T GetValue(short block, short blockIndex)
        {
            if (block >= blockCount)
            {
                return default(T);
            }
            var arrayBlock = array[block];
            return arrayBlock == null ? default(T) : arrayBlock[blockIndex];
        }

        public override void SetValue(short block, short blockIndex, T value)
        {
            if (block >= blockCount)
            {
                var blockAdd = (block + 1) - blockCount;
                Interlocked.Add(ref blockCount, blockAdd);
                if (blockCount >= array.Length)
                {
                    Reallocate(blockCount);
                }
            }
            var arrayBlock = array[block];
            if (arrayBlock == null)
            {
                array[block] = arrayBlock = new T[blockSize];
                maxIndex = block == (blockCount - 1) ? 0 : maxIndex;
            }
            arrayBlock[blockIndex] = value;
            if (block == blockCount - 1)
            {
                maxIndex = Math.Max(maxIndex, blockIndex);
            }
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
            for (int i = 0; i < blockCount; i++)
            {
                var block = array[i];
                var size = i == blockCount - 1 ? maxIndex : blockSize;
                for (int j = 0; j < size; j++)
                {
                    yield return block == null ? default(T) : block[j];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override void Trunc(int maxIndex)
        {
            Helper.OneToTwoShift(maxIndex, out short block, out short blockIndex);
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
