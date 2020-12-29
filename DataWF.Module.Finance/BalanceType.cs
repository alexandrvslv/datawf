using DataWF.Data;
using DataWF.Common;
using DataWF.Module.Common;

namespace DataWF.Module.Finance
{
    [ItemType(501), InvokerGenerator]
    public sealed partial class BalanceType : Book
    {
        public BalanceType(DBTable table) : base(table)
        {
            ItemType = 501;
        }

    }

    [ItemType(502), InvokerGenerator]
    public sealed partial class PaymentType : Book
    {
        public PaymentType(DBTable table) : base(table)
        {
            ItemType = 502;
        }

    }
}
