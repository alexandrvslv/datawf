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

    [Table("wf_common", "dstats")]
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

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
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
            get { return GetProperty<int?>(nameof(SchedulerId)); }
            set { SetProperty(value, nameof(SchedulerId)); }
        }

        [Reference("fk_dstats_scheduler_id", nameof(SchedulerId))]
        public Scheduler Scheduler
        {
            get { return GetPropertyReference<Scheduler>(nameof(SchedulerId)); }
            set { SetPropertyReference(value, nameof(SchedulerId)); }
        }

        [Column("stat_result")]
        public decimal? Result
        {
            get { return GetProperty<decimal?>(); }
            set { SetProperty(value); }
        }
    }
}
