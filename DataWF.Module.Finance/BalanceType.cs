using DataWF.Data;
using DataWF.Common;
using DataWF.Module.Common;

namespace DataWF.Module.Finance
{
    [VirtualTable(501)]
    public sealed partial class BalanceType : Book
    {
        public BalanceType(DBTable table) : base(table)
        {
            ItemType = 501;
        }

    }
}
