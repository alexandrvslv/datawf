using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Counterpart;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace DataWF.Module.Common
{

    [Table("rscheduler", "Reference Book", BlockSize = 20)]
    public sealed partial class Scheduler : DBItem//, IComparable
    {
        private Company company;

        public Scheduler(DBTable table) : base(table)
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Column("company_id"), Browsable(false)]
        public int? CompanyId
        {
            get => GetValue<int?>(Table.CompanyIdKey);
            set => SetValue(value, Table.CompanyIdKey);
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get => GetReference(Table.CompanyIdKey, ref company);
            set => SetReference(company = value, Table.CompanyIdKey);
        }

        [Column("code", Keys = DBColumnKeys.Code)]
        public string Code
        {
            get => GetValue<string>(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
        }

        [Column("name", Keys = DBColumnKeys.Culture | DBColumnKeys.View)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }

        [CultureKey(nameof(Name))]
        public string NameEN
        {
            get => GetValue<string>(Table.NameENKey);
            set => SetValue(value, Table.NameENKey);
        }

        [CultureKey(nameof(Name))]
        public string NameRU
        {
            get => GetValue<string>(Table.NameRUKey);
            set => SetValue(value, Table.NameRUKey);
        }

        [Column("orderid")]
        public int? Order
        {
            get => GetValue<int?>(Table.OrderKey);
            set => SetValue(value, Table.OrderKey);
        }

        [DefaultValue(SchedulerType.Interval), Column("type_id", Keys = DBColumnKeys.ElementType | DBColumnKeys.Notnull)]
        public SchedulerType? Type
        {
            get => GetValue<SchedulerType?>(Table.ElementTypeKey);
            set => SetValue(value, Table.ElementTypeKey);
        }

        [Column("run_interval", Keys = DBColumnKeys.Notnull)]
        public TimeSpan? Interval
        {
            get => GetValue<TimeSpan?>(Table.IntervalKey);
            set => SetValue(value, Table.IntervalKey);
        }

        [Browsable(false)]
        [Column("procedure_name")]
        public string ProcedureName
        {
            get => GetValue<string>(Table.ProcedureNameKey);
            set => SetValue(value, Table.ProcedureNameKey);
        }

        public DBProcedure Procedure
        {
            get => DBService.Schems.ParseProcedure(ProcedureName);
            set => ProcedureName = value?.Name;
        }

        [Column("date_execute")]
        public DateTime? DateExecute
        {
            get => GetValue<DateTime?>(Table.DateExecuteKey);
            set => SetValue(value, Table.DateExecuteKey);
        }

        public async Task<StateInfo> Execute()
        {
            using (var transaction = new DBTransaction(Table))
            {
                try
                {
                    var result = await Execute(transaction);
                    transaction.Commit();
                    return result;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Helper.OnException(ex);
                    throw ex;
                }
            }
        }

        [ControllerMethod]
        public async Task<StateInfo> Execute(DBTransaction transaction)
        {
            if (Procedure == null)
            {
                throw new Exception("Procedure not specified!");
            }
            var task = Procedure.GetExecutor(null, transaction);
            var result = task.Execute();

            var info = new StateInfo
            {
                Module = "Task",
                Message = Name ?? task.Name,
                Tag = result
            };

            DateExecute = DateTime.Now;
            await Save();

            if (result is Exception exception)
            {
                Helper.OnException(exception);
                info.Message += " Fail!";
                info.Description = exception.Message;
                info.Stack = exception.StackTrace;
                info.Type = StatusType.Warning;
            }
            else
            {
                var statisticTable = (StatisticTable)transaction.Schema.GetTable<Statistic>();
                if (result is decimal rez && statisticTable != null)
                {
                    var stat = new Statistic(statisticTable)
                    {
                        Scheduler = this,
                        Result = (decimal)rez
                    };
                    await stat.Save();
                }

                info.Description = string.Format("Completed in {0:n} {1}", task.Time.TotalMilliseconds / 1000, result);
                info.Type = StatusType.Information;
            }

            Helper.Logs.Add(info);

            if (result is Exception)
            {
                throw (Exception)result;
            }

            return info;
        }
    }
}
