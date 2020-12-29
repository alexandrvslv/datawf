using DataWF.Common;
using DataWF.Data;
using System.ComponentModel;

namespace DataWF.Module.Flow
{
    public class WorkList : DBTableView<Work>
    {
        public WorkList(WorkTable table, string filter = "", DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
            : base(table, filter, mode, status)
        {
            ApplySortInternal(new DBComparer<Work, string>(table.CodeKey, ListSortDirection.Ascending));
        }

    }
}
