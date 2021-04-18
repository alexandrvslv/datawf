using DataWF.Data;
using DataWF.Common;
using DataWF.Module.Common;

namespace DataWF.Module.Finance
{
    [ItemType(500)]
    public sealed partial class AccountType : Book
    {
        public AccountType(DBTable table) : base(table)
        {
            ItemType = 500;
        }

    }
}
