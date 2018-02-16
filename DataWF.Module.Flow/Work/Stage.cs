﻿/*
 Stage.cs
 
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DataWF.Data;
using DataWF.Common;
using DataWF.Module.Common;

namespace DataWF.Module.Flow
{
    public class StageList : DBTableView<Stage>
    {
        public StageList(string filter, DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
            : base(Stage.DBTable, filter, mode, status)
        {
            ApplySortInternal(new DBComparer(Stage.DBTable.CodeKey, ListSortDirection.Ascending));
        }

        public StageList()
            : this("")
        {
        }

        public StageList(Work flow)
            : this(Stage.DBTable.ParseColumn(nameof(Stage.Work)).Name + "=" + flow.PrimaryId)
        {
        }
    }

    [Flags]
    public enum StageKey
    {
        MultiUser,
        IsStop,
        IsStart,
        IsSystem,
        IsReturn,
        IsAutoComplete
    }

    [Table("flow", "rstage", BlockSize = 200)]
    public class Stage : DBItem, IDisposable
    {
        public static DBTable<Stage> DBTable
        {
            get { return DBService.GetTable<Stage>(); }
        }

        public Stage()
        {
            Build(DBTable);
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetProperty<int?>(nameof(Id)); }
            set { SetProperty(value, nameof(Id)); }
        }

        [Column("code", Keys = DBColumnKeys.Code)]
        public string Code
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { SetValue(value, Table.CodeKey); }
        }

        [Column("name", 512, Keys = DBColumnKeys.Culture)]
        public override string Name
        {
            get { return GetName("name"); }
            set { SetName("name", value); }
        }

        [Browsable(false)]
        [Column("workid")]
        public int? WorkId
        {
            get { return GetProperty<int?>(nameof(WorkId)); }
            set { SetProperty(value, nameof(WorkId)); }
        }

        [Reference("fk_rstage_workid", nameof(WorkId))]
        public Work Work
        {
            get { return GetPropertyReference<Work>(nameof(WorkId)); }
            set { SetPropertyReference(value, nameof(WorkId)); }
        }

        [Column("keys")]
        public StageKey? Keys
        {
            get { return GetProperty<StageKey?>(nameof(Keys)); }
            set { SetProperty(value, nameof(Keys)); }
        }

        [Column("timelimit")]
        public TimeSpan? TimeLimit
        {
            get { return GetProperty<TimeSpan?>(nameof(TimeLimit)); }
            set { SetProperty(value, nameof(TimeLimit)); }
        }

        public IEnumerable<StageParam> GetParams()
        {
            var list = StageParam.DBTable.Select(StageParam.DBTable.ParseColumn(nameof(StageParam.Stage)), PrimaryId, CompareType.Equal).ToList();
            list.Sort(new DBComparer(StageParam.DBTable.PrimaryKey, ListSortDirection.Ascending));
            return list;
        }

        public IEnumerable<User> GetUsers()
        {
            foreach (var access in Access.Items)
            {
                if (access.Create)
                {
                    foreach (User user in User.DBTable)
                    {
                        if (user.Access.GetCreate(access.Group))
                            yield return user;
                    }
                }
            }
        }
    }
}
