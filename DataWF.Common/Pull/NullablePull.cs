using System.Collections.Generic;

namespace DataWF.Common
{
    public class NullablePull<T> : Pull<T?> where T : struct
    {
        public NullablePull(int BlockSize) : base(BlockSize)
        { }

        public override void Set(int index, object value)
        {
            (short block, short blockIndex) = Helper.OneToTwoShift(index);
            SetValue(block, blockIndex, value == null ? null : value is T? ? (T?)value : (T?)(T)value);
        }

        public override void Set(short block, short blockIndex, object value)
        {
            SetValue(block, blockIndex, value == null ? null : value is T? ? (T?)value : (T?)(T)value);
        }
    }

}
