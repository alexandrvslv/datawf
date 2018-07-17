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
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Reflection;
using DataWF.Data;
using DataWF.Common;
using System.Runtime.Serialization;

namespace DataWF.Module.Common
{
    public class SchedulerList : DBTableView<Scheduler>
    {
        public SchedulerList(string filter, DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
            : base(Scheduler.DBTable, filter, mode, status)
        {
            ApplySortInternal(new DBComparer(Scheduler.DBTable.ParseProperty(nameof(Scheduler.Order)), ListSortDirection.Ascending));
        }

        public SchedulerList()
            : this(string.Empty)
        {
        }
    }

    [DataContract, Table("rscheduler", "Reference Book", BlockSize = 20)]
    public class Scheduler : DBItem//, IComparable
    {
        public static DBTable<Scheduler> DBTable
        {
            get { return GetTable<Scheduler>(); }
        }

        public Scheduler()
        {
            Build(DBTable);
            Type = SchedulerType.Interval;
        }

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
            get { return GetProperty<int?>(nameof(Order)); }
            set { SetProperty(value, nameof(Order)); }
        }

        [DataMember, Column("type_id", Keys = DBColumnKeys.ElementType)]
        public SchedulerType? Type
        {
            get { return GetProperty<SchedulerType?>(nameof(SchedulerType)); }
            set { SetProperty(value, nameof(SchedulerType)); }
        }

        [DataMember, Column("run_interval")]
        public TimeSpan? Interval
        {
            get { return GetProperty<TimeSpan?>(nameof(Interval)); }
            set { SetProperty(value, nameof(Interval)); }
        }

        [Browsable(false)]
        [DataMember, Column("procedure_name")]
        public string ProcedureName
        {
            get { return GetProperty<string>(nameof(ProcedureName)); }
            set { SetProperty(value, nameof(ProcedureName)); }
        }

        public DBProcedure Procedure
        {
            get { return DBService.ParseProcedure(ProcedureName); }
            set { ProcedureName = value?.Name; }
        }

        [DataMember, Column("date_execute")]
        public DateTime? DateExecute
        {
            get { return GetProperty<DateTime?>(nameof(DateExecute)); }
            set { SetProperty(value, nameof(DateExecute)); }
        }

        public object Execute()
        {
            object rez = null;
            if (Procedure == null)
                throw new Exception("Procedure not specified!");

            var task = Procedure.ExecuteTask(null);
            var result = task.Execute();

            var info = new StateInfo();
            info.Module = "Task";
            info.Message = task.Name;

            if (result is Exception)
            {
                Helper.OnException((Exception)result);
                info.Description = string.Format("Fail!"); ;
                info.Type = StatusType.Warning;
            }
            else
            {
                DateExecute = DateTime.Now;
                Save();

                if (result is decimal && Statistic.DBTable != null)
                {
                    var stat = new Statistic();
                    stat.Scheduler = this;
                    stat.Result = (decimal)rez;
                    stat.Save();
                }

                info.Description = string.Format("Completed in {0:n} {1}", task.Time.TotalMilliseconds / 1000, result);
                info.Type = StatusType.Information;
            }
            Helper.Logs.Add(info);

            return result;
        }
    }
}
