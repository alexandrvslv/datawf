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
    public class StatisticList : DBTableView<Statistic>
    {
        public StatisticList(string filter, DBViewKeys mode = DBViewKeys.None)
            : base(Statistic.DBTable, filter, mode)
        {
            ApplySortInternal(new DBComparer(Statistic.DBTable.ParseProperty(nameof(Statistic.Date)), ListSortDirection.Ascending));
        }

        public StatisticList()
            : this(string.Empty)
        {
        }
    }

    [DataContract, Table("dstats", "Reference Book")]
    public class Statistic : DBItem//, IComparable
    {
        public static DBTable<Statistic> DBTable
        {
            get { return DBService.GetTable<Statistic>(); }
        }

        public Statistic()
        {
            Build(DBTable);
        }

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
            get { return GetProperty<int?>(nameof(SchedulerId)); }
            set { SetProperty(value, nameof(SchedulerId)); }
        }

        [Reference(nameof(SchedulerId))]
        public Scheduler Scheduler
        {
            get { return GetPropertyReference<Scheduler>(); }
            set { SetPropertyReference(value); }
        }

        [DataMember, Column("stat_result")]
        public decimal? Result
        {
            get { return GetProperty<decimal?>(); }
            set { SetProperty(value); }
        }
    }
}
