using DataWF.Data;

namespace DataWF.Module.Finance
{
    public class PaymentList : DBTableView<Payment>
    {
        public PaymentList(PaymentTable<Payment> table) : base(table)
        {
        }
    }
}

