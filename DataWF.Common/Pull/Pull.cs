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

        public static int GetHIndex(int index, int blockSize, out short block, out short blockIndex)
        {
            block = (short)(index / blockSize);
            blockIndex = (short)(index % blockSize);
            return Helper.TwoToOneShift(block, blockIndex);
        }

        protected int blockCount;
        protected int blockSize;
        private Type itemType;

        internal Pull(int blockSize)
        {
            BlockSize = blockSize;
        }

        public abstract object Get(int index);

        public abstract object Get(short block, short blockIndex);

        public abstract void Set(int index, object value);

        public abstract void Set(short block, short blockIndex, object value);

        public T GetValue<T>(int index)
        {
            Helper.OneToTwoShift(index, out short block, out short blockIndex);
            return GetValue<T>(block, blockIndex);
        }

        public T GetValue<T>(short block, short blockIndex)
        {
            return ((GenericPull<T>)this).GetValue(block, blockIndex);
        }

        public void SetValue<T>(int index, T value)
        {
            Helper.OneToTwoShift(index, out short block, out short blockIndex);
            SetValue(block, blockIndex, value);
        }

        public void SetValue<T>(short block, short blockIndex, T value)
        {
            ((GenericPull<T>)this).SetValue(block, blockIndex, value);
        }

        public virtual int Capacity { get { return 0; } }
        public int BlockSize
        {
            get { return blockSize; }
            set
            {
                if (blockCount == 0)
                {
                    blockSize = value;
                }
                else
                {
                    throw new Exception("Unable set block size after data modified");
                }
            }
        }

        public Type ItemType
        {
            get { return itemType; }
            set { itemType = value; }
        }

        public abstract bool EqualNull(object value);

        public virtual void Clear()
        {
        }

        public abstract void Trunc(int maxIndex);
    }

    

}
