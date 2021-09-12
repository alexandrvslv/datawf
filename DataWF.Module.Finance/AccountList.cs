using DataWF.Data;

namespace DataWF.Module.Finance
{
    public class AccountList : DBTableView<Account>
    {
        public AccountList(DBTable<Account> table, string filter = "", DBViewKeys mode = DBViewKeys.Empty)
            : base(table, filter, mode)
        {
        }
    }
}
