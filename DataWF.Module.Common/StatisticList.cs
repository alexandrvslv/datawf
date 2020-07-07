using System;
using System.ComponentModel;
using DataWF.Data;

namespace DataWF.Module.Common
{
    public class StatisticList : DBTableView<Statistic>
    {
        public StatisticList(string filter, DBViewKeys mode = DBViewKeys.None)
            : base(Statistic.DBTable, filter, mode)
        {
            ApplySortInternal(new DBComparer<Statistic, DateTime?>(Statistic.DBTable.DateKey, ListSortDirection.Ascending));
        }

        public StatisticList()
            : this(string.Empty)
        {
        }
    }
}
