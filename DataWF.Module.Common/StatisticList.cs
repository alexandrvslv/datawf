using System;
using System.ComponentModel;
using DataWF.Data;

namespace DataWF.Module.Common
{
    public class StatisticList : DBTableView<Statistic>
    {
        public StatisticList(StatisticTable table, string filter, DBViewKeys mode = DBViewKeys.None)
            : base(table, filter, mode)
        {
            ApplySortInternal(new DBComparer<Statistic, DateTime?>(table.DateCreateKey, ListSortDirection.Ascending));
        }
    }
}
