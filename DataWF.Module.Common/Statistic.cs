using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Counterpart;
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

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
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
            get => GetValue<int?>(Table.SchedulerIdKey);
            set => SetValue(value, Table.SchedulerIdKey);
        }

        [Reference(nameof(SchedulerId))]
        public Scheduler Scheduler
        {
            get => GetReference(Table.SchedulerIdKey, ref scheduler);
            set => SetReference(scheduler = value, Table.SchedulerIdKey);
        }

        [Column("stat_result")]
        public decimal? Result
        {
            get => GetValue<decimal?>(Table.ResultKey);
            set => SetValue(value, Table.ResultKey);
        }
    }
}
