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

namespace DataWF.Module.Flow
{
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

    [DataContract, Table("wf_flow", "rstage_param", "Template", BlockSize = 200)]
    public class StageParam : ParamBase
    {
        public static DBTable<StageParam> DBTable
        {
            get { return DBService.GetTable<StageParam>(); }
        }

        public StageParam()
        {
            Build(DBTable);
            Type = ParamType.Relation;
        }

        public override DBItem Owner
        {
            get { return Stage; }
        }

        [DataMember, Column("code")]
        public override string ParamCode
        {
            get { return GetProperty<string>(nameof(ParamCode)); }
            set { SetProperty(value, nameof(ParamCode)); }
        }

        [Browsable(false)]
        [DataMember, Column("stage_id")]
        public int? StageId
        {
            get { return GetProperty<int?>(nameof(StageId)); }
            set { SetProperty(value, nameof(StageId)); }
        }

        [Reference(nameof(StageId))]
        public Stage Stage
        {
            get { return GetPropertyReference<Stage>(); }
            set { SetPropertyReference(value); }
        }
    }
}
