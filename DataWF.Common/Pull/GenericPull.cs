using System;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public abstract class GenericPull<T> : Pull
    {
        internal GenericPull(int blockSize) : base(blockSize)
        { }

        public abstract T GetValue(short block, short blockIndex);
        public abstract void SetValue(short block, short blockIndex, T value);
    }


    public class Pull<T> : GenericPull<T>, IEnumerable<T>
    {
        private readonly List<T[]> array = new List<T[]>();
        private int maxIndex;

        public Pull(int blockSize) : base(blockSize)
        {
            ItemType = typeof(T);
        }

        public override int Capacity => array.Count * blockSize;

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
            Helper.OneToTwoShift(index, out var block, out var blockIndex);
            return GetValue(block, blockIndex);
        }

        public override object Get(short block, short blockIndex)
        {
            return GetValue(block, blockIndex);
        }

        public override void Set(int index, object value)
        {
            Helper.OneToTwoShift(index, out var block, out var blockIndex);
            SetValue(block, blockIndex, (T)value);
        }

        public override void Set(short block, short blockIndex, object value)
        {
            SetValue(block, blockIndex, (T)value);
        }

        public override T GetValue(short block, short blockIndex)
        {
            if (block >= blockCount || array[block] == null)
            {
                return default(T);
            }
            return array[block][blockIndex];
        }

        public override void SetValue(short block, short blockIndex, T value)
        {
            if (block >= blockCount)
            {
                var blockAdd = (block + 1) - blockCount;
                array.AddRange(Enumerable.Repeat((T[])null, blockAdd));
                Interlocked.Add(ref blockCount, blockAdd);
            }
            if (array[block] == null)
            {
                array[block] = new T[blockSize];
                maxIndex = block == (blockCount - 1) ? 0 : maxIndex;
            }
            array[block][blockIndex] = value;
            if (block == blockCount - 1)
            {
                maxIndex = Math.Max(maxIndex, blockIndex);
            }
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
            Helper.OneToTwoShift(maxIndex, out var block, out var blockIndex);
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
