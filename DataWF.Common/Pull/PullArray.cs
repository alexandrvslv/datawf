﻿using System;
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
        { }

        public override int Capacity => array.Length * blockSize;

        public int Count => maxIndex;

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

        public override object Get(short block, short blockIndex)
        {
            return GetValue(new PullHandler(block, blockIndex));
        }

        public override void Set(int index, object value)
        {
            SetValue(PullHandler.FromSeqence(index, blockSize), (T)value);
        }

        public override void Set(short block, short blockIndex, object value)
        {
            SetValue(new PullHandler(block, blockIndex), (T)value);
        }

        public override T GetValue(in PullHandler handler)
        {
            if (handler.Block >= blockCount)
            {
                return default(T);
            }
            var arrayBlock = array[handler.Block];
            return arrayBlock != null ? arrayBlock[handler.BlockIndex] : default(T);
        }

        public override void SetValue(in PullHandler handler, T value)
        {
            if (handler.Block >= blockCount)
            {
                var blockAdd = (handler.Block + 1) - blockCount;
                Interlocked.Add(ref blockCount, blockAdd);
                if (blockCount >= array.Length)
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
            maxIndex = Math.Max(maxIndex, handler.GetSeqence(blockSize));
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
