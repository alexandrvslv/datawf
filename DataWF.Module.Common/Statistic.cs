/*
 Statistic.cs
 
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
using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Common
{
    [DataContract, Table("dstats", "Reference Book")]
    public class Statistic : DBItem//, IComparable
    {
        private static DBColumn schedulerKey = DBColumn.EmptyKey;
        private static DBColumn resultKey = DBColumn.EmptyKey;
        private static DBTable<Statistic> dbTable;

        public static DBColumn SchedulerKey => DBTable.ParseProperty(nameof(SchedulerId), ref schedulerKey);
        public static DBColumn ResultKey => DBTable.ParseProperty(nameof(Result), ref resultKey);
        public static DBTable<Statistic> DBTable => dbTable ?? (dbTable = GetTable<Statistic>());

        public Statistic()
        { }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        //[DataMember, Column("code", Keys = DBColumnKeys.Code)]
        //public string Code
        //{
        //    get { return GetValue<string>(Table.CodeKey); }
        //    set { SetValue(value, Table.CodeKey); }
        //}

        //[DataMember, Column("name", Keys = DBColumnKeys.Culture)]
        //public override string Name
        //{
        //    get { return GetName("name"); }
        //    set { SetName("name", value); }
        //}

        [Browsable(false)]
        [DataMember, Column("scheduler_id")]
        public int? SchedulerId
        {
            get { return GetValue<int?>(SchedulerKey); }
            set { SetValue(value, SchedulerKey); }
        }

        [Reference(nameof(SchedulerId))]
        public Scheduler Scheduler
        {
            get { return GetReference<Scheduler>(SchedulerKey); }
            set { SetReference(value, SchedulerKey); }
        }

        [DataMember, Column("stat_result")]
        public decimal? Result
        {
            get { return GetValue<decimal?>(ResultKey); }
            set { SetValue(value, ResultKey); }
        }
    }
}
