using DataWF.Common;
using DataWF.Data;
using System.ComponentModel;

namespace DataWF.Module.Flow
{
    public class StageList : DBTableView<Stage>
    {
        public StageList(StageTable<Stage> table, string filter = "", DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
            : base(table, filter, mode, status)
        {
            ApplySortInternal(new DBComparer<Stage, string>(table.CodeKey, ListSortDirection.Ascending));
        }

        public StageList(StageTable<Stage> table, Work flow)
            : this(table, $"{table.WorkIdKey.Name}={flow.PrimaryId}")
        {
        }
    }
}
