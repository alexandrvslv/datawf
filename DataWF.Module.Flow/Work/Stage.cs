using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{

    [Table("rstage", "Template", BlockSize = 100), InvokerGenerator]
    public sealed partial class Stage : DBItem, IDisposable
    {
        private Work work;

        public Stage(DBTable table) : base(table)
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(StageTable.IdKey);
            set => SetValue(value, StageTable.IdKey);
        }

        [Column("code", 512, Keys = DBColumnKeys.Code)]
        public string Code
        {
            get => GetValue<string>(StageTable.CodeKey);
            set => SetValue(value, StageTable.CodeKey);
        }

        [Column("export_code", 512)]
        public string ExportCode
        {
            get => GetValue<string>(StageTable.ExportCodeKey);
            set => SetValue(value, StageTable.ExportCodeKey);
        }

        [Column("name", 512, Keys = DBColumnKeys.Culture | DBColumnKeys.View)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }

        [CultureKey(nameof(Name))]
        public string NameEN
        {
            get => GetValue<string>(StageTable.NameENKey);
            set => SetValue(value, StageTable.NameENKey);
        }

        [CultureKey(nameof(Name))]
        public string NameRU
        {
            get => GetValue<string>(StageTable.NameRUKey);
            set => SetValue(value, StageTable.NameRUKey);
        }

        [Browsable(false)]
        [Column("work_id")]
        public int? WorkId
        {
            get => GetValue<int?>(StageTable.WorkIdKey);
            set => SetValue(value, StageTable.WorkIdKey);
        }

        [Reference(nameof(WorkId))]
        public Work Work
        {
            get => GetReference(StageTable.WorkIdKey, ref work);
            set => SetReference(work = value, StageTable.WorkIdKey);
        }

        [Column("keys")]
        public StageKey? Keys
        {
            get => GetValue<StageKey?>(StageTable.KeysKey);
            set => SetValue(value, StageTable.KeysKey);
        }

        [Column("time_limit")]
        public TimeSpan? TimeLimit
        {
            get => GetValue<TimeSpan?>(StageTable.TimeLimitKey);
            set => SetValue(value, StageTable.TimeLimitKey);
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
