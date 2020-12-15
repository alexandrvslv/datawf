using System.Collections.Generic;

namespace DataWF.Common
{
    public class DBNullablePull<T> : Pull<DBNullable<T>>, IEnumerable<DBNullable<T>> where T : struct
    {
        public DBNullablePull(int BlockSize) : base(BlockSize)
        {
            ItemType = typeof(T);
        }

        public override void Set(int index, object value)
        {
            (short block, short blockIndex) = Helper.OneToTwoShift(index);
            SetValue(block, blockIndex, DBNullable<T>.CheckNull(value));
        }

        public override void Set(short block, short blockIndex, object value)
        {
            SetValue(block, blockIndex, DBNullable<T>.CheckNull(value));
        }
    }

}
