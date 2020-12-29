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
            ApplySort(new InvokerComparer<GroupPermission, string>("ToString", ListSortDirection.Ascending));
        }
        
    }
}
