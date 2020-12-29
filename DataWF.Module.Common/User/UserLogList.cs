using DataWF.Common;
using DataWF.Data;

namespace DataWF.Module.Common
{
    public class UserLogList : DBTableView<UserReg>
    {
        public UserLogList(UserRegTable table, string filter)
            : base(table, filter)
        {
        }

    }
}
