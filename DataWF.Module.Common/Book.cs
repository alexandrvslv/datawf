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
using System.Runtime.Serialization;

namespace DataWF.Module.Common
{
    [DataContract, Table("rbook", "Reference Book", BlockSize = 100)]
    public class Book : DBGroupItem
    {
        private static DBColumn valueKey = DBColumn.EmptyKey;
        private static DBColumn nameENKey = DBColumn.EmptyKey;
        private static DBColumn nameRUKey = DBColumn.EmptyKey;
        private static DBTable<Book> dbTable;

        public static DBColumn ValueKey => DBTable.ParseProperty(nameof(Value), valueKey);
        public static DBColumn NameENKey => DBTable.ParseProperty(nameof(NameEN), nameENKey);
        public static DBColumn NameRUKey => DBTable.ParseProperty(nameof(NameRU), nameRUKey);
        public static DBTable<Book> DBTable => dbTable ?? (dbTable = GetTable<Book>());

        public Book()
        { }

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

        [DataMember, Column("book_value")]
        public string Value
        {
            get { return GetValue<string>(ValueKey); }
            set { SetValue(value, ValueKey); }
        }
    }
}
