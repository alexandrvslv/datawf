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
        private static DBColumn orderKey = DBColumn.EmptyKey;
        private static DBColumn intervalKey = DBColumn.EmptyKey;
        private static DBColumn procedureKey = DBColumn.EmptyKey;
        private static DBColumn dateExecuteKey = DBColumn.EmptyKey;
        private static DBColumn companyKey = DBColumn.EmptyKey;
        private static DBColumn nameENKey = DBColumn.EmptyKey;
        private static DBColumn nameRUKey = DBColumn.EmptyKey;
        private static DBTable<Scheduler> dbTable;
        private Company company;

        public static DBColumn OrderKey => DBTable.ParseProperty(nameof(Order), ref orderKey);
        public static DBColumn NameENKey => DBTable.ParseProperty(nameof(NameEN), ref nameENKey);
        public static DBColumn NameRUKey => DBTable.ParseProperty(nameof(NameRU), ref nameRUKey);
        public static DBColumn IntervalKey => DBTable.ParseProperty(nameof(Interval), ref intervalKey);
        public static DBColumn ProcedureKey => DBTable.ParseProperty(nameof(ProcedureName), ref procedureKey);
        public static DBColumn DateExecuteKey => DBTable.ParseProperty(nameof(DateExecute), ref dateExecuteKey);
        public static DBColumn CompanyKey => DBTable.ParseProperty(nameof(Company), ref companyKey);
        public static DBTable<Scheduler> DBTable => dbTable ?? (dbTable = GetTable<Scheduler>());

        public Scheduler()
        { }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [DataMember, Column("company_id"), Browsable(false)]
        public int? CompanyId
        {
            get { return GetValue<int?>(CompanyKey); }
            set { SetValue(value, CompanyKey); }
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get { return GetReference(CompanyKey, ref company); }
            set { SetReference(company = value, CompanyKey); }
        }

        [DataMember, Column("code", Keys = DBColumnKeys.Code)]
        public string Code
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { SetValue(value, Table.CodeKey); }
        }

        [DataMember, Column("name", Keys = DBColumnKeys.Culture | DBColumnKeys.View)]
        public string Name
        {
            get { return GetName(); }
            set { SetName(value); }
        }

        public string NameEN
        {
            get { return GetValue<string>(NameENKey); }
            set { SetValue(value, NameENKey); }
        }

        public string NameRU
        {
            get { return GetValue<string>(NameRUKey); }
            set { SetValue(value, NameRUKey); }
        }

        [DataMember, Column("orderid")]
        public int? Order
        {
            get { return GetValue<int?>(OrderKey); }
            set { SetValue(value, OrderKey); }
        }

        [DataMember, DefaultValue(SchedulerType.Interval), Column("type_id", Keys = DBColumnKeys.ElementType | DBColumnKeys.Notnull)]
        public SchedulerType? Type
        {
            get { return GetValue<SchedulerType?>(Table.ElementTypeKey); }
            set { SetValue(value, Table.ElementTypeKey); }
        }

        [DataMember, Column("run_interval", Keys = DBColumnKeys.Notnull)]
        public TimeSpan? Interval
        {
            get { return GetValue<TimeSpan?>(IntervalKey); }
            set { SetValue(value, IntervalKey); }
        }

        [Browsable(false)]
        [DataMember, Column("procedure_name")]
        public string ProcedureName
        {
            get { return GetValue<string>(ProcedureKey); }
            set { SetValue(value, ProcedureKey); }
        }

        public DBProcedure Procedure
        {
            get { return DBService.Schems.ParseProcedure(ProcedureName); }
            set { ProcedureName = value?.Name; }
        }

        [DataMember, Column("date_execute")]
        public DateTime? DateExecute
        {
            get { return GetValue<DateTime?>(DateExecuteKey); }
            set { SetValue(value, DateExecuteKey); }
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
            object rez = null;
            if (Procedure == null)
                throw new Exception("Procedure not specified!");

            var task = Procedure.GetExecutor(null, transaction);
            var result = task.Execute();

            var info = new StateInfo
            {
                Module = "Task",
                Message = Name ?? task.Name,
                Tag = result
            };

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
                if (result is decimal && Statistic.DBTable != null)
                {
                    var stat = new Statistic
                    {
                        Scheduler = this,
                        Result = (decimal)rez
                    };
                    await stat.Save(transaction);
                }

                info.Description = string.Format("Completed in {0:n} {1}", task.Time.TotalMilliseconds / 1000, result);
                info.Type = StatusType.Information;
            }
            DateExecute = DateTime.Now;
            await Save(transaction);

            Helper.Logs.Add(info);

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
