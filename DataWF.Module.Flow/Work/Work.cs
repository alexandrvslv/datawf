/*
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

namespace DataWF.Module.Flow
{
    public class WorkList : DBTableView<Work>
    {
        public WorkList(string filter = "", DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
            : base(Work.DBTable, filter, mode, status)
        {
            ApplySortInternal(new DBComparer<Work>(Work.DBTable.CodeKey, ListSortDirection.Ascending));
        }

    }

    [Table("wf_flow", "rwork", "Reference Book", BlockSize = 20)]
    public class Work : DBItem, IDisposable
    {
        public static DBTable<Work> DBTable
        {
            get { return DBService.GetTable<Work>(); }
        }

        public Work()
        {
            Build(DBTable);
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Column("code", Keys = DBColumnKeys.Code)]
        public string Code
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { SetValue(value, Table.CodeKey); }
        }

        [Column("name", Keys = DBColumnKeys.Culture)]
        public override string Name
        {
            get { return GetName(nameof(Name)); }
            set { SetName(nameof(Name), value); }
        }

        public List<Template> GetTemplates()
        {
            var list = new List<Template>(Template.DBTable.Select(Template.DBTable.ParseProperty(nameof(Template.WorkId)), PrimaryId, CompareType.Equal));
            list.Sort(new DBComparer<Template>(Template.DBTable.CodeKey, ListSortDirection.Ascending));
            return list;
        }

        public List<Stage> GetStages()
        {
            var list = new List<Stage>(Stage.DBTable.Select(Stage.DBTable.ParseProperty(nameof(Stage.WorkId)), PrimaryId, CompareType.Equal));
            list.Sort(new DBComparer<Stage>(Stage.DBTable.CodeKey, ListSortDirection.Ascending));
            return list;
        }

        public Stage GetStartStage()
        {
            var stages = GetStages();
            foreach (Stage stage in stages)
                if ((stage.Keys & StageKey.IsStart) == StageKey.IsStart)
                    return stage;
            return stages.Count == 0 ? null : stages[0];
        }

        public Stage GetStopStage()
        {
            var stages = GetStages();
            foreach (Stage stage in stages)
                if ((stage.Keys & StageKey.IsStop) == StageKey.IsStop)
                    return stage;
            return stages.Count == 0 ? null : stages[stages.Count - 1];
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
