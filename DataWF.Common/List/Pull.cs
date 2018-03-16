using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DataWF.Common
{
    public abstract class Pull
    {
        public static Pull Fabric(Type type, int blockSize)
        {
            Type gtype = null;
            if (type.IsValueType || type.IsEnum)
            {
                gtype = typeof(NullablePull<>).MakeGenericType(type);
            }
            else
            {
                gtype = typeof(Pull<>).MakeGenericType(type);
            }
            return (Pull)EmitInvoker.CreateObject(gtype, new Type[] { typeof(int) }, new object[] { blockSize }, true);
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
                for (count = index + 1; count <= length / 2; count *= 2)
                    Buffer.BlockCopy(array, 0, array, count, count * size);
                Buffer.BlockCopy(array, 0, array, count, (length - count) * size);
            }
            else if (!typeof(T).IsClass)
            {
                for (count = index + 1; count <= length / 2; count *= 2)
                    Array.Copy(array, 0, array, count, count);
                Array.Copy(array, 0, array, count, length - count);
            }
            else
            { }
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

        public static int GetHIndex(int index, int blockSize)
        {
            short a = (short)(index / blockSize);
            short b = (short)(index % blockSize);
            return (a << 16) | (b & 0xFFFF);
        }

        public static void GetBlockIndex(int index, out short block, out short blockIndex)
        {
            block = (short)(index >> 16);
            blockIndex = (short)(index & 0xFFFF);
        }

        protected int blockSize;
        private Type itemType;


        internal Pull(int BlockSize)
        {
            blockSize = BlockSize;
        }

        public abstract object Get(int index);

        public abstract void Set(int index, object value);

        public T GetValue<T>(int index)
        {
            return ((Pull<T>)this).GetValueInternal(index);
        }

        public void SetValue<T>(int index, T value)
        {
            ((Pull<T>)this).SetValueInternal(index, value);
        }

        public int Count { get; internal set; }
        public int Capacity { get { return BlockCount * BlockSize; } }
        public int BlockCount { get; internal set; }
        public int BlockSize
        {
            get { return blockSize; }
            set
            {
                if (blockSize != 0)
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
            BlockCount = 0;
            Count = 0;
        }
    }

    public class NullablePull<T> : Pull<T?>, IEnumerable<T?> where T : struct
    {
        public NullablePull(int BlockSize) : base(BlockSize)
        {
            ItemType = typeof(T);
        }

        public override void Set(int index, object value)
        {
            SetValue(index, value == null ? null : value is T? ? (T?)value : (T?)(T)value);
        }
    }

    public class Pull<T> : Pull, IEnumerable<T>
    {
        private List<T[]> array = new List<T[]>();

        public Pull(int blockSize) : base(blockSize)
        {
            ItemType = typeof(T);
        }

        public override void Clear()
        {
            foreach (var a in array)
                if (a != null)
                    Array.Clear(a, 0, a.Length);
            array.Clear();
            base.Clear();
        }

        public override bool EqualNull(object value)
        {
            return value == null;
        }

        public override object Get(int index)
        {
            return GetValueInternal(index);
        }

        public override void Set(int index, object value)
        {
            SetValueInternal(index, (T)value);
        }

        public T GetValueInternal(int index)
        {
            GetBlockIndex(index, out short block, out short blockIndex);
            if (block >= array.Count || array[block] == null)
                return default(T);
            return array[block][blockIndex];
        }

        public void SetValueInternal(int index, T value)
        {
            GetBlockIndex(index, out short block, out short blockIndex);
            while (block > array.Count)
                array.Add(null);
            if (block == array.Count)
            {
                array.Add(new T[blockSize]);
                BlockCount++;
            }
            if (array[block] == null)
            {
                array[block] = new T[blockSize];
                BlockCount++;
            }
            array[block][blockIndex] = value;
            Count = Math.Max(Count, index);
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                var item = GetValueInternal(i);
                if (item != null)
                {
                    yield return item;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }
}
