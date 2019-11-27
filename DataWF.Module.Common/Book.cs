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
    [Table("rbook", "Reference Book", BlockSize = 100)]
    public class Book : DBGroupItem
    {
        public static readonly DBTable<Book> DBTable = GetTable<Book>();
        public static readonly DBColumn ValueKey = DBTable.ParseProperty(nameof(Value));
        public static readonly DBColumn NameENKey = DBTable.ParseProperty(nameof(NameEN));
        public static readonly DBColumn NameRUKey = DBTable.ParseProperty(nameof(NameRU));
        public static readonly DBColumn ExternalIdKey = DBTable.ParseProperty(nameof(ExternalId));

        public Book()
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Column("code", 512, Keys = DBColumnKeys.Code)]
        public string Code
        {
            get => GetValue<string>(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
        }

        [Column("group_id", Keys = DBColumnKeys.Group)]
        public int? ParentId
        {
            get => GetGroupValue<int?>();
            set => SetGroupValue(value);
        }

        [Reference(nameof(ParentId))]
        public Book Parent
        {
            get => GetGroupReference<Book>();
            set => SetGroupReference(value);
        }

        [Column("name", 1024, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }

        public string NameEN
        {
            get => GetValue<string>(NameENKey);
            set => SetValue(value, NameENKey);
        }

        public string NameRU
        {
            get => GetValue<string>(NameRUKey);
            set => SetValue(value, NameRUKey);
        }

        [Column("book_value")]
        public string Value
        {
            get => GetValue<string>(ValueKey);
            set => SetValue(value, ValueKey);
        }

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue<int?>(ExternalIdKey);
            set => SetValue(value, ExternalIdKey);
        }
    }
}
