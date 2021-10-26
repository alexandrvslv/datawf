using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    public class WorkList : DBTableView<Work>
    {
        public WorkList(string filter = "", DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
            : base(filter, mode, status)
        {
            ApplySortInternal(new DBComparer<Work, string>(Work.DBTable.CodeKey, ListSortDirection.Ascending));
        }

    }

    [Table("rwork", "Template", BlockSize = 20)]
    public class Work : DBItem, IDisposable
    {
        public static readonly DBTable<Work> DBTable = GetTable<Work>();

        public static readonly DBColumn NameENKey = DBTable.ParseProperty(nameof(NameEN));
        public static readonly DBColumn NameRUKey = DBTable.ParseProperty(nameof(NameRU));

        public Work()
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
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

        [ControllerMethod]
        public IEnumerable<Template> GetTemplates()
        {
            return GetReferencing<Template>(Template.DBTable, Template.WorkKey, DBLoadParam.None)
                .OrderBy(p => p.Code);
        }

        [ControllerMethod]
        public IEnumerable<Stage> GetStages() =>
            GetReferencing<Stage>(Stage.DBTable, Stage.WorkKey, DBLoadParam.None)
            .OrderBy(p => p.Code);

        [ControllerMethod]
        public Stage GetStartStage()
        {
            foreach (Stage stage in GetStages())
                if (stage.Keys != null && (stage.Keys & StageKey.Start) == StageKey.Start)
                    return stage;
            return GetStages().FirstOrDefault();
        }

        [ControllerMethod]
        public Stage GetStopStage()
        {
            foreach (Stage stage in GetStages())
                if (stage.Keys != null && (stage.Keys & StageKey.Stop) == StageKey.Stop)
                    return stage;
            return GetStages().LastOrDefault();
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
