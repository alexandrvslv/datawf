using System;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DataWF.Common
{

    public abstract class GenericPull<T> : Pull
    {
        internal GenericPull(int blockSize) : base(blockSize)
        {
            ItemType = typeof(T);
        }

        public T GetValue(int index) => GetValue(PullHandler.FromSeqence(index, blockSize));

        public void SetValue(int index, T value) => SetValue(PullHandler.FromSeqence(index, blockSize), value);

        public abstract T GetValue(in PullHandler index);

        public abstract void SetValue(in PullHandler index, T value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetValue(short block, short blockIndex) => GetValue(new PullHandler(block, blockIndex));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(short block, short blockIndex, T value) => SetValue(new PullHandler(block, blockIndex), value);
    }
}
