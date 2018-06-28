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
using DataWF.Data;
using DataWF.Common;
using System.ComponentModel;
using System.Runtime.Serialization;
using DataWF.Module.Common;
using System.Collections.Generic;

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
            ApplySortInternal(new DBComparer(StageParam.DBTable.PrimaryKey, ListSortDirection.Ascending));
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

    [DataContract, Table("rstage_param", "Template", BlockSize = 200)]
    public class StageParam : DBItem
    {
        private object _cache;

        public static DBTable<StageParam> DBTable
        {
            get { return GetTable<StageParam>(); }
        }

        public StageParam()
        {
            Build(DBTable);
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetProperty<int?>(nameof(Id)); }
            set { SetProperty(value, nameof(Id)); }
        }

        [Browsable(false)]
        [DataMember, Column("stage_id")]
        public int? StageId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(StageId))]
        public Stage Stage
        {
            get { return GetPropertyReference<Stage>(); }
            set { SetPropertyReference(value); }
        }

        [DataMember, Column("code", 1024)]
        public string ParamCode
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        [Browsable(false)]
        public object Param
        {
            get
            {
                if (_cache == null)
                {
                    if (!string.IsNullOrEmpty(ParamCode))
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
                                _cache = DBService.ParseProcedure(ParamCode);
                                break;
                            case StageParamType.Reference:
                                _cache = Stage.DBTable.LoadItemById(ParamCode);
                                break;
                            case StageParamType.Template:
                                _cache = Template.DBTable.LoadItemById(ParamCode);
                                break;
                        }
                    }
                }
                return _cache;
            }
            set
            {
                ParamCode = value == null ? null : value is DBItem ? ((DBItem)value).PrimaryId.ToString() : ((DBSchemaItem)value).FullName;
                _cache = null;
            }
        }

        public DBForeignKey GetReference()
        {
            int index = ParamCode.IndexOf(' ');
            string code = ParamCode.Substring(0, index < 0 ? ParamCode.Length : index);

            DBColumn column = DBService.ParseColumn(code, Table.Schema);
            //var result = new DBConstraintForeign() { Table = column.Table, Column = column, Value = column.Reference };
            //if (index >= 0) result.Value = ParamCode.Substring(index + 1);
            return column == null ? null : column.GetForeign();
        }

        public DBColumn GetColumn()
        {
            DBColumn result = null;

            int index = ParamCode.IndexOf(' ');
            string code = ParamCode.Substring(0, index < 0 ? ParamCode.Length : index);
            DBColumn column = DBService.ParseColumn(code, Table.Schema);
            if (column != null)
            {
                result = new DBVirtualColumn(column);
                result.Index = column.Index;
                result.Table = column.Table;
                result.Access = this.Access;
                //TODO reference if (index >= 0)
                //    result.Reference = ParamCode.Substring(index + 1);

                //result.LocaleInfo = (LocaleItem)column.LocaleInfo.Clone();
            }
            return result;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", (StageParamType)ItemType, Param);
        }
    }

    [ItemType((int)StageParamType.Procedure)]
    public class StageProcedure : StageParam
    {
        public StageProcedure()
        {
            ItemType = (int)StageParamType.Procedure;
        }

        public DBProcedure Procedure
        {
            get { return Param as DBProcedure; }
            set { Param = value; }
        }

        [DataMember, Column("procedure_type")]
        public StageParamProcudureType? ProcedureType
        {
            get { return GetProperty<StageParamProcudureType?>(); }
            set { SetProperty(value); }
        }
    }

    [ItemType((int)StageParamType.Reference)]
    public class StageReference : StageParam
    {
        public StageReference()
        {
            ItemType = (int)StageParamType.Reference;
        }

        public Stage ReferenceStage
        {
            get { return Param as Stage; }
            set { Param = value; }
        }

        [DataMember, Column("is_next")]
        public bool? Next
        {
            get => GetProperty<bool?>();
            set => SetProperty(value);
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
            get { return Param as DBForeignKey; }
            set { Param = value; }
        }
    }
}
