﻿/*
 Flow.cs
 
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
using System.ComponentModel;
using DataWF.Data;
using System.Collections.Generic;
using DataWF.Common;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    public class WorkList : DBTableView<Work>
    {
        public WorkList(string filter = "", DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
            : base(filter, mode, status)
        {
            ApplySortInternal(new DBComparer<Work>(Work.DBTable.CodeKey, ListSortDirection.Ascending));
        }

    }

    [DataContract, Table("rwork", "Template", BlockSize = 20)]
    public class Work : DBItem, IDisposable
    {
        public static DBTable<Work> DBTable
        {
            get { return GetTable<Work>(); }
        }

        public Work()
        {
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

        public string NameEN
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        public string NameRU
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        [ControllerMethod]
        public List<Template> GetTemplates()
        {
            var list = new List<Template>(Template.DBTable.Select(Template.DBTable.ParseProperty(nameof(Template.WorkId)), CompareType.Equal, PrimaryId));
            list.Sort(new DBComparer<Template>(Template.DBTable.CodeKey, ListSortDirection.Ascending));
            return list;
        }

        [ControllerMethod]
        public List<Stage> GetStages()
        {
            var list = new List<Stage>(Stage.DBTable.Select(Stage.DBTable.ParseProperty(nameof(Stage.WorkId)), CompareType.Equal, PrimaryId));
            list.Sort(new DBComparer<Stage>(Stage.DBTable.CodeKey, ListSortDirection.Ascending));
            return list;
        }

        [ControllerMethod]
        public Stage GetStartStage()
        {
            var stages = GetStages();
            foreach (Stage stage in stages)
                if (stage.Keys != null && (stage.Keys & StageKey.Start) == StageKey.Start)
                    return stage;
            return stages.Count == 0 ? null : stages[0];
        }

        [ControllerMethod]
        public Stage GetStopStage()
        {
            var stages = GetStages();
            foreach (Stage stage in stages)
                if (stage.Keys != null && (stage.Keys & StageKey.Stop) == StageKey.Stop)
                    return stage;
            return stages.Count == 0 ? null : stages[stages.Count - 1];
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
