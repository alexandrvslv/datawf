using System.ComponentModel;
using System.Linq;
using DataWF.Data;
using DataWF.Common;

namespace DataWF.Module.Common
{
    public class GroupPermissionList : DBTableView<GroupPermission>
    {
        public GroupPermissionList(DBTable<GroupPermission> table, string filter, DBViewKeys mode)
            : base(table, filter, mode)
        {
            ApplySort(new InvokerComparer(typeof(GroupPermission), "ToString", ListSortDirection.Ascending));
        }

        public GroupPermissionList(string filter = "", DBViewKeys mode = DBViewKeys.None)
            : this(GroupPermission.DBTable, filter, mode)
        {
        }


    }
}
