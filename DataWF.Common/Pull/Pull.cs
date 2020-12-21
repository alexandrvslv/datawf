using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace DataWF.Common
{
    public abstract class Pull
    {
        private static readonly Type[] ctorTypes = new Type[] { typeof(int) };

        public static Pull Fabric(Type type, int blockSize)
        {
            Type gtype = type.IsValueType || type.IsEnum
                ? typeof(NullablePullArray<>).MakeGenericType(type)
                : typeof(PullArray<>).MakeGenericType(type);
            return (Pull)EmitInvoker.CreateObject(gtype, ctorTypes, new object[] { blockSize }, true);
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
        private Type itemType;

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

        public Type ItemType
        {
            get => itemType;
            set => itemType = value;
        }

        public PullHandler GetSequenceHandler(int index) => PullHandler.FromSeqence(index, blockSize);

        public abstract object Get(int index);

        public abstract object Get(int block, int blockIndex);

        public abstract void Set(int index, object value);

        public abstract void Set(int block, int blockIndex, object value);

        public T GetValue<T>(in PullHandler handler) => ((GenericPull<T>)this).GetValue(in handler);

        public void SetValue<T>(in PullHandler handler, T value) => ((GenericPull<T>)this).SetValue(handler, value);

        public T GetValue<T>(int index) => ((GenericPull<T>)this).GetValue(index);

        public void SetValue<T>(int index, T value) => ((GenericPull<T>)this).SetValue(index, value);

        public T GetValue<T>(int block, int blockIndex) => ((GenericPull<T>)this).GetValue(new PullHandler(block, blockIndex));

        public void SetValue<T>(int block, int blockIndex, T value) => ((GenericPull<T>)this).SetValue(new PullHandler(block, blockIndex), value);

        public abstract bool EqualNull(object value);

        public virtual void Clear()
        { }

        public abstract void Trunc(int maxIndex);
    }



}
