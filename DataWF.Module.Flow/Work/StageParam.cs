/*
 StageParam.cs
 
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
using DataWF.Common;
using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{

    public enum StageParamType
    {
        None,
        Column,
        Foreign,
        Template,
        Procedure,
        Reference,
    }

    public enum StageParamProcudureType
    {
        Start,
        Manual,
        Check,
        Finish
    }

    public class StageParamList : DBTableView<StageParam>
    {
        public StageParamList(string filter, DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
            : base(filter, mode, status)
        {
            ApplySortInternal(new DBComparer<StageParam, int?>(StageParam.DBTable.PrimaryKey, ListSortDirection.Ascending));
        }

        public StageParamList()
            : this(string.Empty)
        {
        }

        public StageParamList(Stage stage)
            : this(StageParam.DBTable.ParseProperty(nameof(StageParam.StageId)).Name + "=" + stage.PrimaryId)
        {
        }
    }

    [Table("rstage_param", "Template", BlockSize = 200)]
    public class StageParam : DBItem
    {
        public static readonly DBTable<StageParam> DBTable = GetTable<StageParam>();

        public static readonly DBColumn StageKey = DBTable.ParseProperty(nameof(StageId));
        public static readonly DBColumn ParamCodeKey = DBTable.ParseProperty(nameof(ParamCode));
        public static readonly DBColumn NameKey = DBTable.ParseProperty(nameof(Name));

        private object _cache;
        private Stage stage;

        public StageParam()
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Browsable(false)]
        [Column("stage_id")]
        public int? StageId
        {
            get => GetValue<int?>(StageKey);
            set => SetValue(value, StageKey);
        }

        [Reference(nameof(StageId))]
        public Stage Stage
        {
            get => GetReference(StageKey, ref stage);
            set => SetReference(stage = value, StageKey);
        }

        [Column("code", 1024)]
        public string ParamCode
        {
            get => GetValue<string>(ParamCodeKey);
            set => SetValue(value, ParamCodeKey);
        }

        [Column("name", 1024)]
        public string Name
        {
            get => GetValue<string>(NameKey);
            set => SetValue(value, NameKey);
        }

        [Browsable(false)]
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
                            _cache = Stage.DBTable.LoadItemById(ParamCode);
                            break;
                        case StageParamType.Template:
                            _cache = Template.DBTable.LoadItemById(ParamCode);
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

    [ItemType((int)StageParamType.Procedure)]
    public class StageProcedure : StageParam
    {
        private static DBColumn procedureTypeKey = DBColumn.EmptyKey;
        public static DBColumn ProcedureTypeKey => DBTable.ParseProperty(nameof(ProcedureType), ref procedureTypeKey);

        public StageProcedure()
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
            get => GetValue<StageParamProcudureType?>(ProcedureTypeKey);
            set => SetValue(value, ProcedureTypeKey);
        }
    }

    [ItemType((int)StageParamType.Reference)]
    public class StageReference : StageParam
    {
        private static DBColumn nextKey = DBColumn.EmptyKey;
        public static DBColumn NextKey => DBTable.ParseProperty(nameof(Next), ref nextKey);

        public StageReference()
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
            get => GetValue<bool?>(NextKey);
            set => SetValue(value, NextKey);
        }
    }

    [ItemType((int)StageParamType.Template)]
    public class StageTemplate : StageParam
    {
        public StageTemplate()
        {
            ItemType = (int)StageParamType.Template;
        }

        public Template Template
        {
            get => Param as Template;
            set => Param = value;
        }
    }

    [ItemType((int)StageParamType.Foreign)]
    public class StageForeign : StageParam
    {
        public StageForeign()
        {
            ItemType = (int)StageParamType.Foreign;
        }

        public DBForeignKey Foreign
        {
            get => Param as DBForeignKey;
            set => Param = value;
        }
    }
}
