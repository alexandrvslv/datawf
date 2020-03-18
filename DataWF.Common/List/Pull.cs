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
            Helper.OneToTwoShift(index, out var block, out var blockIndex);
            return GetValue<T>(block, blockIndex);
        }

        public T GetValue<T>(short block, short blockIndex)
        {
            return ((GenericPull<T>)this).GetValue(block, blockIndex);
        }

        public void SetValue<T>(int index, T value)
        {
            Helper.OneToTwoShift(index, out var block, out var blockIndex);
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

    public class DBNullablePull<T> : Pull<DBNullable<T>>, IEnumerable<DBNullable<T>> where T : struct
    {
        public DBNullablePull(int BlockSize) : base(BlockSize)
        {
            ItemType = typeof(T);
        }

        public override void Set(int index, object value)
        {
            Helper.OneToTwoShift(index, out var block, out var blockIndex);
            SetValue(block, blockIndex, DBNullable<T>.CheckNull(value));
        }

        public override void Set(short block, short blockIndex, object value)
        {
            SetValue(block, blockIndex, DBNullable<T>.CheckNull(value));
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
            Helper.OneToTwoShift(index, out var block, out var blockIndex);
            SetValue(block, blockIndex, value == null ? null : value is T? ? (T?)value : (T?)(T)value);
        }

        public override void Set(short block, short blockIndex, object value)
        {
            SetValue(block, blockIndex, value == null ? null : value is T? ? (T?)value : (T?)(T)value);
        }
    }

    public class NullablePullArray<T> : PullArray<T?>, IEnumerable<T?> where T : struct
    {
        public NullablePullArray(int BlockSize) : base(BlockSize)
        {
            ItemType = typeof(T);
        }

        public override void Set(int index, object value)
        {
            Helper.OneToTwoShift(index, out var block, out var blockIndex);
            SetValue(block, blockIndex, value == null ? null : value is T? ? (T?)value : (T?)(T)value);
        }

        public override void Set(short block, short blockIndex, object value)
        {
            SetValue(block, blockIndex, value == null ? null : value is T? ? (T?)value : (T?)(T)value);
        }
    }

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
            if (block >= blockCount || array[block])
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

    public class PullArray<T> : GenericPull<T>, IEnumerable<T>
    {
        private T[][] array = new T[32][];
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
                Interlocked.Add(ref blockCount, blockAdd);
                if (blockCount >= array.Length)
                {
                    Reallocate(blockCount);
                }
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

        private void Reallocate(int minCount)
        {
            var size = array.Length;
            while (size < minCount)
            {
                size += 32;
            }
            var temp = new T[size][];
            array.CopyTo(temp, 0);
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
            Helper.OneToTwoShift(maxIndex, out var block, out var blockIndex);
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
