/*
 User.cs
 
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
using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Module.Common
{
    [DataContract, Table("wf_common", "rposition", "User")]
    public class Position : DBItem
    {
        public static DBTable<Position> DBTable
        {
            get { return DBService.GetTable<Position>(); }
        }

        public Position()
        {
            Build(DBTable);
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [DataMember, Column("department_id"), Index("rposition_department_id"), Browsable(false)]
        public int? DepartmentId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference("fk_rposition_department_id", nameof(DepartmentId))]
        public Department Department
        {
            get { return GetPropertyReference<Department>(nameof(DepartmentId)); }
            set { SetPropertyReference(value, nameof(DepartmentId)); }
        }

        [DataMember, Column("parent_id", Keys = DBColumnKeys.Group), Index("rposition_parent_id"), Browsable(false)]
        public int? ParentId
        {
            get { return GetValue<int?>(Table.GroupKey); }
            set { this[Table.GroupKey] = value; }
        }

        [Reference("fk_rposition_parent_id", nameof(ParentId))]
        public Position Parent
        {
            get { return GetReference<Position>(Table.GroupKey); }
            set { SetReference(value, Table.GroupKey); }
        }

        [DataMember, Column("code", 40, Keys = DBColumnKeys.Code | DBColumnKeys.View | DBColumnKeys.Indexing)]
        [Index("rposition_code", true)]
        public string Code
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { SetValue(value, Table.CodeKey); }
        }

        [DataMember, Column("name", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public override string Name
        {
            get { return GetName(nameof(Name)); }
            set { SetName(nameof(Name), value); }
        }

        public IEnumerable<User> GetUsers()
        {
            return GetReferencing<User>(nameof(User.PositionId), DBLoadParam.None);
        }
    }
}
