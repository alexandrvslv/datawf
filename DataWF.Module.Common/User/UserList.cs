using System.Linq;
using DataWF.Data;

namespace DataWF.Module.Common
{
    public class UserList : DBTableView<User>
    {
        public UserList(DBTable<User> table, string filter, DBViewKeys mode = DBViewKeys.None)
            : base(table, filter, mode)
        { }

        public UserList(UserTable table, Department department)
            : this(table, table.DepartmentIdKey.Name + "=" + department.PrimaryId)
        { }


    }
}
