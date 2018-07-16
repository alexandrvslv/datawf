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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Common
{
    [DataContract, Table("rdepartment", "User", BlockSize = 100)]
    public class Department : DBGroupItem, IComparable, IDisposable
    {
        public static DBTable<Department> DBTable
        {
            get { return GetTable<Department>(); }
        }

        public Department()
        {
            Build(DBTable);
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [DataMember, Column("parent_id", Keys = DBColumnKeys.Group), Index("rdepartment_parent_id"), Browsable(false)]
        public int? ParentId
        {
            get { return GetGroupValue<int?>(); }
            set { SetGroupValue(value); }
        }

        [Reference(nameof(ParentId))]
        public Department Parent
        {
            get { return GetGroupReference<Department>(); }
            set { SetGroupReference(value); }
        }

        [DataMember, Column("code", 256, Keys = DBColumnKeys.Code | DBColumnKeys.Indexing), Index("rdepartment_code", false)]
        public string Code
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { this[Table.CodeKey] = value; }
        }

        [DataMember, Column("name", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public override string Name
        {
            get { return GetName(nameof(Name)); }
            set { SetName(nameof(Name), value); }
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
        public IEnumerable<Position> GetPositions()
        {
            return GetReferencing<Position>(nameof(Position.DepartmentId), DBLoadParam.None);
        }

        [ControllerMethod]
        public IEnumerable<User> GetUsers()
        {
            return GetReferencing<User>(nameof(User.DepartmentId), DBLoadParam.None);
        }
    }
}
