using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DataWF.Common
{
    public abstract class Pull
    {
        private static readonly Type[] ctorTypes = new Type[] { typeof(int) };

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
            return Helper.TwoToOnePointer(block, blockIndex);
        }

        protected int blockSize;
        private Type itemType;

        internal Pull(int BlockSize)
        {
            blockSize = BlockSize;
        }

        public abstract object Get(int index);

        public abstract object Get(short block, short blockIndex);

        public abstract void Set(int index, object value);

        public abstract void Set(short block, short blockIndex, object value);

        public T GetValue<T>(int index)
        {
            Helper.OneToTwoPointer(index, out var block, out var blockIndex);
            return GetValue<T>(block, blockIndex);
        }

        public T GetValue<T>(short block, short blockIndex)
        {
            return ((Pull<T>)this).GetValue(block, blockIndex);
        }

        public void SetValue<T>(int index, T value)
        {
            Helper.OneToTwoPointer(index, out var block, out var blockIndex);
            SetValue(block, blockIndex, value);
        }

        public void SetValue<T>(short block, short blockIndex, T value)
        {
            ((Pull<T>)this).SetValue(block, blockIndex, value);
        }

        public virtual int Capacity { get { return 0; } }
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
            Helper.OneToTwoPointer(index, out var block, out var blockIndex);
            Set(block, blockIndex, value);
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
            Helper.OneToTwoPointer(index, out var block, out var blockIndex);
            Set(block, blockIndex, value);
        }

        public override void Set(short block, short blockIndex, object value)
        {
            base.Set(block, blockIndex, value == null ? null : value is T? ? (T?)value : (T?)(T)value);
        }
    }

    public class Pull<T> : Pull, IEnumerable<T>
    {
        private List<T[]> array = new List<T[]>();
        private short maxIndex;

        public Pull(int blockSize) : base(blockSize)
        {
            ItemType = typeof(T);
        }

        public override int Capacity { get { return array.Count * blockSize; } }

        public override void Clear()
        {
            foreach (var a in array)
            {
                if (a != null)
                    Array.Clear(a, 0, a.Length);
            }

            array.Clear();
        }

        public override bool EqualNull(object value)
        {
            return value == null;
        }

        public override object Get(int index)
        {
            Helper.OneToTwoPointer(index, out var block, out var blockIndex);
            return GetValue(block, blockIndex);
        }

        public override object Get(short block, short blockIndex)
        {
            return GetValue(block, blockIndex);
        }

        public override void Set(int index, object value)
        {
            Helper.OneToTwoPointer(index, out var block, out var blockIndex);
            SetValue(block, blockIndex, (T)value);
        }

        public override void Set(short block, short blockIndex, object value)
        {
            SetValue(block, blockIndex, (T)value);
        }

        public T GetValue(short block, short blockIndex)
        {
            if (block >= array.Count || array[block] == null)
                return default(T);
            return array[block][blockIndex];
        }

        public void SetValue(short block, short blockIndex, T value)
        {
            while (block > array.Count)
                array.Add(null);
            if (block == array.Count)
            {
                array.Add(new T[blockSize]);
                maxIndex = 0;
            }
            if (array[block] == null)
            {
                array[block] = new T[blockSize];
            }
            array[block][blockIndex] = value;
            if (block == array.Count - 1)
            {
                maxIndex = Math.Max(maxIndex, blockIndex);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < array.Count; i++)
            {
                var block = array[i];
                var size = i == array.Count - 1 ? maxIndex : blockSize;
                for (int j = 0; j < size; j++)
                {
                    if (block == null)
                        yield return default(T);
                    yield return block[j];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override void Trunc(int maxIndex)
        {
            Helper.OneToTwoPointer(maxIndex, out var block, out var blockIndex);
            while (block < array.Count - 1)
            {
                array.RemoveAt(array.Count - 1);
            }
            if (block < array.Count && blockIndex + 1 < BlockSize)
            {
                Memset<T>(array[block], default(T), blockIndex + 1);
            }
        }
    }
}
