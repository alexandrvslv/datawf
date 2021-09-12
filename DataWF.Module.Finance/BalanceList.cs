using DataWF.Data;

namespace DataWF.Module.Finance
{
    public class BalanceList : DBTableView<Balance>
    {
        public BalanceList(BalanceTable<Balance> table) : base(table)
        {
        }
    }
}

