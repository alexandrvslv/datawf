using System.Collections.Generic;

namespace DataWF.Common
{
    public class DBNullablePull<T> : Pull<DBNullable<T>>, IEnumerable<DBNullable<T>> where T : struct
    {
        public DBNullablePull(int BlockSize) : base(BlockSize)
        {
        }

        public override void Set(int index, object value)
        {
            SetValue(PullHandler.FromSeqence(index, blockSize), DBNullable<T>.CheckNull(value));
        }

        public override void Set(short block, short blockIndex, object value)
        {
            SetValue(new PullHandler(block, blockIndex), DBNullable<T>.CheckNull(value));
        }
    }

}
