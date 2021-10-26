using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    public class StageList : DBTableView<Stage>
    {
        public StageList(string filter, DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
            : base(filter, mode, status)
        {
            ApplySortInternal(new DBComparer<Stage, string>(Stage.DBTable.CodeKey, ListSortDirection.Ascending));
        }

        public StageList()
            : this("")
        {
        }

        public StageList(Work flow)
            : this(Stage.WorkKey.Name + "=" + flow.PrimaryId)
        {
        }
    }

    [Flags]
    public enum StageKey
    {
        None = 0,
        Stop = 1,
        Start = 2,
        System = 4,
        Return = 8,
        AutoComplete = 16
    }

    [Table("rstage", "Template", BlockSize = 100)]
    public class Stage : DBItem, IDisposable
    {
        public static readonly DBTable<Stage> DBTable = GetTable<Stage>();
        public static readonly DBColumn ExportCodeKey = DBTable.ParseProperty(nameof(ExportCode));
        public static readonly DBColumn NameENKey = DBTable.ParseProperty(nameof(NameEN));
        public static readonly DBColumn NameRUKey = DBTable.ParseProperty(nameof(NameRU));
        public static readonly DBColumn WorkKey = DBTable.ParseProperty(nameof(WorkId));
        public static readonly DBColumn KeysKey = DBTable.ParseProperty(nameof(Keys));
        public static readonly DBColumn TimeLimitKey = DBTable.ParseProperty(nameof(TimeLimit));

        private Work work;

        public Stage()
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(table.PrimaryKey);
            set => SetValue(value, table.PrimaryKey);
        }

        [Column("code", 512, Keys = DBColumnKeys.Code)]
        public string Code
        {
            get => GetValue<string>(table.CodeKey);
            set => SetValue(value, table.CodeKey);
        }

        [Column("export_code", 512)]
        public string ExportCode
        {
            get => GetValue<string>(ExportCodeKey);
            set => SetValue(value, ExportCodeKey);
        }

        [Column("name", 512, Keys = DBColumnKeys.Culture | DBColumnKeys.View)]
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

        [Browsable(false)]
        [Column("work_id")]
        public int? WorkId
        {
            get => GetValue<int?>(WorkKey);
            set => SetValue(value, WorkKey);
        }

        [Reference(nameof(WorkId))]
        public Work Work
        {
            get => GetReference(WorkKey, ref work);
            set => SetReference(work = value, WorkKey);
        }

        [Column("keys")]
        public StageKey? Keys
        {
            get => GetValue<StageKey?>(KeysKey);
            set => SetValue(value, KeysKey);
        }

        [Column("time_limit")]
        public TimeSpan? TimeLimit
        {
            get => GetValue<TimeSpan?>(TimeLimitKey);
            set => SetValue(value, TimeLimitKey);
        }

        public IEnumerable<T> GetParams<T>() where T : StageParam
        {
            return GetReferencing<StageParam>(nameof(StageParam.StageId), DBLoadParam.None).OfType<T>();
        }

        [ControllerMethod]
        public IEnumerable<StageParam> GetParams()
        {
            return GetReferencing<StageParam>(nameof(StageParam.StageId), DBLoadParam.None);
        }

        [ControllerMethod]
        public IEnumerable<StageReference> GetReferences()
        {
            return GetParams<StageReference>();
        }

        [ControllerMethod]
        public StageReference GetNextReference()
        {
            return GetParams<StageReference>().FirstOrDefault(p => p.Next ?? false);
        }

        [ControllerMethod]
        public IEnumerable<StageProcedure> GetProcedures()
        {
            return GetParams<StageProcedure>();
        }

        [ControllerMethod]
        public IEnumerable<StageProcedure> GetProceduresByType(StageParamProcudureType type)
        {
            return GetParams<StageProcedure>().Where(p => p.ProcedureType == type);
        }


    }
}
