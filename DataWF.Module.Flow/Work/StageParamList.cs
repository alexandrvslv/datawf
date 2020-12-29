using DataWF.Common;
using DataWF.Data;
using System.ComponentModel;

namespace DataWF.Module.Flow
{
    public class StageParamList : DBTableView<StageParam>
    {
        public StageParamList(StageParamTable<StageParam> table, string filter, DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
            : base(table, filter, mode, status)
        {
            ApplySortInternal(new DBComparer<StageParam, int?>(table.IdKey, ListSortDirection.Ascending));
        }

        public StageParamList(StageParamTable<StageParam> table, Stage stage)
            : this(table, $"{table.StageIdKey.Name}={stage.PrimaryId}")
        {
        }
    }
}
