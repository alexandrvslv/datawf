/*
 Location.cs
 
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

namespace DataWF.Module.Counterpart
{
    [VirtualTable("rcurrency", typeof(Location), "typeid = 3")]
    public class Currency : DBVirtualItem
    {
        public static DBVirtualTable<Currency> DBTable
        {
            get { return (DBVirtualTable<Currency>)GetTable<Currency>(); }
        }

        [VirtualColumn("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { this[Table.PrimaryKey] = value; }
        }

        [VirtualColumn("code", Keys = DBColumnKeys.View | DBColumnKeys.Code)]
        public string Code
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { this[Table.CodeKey] = value; }
        }

        [VirtualColumn("codei")]
        public string CodeI
        {
            get { return GetProperty<string>(nameof(CodeI)); }
            set { SetProperty(value, nameof(CodeI)); }
        }

        [Browsable(false)]
        [VirtualColumn("parent_id")]
        public int? CountryId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(CountryId))]
        public Country Country
        {
            get { return GetPropertyReference<Country>(); }
            set { SetPropertyReference(value); }
        }

        [VirtualColumn("name", Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public string Name
        {
            get { return GetName(); }
            set { SetName(value); }
        }

    }
}
