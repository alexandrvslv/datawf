using DataWF.Common;
using DataWF.Data;

namespace DataWF.Module.Flow
{
    [ItemType((int)StageParamType.Procedure), InvokerGenerator]
    public sealed partial class StageProcedure : StageParam
    {
        public StageProcedure(DBTable table) : base(table)
        {
            ItemType = (int)StageParamType.Procedure;
        }

        public DBProcedure Procedure
        {
            get => Param as DBProcedure;
            set => Param = value;
        }

        [Column("procedure_type")]
        public StageParamProcudureType? ProcedureType
        {
            get => GetValue<StageParamProcudureType?>(StageProcedureTable.ProcedureTypeKey);
            set => SetValue(value, StageProcedureTable.ProcedureTypeKey);
        }
    }
}
