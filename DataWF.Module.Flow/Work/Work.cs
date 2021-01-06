using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    [Table("rwork", "Template", BlockSize = 20), InvokerGenerator]
    public sealed partial class Work : DBItem, IDisposable
    {
        public Work(DBTable table) : base(table)
        {
        }

        public WorkTable WorkTable => (WorkTable)Table;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(WorkTable.IdKey);
            set => SetValue(value, WorkTable.IdKey);
        }

        [Column("code", Keys = DBColumnKeys.Code)]
        public string Code
        {
            get => GetValue<string>(WorkTable.CodeKey);
            set => SetValue(value, WorkTable.CodeKey);
        }

        [Column("name", Keys = DBColumnKeys.Culture | DBColumnKeys.View)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }

        [CultureKey(nameof(Name))]
        public string NameEN
        {
            get => GetValue<string>(WorkTable.NameENKey);
            set => SetValue(value, WorkTable.NameENKey);
        }

        [CultureKey(nameof(Name))]
        public string NameRU
        {
            get => GetValue<string>(WorkTable.NameRUKey);
            set => SetValue(value, WorkTable.NameRUKey);
        }

        [ControllerMethod]
        public IEnumerable<Template> GetTemplates()
        {
            var templateTable = (TemplateTable<Template>)Schema.GetTable<Template>();
            return GetReferencing<Template>(templateTable, templateTable.WorkIdKey, DBLoadParam.None)
                .OrderBy(p => p.Code);
        }

        [ControllerMethod]
        public IEnumerable<Stage> GetStages()
        {
            var stageTable = (StageTable)Schema.GetTable<Stage>();
            return GetReferencing<Stage>(stageTable.WorkIdKey, DBLoadParam.None)
            .OrderBy(p => p.Code);
        }

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
