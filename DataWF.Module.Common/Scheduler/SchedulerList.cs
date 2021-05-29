using System.ComponentModel;
using DataWF.Data;
using DataWF.Common;

namespace DataWF.Module.Common
{
    public class SchedulerList : DBTableView<Scheduler>
    {
        public SchedulerList(SchedulerTable table, string filter = "", DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
            : base(table, filter, mode, status)
        {
            ApplySortInternal(new DBComparer<Scheduler, int?>(table.OrderKey, ListSortDirection.Ascending));
        }
    }
}
