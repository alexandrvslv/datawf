using DataWF.Common;
using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Common
{
    [Table("dstats", "Reference Book"), InvokerGenerator]
    public sealed partial class Statistic : DBItem//, IComparable
    {
        private Scheduler scheduler;

        public Statistic(DBTable table) : base(table)
        {
        }

        public StatisticTable StatisticTable => (StatisticTable)Table;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(StatisticTable.IdKey);
            set => SetValue(value, StatisticTable.IdKey);
        }

        //[Column("code", Keys = DBColumnKeys.Code)]
        //public string Code
        //{
        //    get { return GetValue<string>(Table.CodeKey); }
        //    set { SetValue(value, Table.CodeKey); }
        //}

        //[Column("name", Keys = DBColumnKeys.Culture)]
        //public override string Name
        //{
        //    get { return GetName("name"); }
        //    set { SetName("name", value); }
        //}

        [Browsable(false)]
        [Column("scheduler_id")]
        public int? SchedulerId
        {
            get => GetValue<int?>(StatisticTable.SchedulerIdKey);
            set => SetValue(value, StatisticTable.SchedulerIdKey);
        }

        [Reference(nameof(SchedulerId))]
        public Scheduler Scheduler
        {
            get => GetReference(StatisticTable.SchedulerIdKey, ref scheduler);
            set => SetReference(scheduler = value, StatisticTable.SchedulerIdKey);
        }

        [Column("stat_result")]
        public decimal? Result
        {
            get => GetValue<decimal?>(StatisticTable.ResultKey);
            set => SetValue(value, StatisticTable.ResultKey);
        }
    }
}
