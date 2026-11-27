using DataWF.Invokers;
using System;
using System.Runtime.InteropServices;

namespace DataWF.Collections.Pulls;

    public abstract class Pull
    {
        private static readonly Type[] ctorTypes = new Type[] { typeof(int) };

        public static Pull Fabric<T>(int blockSize)
        {
            return new PullArray<T>(blockSize);
        }

        public static void Memset<T>(T[] array, T elem, int index)
        {
            int length = array.Length - index;
            if (length <= 0) return;

            array[index] = elem;
            int count;
            if (typeof(T).IsPrimitive)
            {
                var size = Marshal.SizeOf(typeof(T));
                for (count = 1; count <= length / 2; count *= 2)
                    Buffer.BlockCopy(array, index, array, index + count, count * size);
                Buffer.BlockCopy(array, index, array, index + count, (length - count) * size);
            }
            else //if (!typeof(T).IsClass)
            {
                for (count = 1; count <= length / 2; count *= 2)
                    Array.Copy(array, index, array, index + count, count);
                Array.Copy(array, index, array, index + count, length - count);
            }
        }

        public static void ReAlloc<T>(ref T[] array, int len, T NullValue)
        {
            var temp = array;
            var narray = new T[len];
            len = len > array.Length ? array.Length : len;
            if (typeof(T).IsPrimitive)
                Buffer.BlockCopy(temp, 0, narray, 0, len * Marshal.SizeOf(typeof(T)));
            else
                Array.Copy(temp, narray, len);

            Memset<T>(narray, NullValue, array.Length);
            array = narray;
        }

        protected int blockCount;
        protected int blockSize;

        internal Pull(int blockSize)
        {
            BlockSize = blockSize;
        }

        public abstract int Capacity { get; }

        public int BlockSize
        {
            get => blockSize;
            set
            {
                if (blockSize != value)
                {
                    if (blockCount == 0)
                    {
                        blockSize = value;
                    }
                    else
                    {
                        throw new Exception("Unable set block size after allocation!");
                    }
                }
            }
        }

        public abstract Type DataType{ get; }

        public PullHandler GetSequenceHandler(int index) => PullHandler.FromSeqence(index, blockSize);

        public abstract object? Get(int index);

        public abstract object? Get(int block, int blockIndex);

        public abstract void Set(int index, object? value);

        public abstract void Set(int block, int blockIndex, object? value);

        public abstract bool EqualNull(object value);

        public virtual void Clear()
        { }

        public abstract void Trunc(int maxIndex);
    }
