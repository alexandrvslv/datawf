using System.Linq;
using DataWF.Data;

namespace DataWF.Module.Common
{
    public class UserList : DBTableView<User>
    {
        public UserList(DBTable<User> table, string filter, DBViewKeys mode = DBViewKeys.None)
            : base(table, filter, mode)
        { }

        public UserList(string filter = "", DBViewKeys mode = DBViewKeys.None)
            : this(User.DBTable, filter, mode)
        { }

        public UserList(User department)
            : this(User.DBTable.GroupKey.Name + "=" + department.PrimaryId)
        { }


    }
}
