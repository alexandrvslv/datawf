using System.ComponentModel;
using DataWF.Data;
using DataWF.Common;

namespace DataWF.Module.Common
{
    public class SchedulerList : DBTableView<Scheduler>
    {
        public SchedulerList(string filter, DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
            : base(Scheduler.DBTable, filter, mode, status)
        {
            ApplySortInternal(new DBComparer<Scheduler, int?>(Scheduler.OrderKey, ListSortDirection.Ascending));
        }

        public SchedulerList()
            : this(string.Empty)
        {
        }
    }
}
