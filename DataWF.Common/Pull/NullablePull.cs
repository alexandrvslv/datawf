using System.Collections.Generic;

namespace DataWF.Common
{
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

}
