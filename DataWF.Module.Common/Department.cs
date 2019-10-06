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
using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Counterpart;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Common
{

    [DataContract, Table("rdepartment", "User", BlockSize = 100)]
    public class Department : DBGroupItem, IComparable, IDisposable
    {
        private static DBColumn nameENKey = DBColumn.EmptyKey;
        private static DBColumn nameRUKey = DBColumn.EmptyKey;
        private static DBColumn companyKey = DBColumn.EmptyKey;
        private static DBTable<Department> dbTable;
        private Company company;

        public static DBColumn NameENKey => DBTable.ParseProperty(nameof(NameEN), ref nameENKey);
        public static DBColumn NameRUKey => DBTable.ParseProperty(nameof(NameRU), ref nameRUKey);
        public static DBColumn CompanyKey => DBTable.ParseProperty(nameof(Company), ref companyKey);
        public static DBTable<Department> DBTable => dbTable ?? (dbTable = GetTable<Department>());

        public Department()
        { }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [DataMember, Column("company_id"), Browsable(false)]
        public int? CompanyId
        {
            get { return GetValue<int?>(CompanyKey); }
            set { SetValue(value, CompanyKey); }
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get { return GetReference(CompanyKey, ref company); }
            set { SetReference(company = value, CompanyKey); }
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
            set { SetValue(value, Table.CodeKey); }
        }

        [DataMember, Column("name", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public string Name
        {
            get { return GetName(); }
            set { SetName(value); }
        }

        public string NameEN
        {
            get { return GetValue<string>(NameENKey); }
            set { SetValue(value, NameENKey); }
        }

        public string NameRU
        {
            get { return GetValue<string>(NameRUKey); }
            set { SetValue(value, NameRUKey); }
        }

        public override AccessValue Access
        {
            get => base.Access != Table.Access ? base.Access
                  : Parent?.Access ?? Table.Access;
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
