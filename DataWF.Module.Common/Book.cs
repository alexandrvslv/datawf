/*
 Account.cs
 
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
using System.ComponentModel;
using DataWF.Common;
using System.Runtime.Serialization;

namespace DataWF.Module.Common
{
    [DataContract, Table("rbook", "Reference Book", BlockSize = 100)]
    public class Book : DBGroupItem
    {
        public static DBTable<Book> DBTable
        {
            get { return GetTable<Book>(); }
        }

        public Book()
        {
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [DataMember, Column("code", 40, Keys = DBColumnKeys.Code)]
        public string Code
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { SetValue(value, Table.CodeKey); }
        }

        [DataMember, Column("group_id", Keys = DBColumnKeys.Group)]
        public int? ParentId
        {
            get { return GetGroupValue<int?>(); }
            set { SetGroupValue(value); }
        }

        [Reference(nameof(ParentId))]
        public Book Parent
        {
            get { return GetGroupReference<Book>(); }
            set { SetGroupReference(value); }
        }

        [DataMember, Column("name", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public string Name
        {
            get { return GetName(); }
            set { SetName(value); }
        }

        [DataMember, Column("book_value")]
        public string Value
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }
    }
}
