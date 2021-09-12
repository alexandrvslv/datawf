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

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue<int>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Browsable(false)]
        [Column("stage_id")]
        public int? StageId
        {
            get => GetValue<int?>(Table.StageIdKey);
            set => SetValue(value, Table.StageIdKey);
        }

        [Reference(nameof(StageId))]
        public Stage Stage
        {
            get => GetReference(Table.StageIdKey, ref stage);
            set => SetReference(stage = value, Table.StageIdKey);
        }

        [Column("code", 1024)]
        public string ParamCode
        {
            get => GetValue<string>(Table.ParamCodeKey);
            set => SetValue(value, Table.ParamCodeKey);
        }

        [Column("name", 1024)]
        public string Name
        {
            get => GetValue<string>(Table.NameKey);
            set => SetValue(value, Table.NameKey);
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
                            _cache = Schema.ParseProcedure(ParamCode);
                            break;
                        case StageParamType.Reference:
                            _cache = Schema.Stage.LoadById(ParamCode);
                            break;
                        case StageParamType.Template:
                            _cache = Schema.Template.LoadById(ParamCode);
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

            var column = Schema.ParseColumn(code);
            //var result = new DBConstraintForeign() { Table = column.Table, Column = column, Value = column.Reference };
            //if (index >= 0) result.Value = ParamCode.Substring(index + 1);
            return column?.GetForeign();
        }

        public DBColumn GetColumn()
        {
            int index = ParamCode.IndexOf(' ');
            string code = ParamCode.Substring(0, index < 0 ? ParamCode.Length : index);
            DBColumn column = Schema.ParseColumn(code);
            return column;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", (StageParamType)ItemType, Param);
        }
    }

    [VirtualTable((int)StageParamType.Reference), InvokerGenerator]
    public sealed partial class StageReference : StageParam
    {
        public Stage ReferenceStage
        {
            get => Param as Stage;
            set => Param = value;
        }

        [Column("is_next")]
        public bool? Next
        {
            get => GetValue<bool?>(Table.NextKey);
            set => SetValue(value, Table.NextKey);
        }
    }

    [VirtualTable((int)StageParamType.Template), InvokerGenerator]
    public sealed partial class StageTemplate : StageParam
    {
        public Template Template
        {
            get => Param as Template;
            set => Param = value;
        }
    }

    [VirtualTable((int)StageParamType.Foreign), InvokerGenerator]
    public sealed partial class StageForeign : StageParam
    {
        [XmlIgnore, JsonIgnore]
        public DBForeignKey Foreign
        {
            get => Param as DBForeignKey;
            set => Param = value;
        }
    }
}
