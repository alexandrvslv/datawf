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
    [DataContract, Table("wf_common", "rbook", "Reference Book", BlockSize = 100)]
    public class Book : DBItem
    {
        public static DBTable<Book> DBTable
        {
            get { return DBService.GetTable<Book>(); }
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [DataMember, Column("code", 40, Keys = DBColumnKeys.Code | DBColumnKeys.View)]
        public string Code
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { SetValue(value, Table.CodeKey); }
        }

        [DataMember, Column("type_id", Keys = DBColumnKeys.ElementType)]
        public short? TypeId
        {
            get { return GetValue<short?>(Table.ElementTypeKey); }
            set { SetValue(value, Table.ElementTypeKey); }
        }

        [Reference(nameof(TypeId))]
        public BookType Type
        {
            get { return GetReference<BookType>(Table.ElementTypeKey); }
            set { SetReference(value, Table.ElementTypeKey); }
        }

        [DataMember, Column("name", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public override string Name
        {
            get { return GetName("name"); }
            set { SetName("name", value); }
        }
    }
}
