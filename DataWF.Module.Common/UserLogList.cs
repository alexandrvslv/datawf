using DataWF.Common;
using DataWF.Data;

namespace DataWF.Module.Common
{
    public class UserLogList : DBTableView<UserReg>
    {
        public UserLogList(string filter)
            : base(UserReg.DBTable, filter)
        {
        }

        public UserLogList()
            : this("")
        {
        }
    }
}
