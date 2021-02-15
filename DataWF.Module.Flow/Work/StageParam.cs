using DataWF.Common;
using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Module.Flow
{

    [Table("rstage_param", "Template", BlockSize = 200), InvokerGenerator]
    public partial class StageParam : DBItem
    {
        private object _cache;
        private Stage stage;

        public StageParam(DBTable table) : base(table)
        { }

        public IStageParamTable StageParamTable => (IStageParamTable)Table;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue<int>(StageParamTable.IdKey);
            set => SetValue(value, StageParamTable.IdKey);
        }

        [Browsable(false)]
        [Column("stage_id")]
        public int? StageId
        {
            get => GetValue<int?>(StageParamTable.StageIdKey);
            set => SetValue(value, StageParamTable.StageIdKey);
        }

        [Reference(nameof(StageId))]
        public Stage Stage
        {
            get => GetReference(StageParamTable.StageIdKey, ref stage);
            set => SetReference(stage = value, StageParamTable.StageIdKey);
        }

        [Column("code", 1024)]
        public string ParamCode
        {
            get => GetValue<string>(StageParamTable.ParamCodeKey);
            set => SetValue(value, StageParamTable.ParamCodeKey);
        }

        [Column("name", 1024)]
        public string Name
        {
            get => GetValue<string>(StageParamTable.NameKey);
            set => SetValue(value, StageParamTable.NameKey);
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public object Param
        {
            get
            {
                if (_cache == null && !string.IsNullOrEmpty(ParamCode))
                {
                    StageParamType type = (StageParamType)ItemType;
                    switch (type)
                    {
                        case StageParamType.Column:
                            _cache = GetColumn();
                            break;
                        case StageParamType.Foreign:
                            _cache = GetReference();
                            break;
                        case StageParamType.Procedure:
                            _cache = DBService.Schems.ParseProcedure(ParamCode);
                            break;
                        case StageParamType.Reference:
                            _cache = Schema.GetTable<Stage>().LoadItemById(ParamCode);
                            break;
                        case StageParamType.Template:
                            _cache = Schema.GetTable<Template>().LoadItemById(ParamCode);
                            break;
                    }
                }
                return _cache;
            }
            set
            {
                ParamCode = value == null ? null : value is DBItem item ? item.PrimaryId.ToString() : ((DBSchemaItem)value).FullName;
                _cache = value;
            }
        }

        public DBForeignKey GetReference()
        {
            int index = ParamCode.IndexOf(' ');
            string code = ParamCode.Substring(0, index < 0 ? ParamCode.Length : index);

            DBColumn column = DBService.Schems.ParseColumn(code, Table.Schema);
            //var result = new DBConstraintForeign() { Table = column.Table, Column = column, Value = column.Reference };
            //if (index >= 0) result.Value = ParamCode.Substring(index + 1);
            return column?.GetForeign();
        }

        public DBColumn GetColumn()
        {
            int index = ParamCode.IndexOf(' ');
            string code = ParamCode.Substring(0, index < 0 ? ParamCode.Length : index);
            DBColumn column = DBService.Schems.ParseColumn(code, Table.Schema);
            return column;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", (StageParamType)ItemType, Param);
        }
    }

    [ItemType((int)StageParamType.Reference), InvokerGenerator]
    public sealed partial class StageReference : StageParam
    {
        public StageReference(DBTable table) : base(table)
        {
            ItemType = (int)StageParamType.Reference;
        }

        public Stage ReferenceStage
        {
            get => Param as Stage;
            set => Param = value;
        }

        [Column("is_next")]
        public bool? Next
        {
            get => GetValue<bool?>(StageReferenceTable.NextKey);
            set => SetValue(value, StageReferenceTable.NextKey);
        }
    }

    [ItemType((int)StageParamType.Template), InvokerGenerator]
    public sealed partial class StageTemplate : StageParam
    {
        public StageTemplate(DBTable table) : base(table)
        {
            ItemType = (int)StageParamType.Template;
        }

        public Template Template
        {
            get => Param as Template;
            set => Param = value;
        }
    }

    [ItemType((int)StageParamType.Foreign), InvokerGenerator]
    public sealed partial class StageForeign : StageParam
    {
        public StageForeign(DBTable table) : base(table)
        {
            ItemType = (int)StageParamType.Foreign;
        }

        [XmlIgnore, JsonIgnore]
        public DBForeignKey Foreign
        {
            get => Param as DBForeignKey;
            set => Param = value;
        }
    }
}
