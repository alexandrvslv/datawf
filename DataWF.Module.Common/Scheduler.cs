/*
 Scheduler.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Counterpart;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace DataWF.Module.Common
{

    [DataContract, Table("rscheduler", "Reference Book", BlockSize = 20)]
    public class Scheduler : DBItem//, IComparable
    {
        public static readonly DBTable<Scheduler> DBTable = GetTable<Scheduler>();
        public static readonly DBColumn OrderKey = DBTable.ParseProperty(nameof(Order));
        public static readonly DBColumn NameENKey = DBTable.ParseProperty(nameof(NameEN));
        public static readonly DBColumn NameRUKey = DBTable.ParseProperty(nameof(NameRU));
        public static readonly DBColumn IntervalKey = DBTable.ParseProperty(nameof(Interval));
        public static readonly DBColumn ProcedureKey = DBTable.ParseProperty(nameof(ProcedureName));
        public static readonly DBColumn DateExecuteKey = DBTable.ParseProperty(nameof(DateExecute));
        public static readonly DBColumn CompanyKey = DBTable.ParseProperty(nameof(Company));

        private Company company;

        public Scheduler()
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Column("company_id"), Browsable(false)]
        public int? CompanyId
        {
            get => GetValue<int?>(CompanyKey);
            set => SetValue(value, CompanyKey);
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get => GetReference(CompanyKey, ref company);
            set => SetReference(company = value, CompanyKey);
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

        public string NameEN
        {
            get => GetValue<string>(NameENKey);
            set => SetValue(value, NameENKey);
        }

        public string NameRU
        {
            get => GetValue<string>(NameRUKey);
            set => SetValue(value, NameRUKey);
        }

        [Column("orderid")]
        public int? Order
        {
            get => GetValue<int?>(OrderKey);
            set => SetValue(value, OrderKey);
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
            get => GetValue<TimeSpan?>(IntervalKey);
            set => SetValue(value, IntervalKey);
        }

        [Browsable(false)]
        [Column("procedure_name")]
        public string ProcedureName
        {
            get => GetValue<string>(ProcedureKey);
            set => SetValue(value, ProcedureKey);
        }

        public DBProcedure Procedure
        {
            get => DBService.Schems.ParseProcedure(ProcedureName);
            set => ProcedureName = value?.Name;
        }

        [Column("date_execute")]
        public DateTime? DateExecute
        {
            get => GetValue<DateTime?>(DateExecuteKey);
            set => SetValue(value, DateExecuteKey);
        }

        public async Task<StateInfo> Execute()
        {
            using (var transaction = new DBTransaction())
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
                if (result is decimal rez && Statistic.DBTable != null)
                {
                    var stat = new Statistic
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

        [ControllerMethod]
        public static void Start()
        {
            if (SchedulerService.Instance == null)
                throw new Exception($"{nameof(SchedulerService)} is not initialized!");

            if (!SchedulerService.Instance.Running)
            {
                SchedulerService.Instance.Start();
            }
        }

        [ControllerMethod]
        public static void Stop()
        {
            if (SchedulerService.Instance == null)
                throw new Exception($"{nameof(SchedulerService)} is not initialized!");

            if (SchedulerService.Instance.Running)
            {
                SchedulerService.Instance.Stop();
            }
        }

        [ControllerMethod]
        public static bool IsRunning()
        {
            if (SchedulerService.Instance == null)
                throw new Exception($"{nameof(SchedulerService)} is not initialized!");

            return SchedulerService.Instance.Running;
        }
    }
}
