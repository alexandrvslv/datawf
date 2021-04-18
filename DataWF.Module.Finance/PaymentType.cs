using DataWF.Data;
using DataWF.Module.Common;

namespace DataWF.Module.Finance
{
    [ItemType(502)]
    public sealed partial class PaymentType : Book
    {
        public PaymentType(DBTable table) : base(table)
        {
            ItemType = 502;
        }

    }
}
