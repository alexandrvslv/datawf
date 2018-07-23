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
    [DataContract, Table("rposition", "User")]
    public class Position : DBGroupItem
    {
        public static DBTable<Position> DBTable
        {
            get { return GetTable<Position>(); }
        }

        public Position()
        {
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

        [Reference(nameof(DepartmentId))]
        public Department Department
        {
            get { return GetPropertyReference<Department>(); }
            set { SetPropertyReference(value); }
        }

        [DataMember, Column("parent_id", Keys = DBColumnKeys.Group), Index("rposition_parent_id"), Browsable(false)]
        public int? ParentId
        {
            get { return GetGroupValue<int?>(); }
            set { SetGroupValue(value); }
        }

        [Reference(nameof(ParentId))]
        public Position Parent
        {
            get { return GetGroupReference<Position>(); }
            set { SetGroupReference(value); }
        }

        [DataMember, Column("code", 40, Keys = DBColumnKeys.Code | DBColumnKeys.Indexing)]
        [Index("rposition_code", true)]
        public string Code
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { SetValue(value, Table.CodeKey); }
        }

        [DataMember, Column("name", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public string Name
        {
            get { return GetName(); }
            set { SetName(value); }
        }

        public string NameRU
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public string NameEN
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        [ControllerMethod]
        public IEnumerable<User> GetUsers()
        {
            return GetReferencing<User>(nameof(User.PositionId), DBLoadParam.None);
        }
    }
}
