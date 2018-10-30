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
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Common
{

    [DataContract, Table("rscheduler", "Reference Book", BlockSize = 20)]
    public class Scheduler : DBItem//, IComparable
    {
        private static DBColumn orderKey = DBColumn.EmptyKey;
        private static DBColumn intervalKey = DBColumn.EmptyKey;
        private static DBColumn procedureKey = DBColumn.EmptyKey;
        private static DBColumn dateExecuteKey = DBColumn.EmptyKey;
        private static DBTable<Scheduler> dbTable;

        public static DBColumn OrderKey => DBTable.ParseProperty(nameof(Order), ref orderKey);
        public static DBColumn IntervalKey => DBTable.ParseProperty(nameof(Interval), ref intervalKey);
        public static DBColumn ProcedureKey => DBTable.ParseProperty(nameof(ProcedureName), ref procedureKey);
        public static DBColumn DateExecuteKey => DBTable.ParseProperty(nameof(DateExecute), ref dateExecuteKey);
        public static DBTable<Scheduler> DBTable => dbTable ?? (dbTable = GetTable<Scheduler>());

        public Scheduler()
        { }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
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

        [DataMember, Column("orderid")]
        public int? Order
        {
            get { return GetValue<int?>(OrderKey); }
            set { SetValue(value, OrderKey); }
        }

        [DataMember, DefaultValue(SchedulerType.Interval), Column("type_id", Keys = DBColumnKeys.ElementType)]
        public SchedulerType? Type
        {
            get { return GetValue<SchedulerType?>(Table.ElementTypeKey); }
            set { SetValue(value, Table.ElementTypeKey); }
        }

        [DataMember, Column("run_interval")]
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

        public User RunBy
        {
            get { return null; }
        }

        public object Execute()
        {
            object rez = null;
            if (Procedure == null)
                throw new Exception("Procedure not specified!");

            var task = Procedure.ExecuteTask(null);
            var result = task.Execute();

            var info = new StateInfo
            {
                Module = "Task",
                Message = task.Name
            };

            if (result is Exception)
            {
                Helper.OnException((Exception)result);
                info.Description = string.Format("Fail!"); ;
                info.Type = StatusType.Warning;
            }
            else
            {
                DateExecute = DateTime.Now;
                Save(RunBy);

                if (result is decimal && Statistic.DBTable != null)
                {
                    var stat = new Statistic
                    {
                        Scheduler = this,
                        Result = (decimal)rez
                    };
                    stat.Save(RunBy);
                }

                info.Description = string.Format("Completed in {0:n} {1}", task.Time.TotalMilliseconds / 1000, result);
                info.Type = StatusType.Information;
            }
            Helper.Logs.Add(info);

            return result;
        }
    }
}
