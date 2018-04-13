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
    [VirtualTable("wf_customer", "rcountry", typeof(Location), "typeid = 2")]
    public class Country : DBVirtualItem
    {
        public static DBVirtualTable<Country> DBTable
        {
            get { return (DBVirtualTable<Country>)DBService.GetTable<Country>(); }
        }

        [VirtualColumn("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [VirtualColumn("code", Keys = DBColumnKeys.Code)]
        public string Code
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { SetValue(value, Table.CodeKey); }
        }

        [VirtualColumn("codei")]
        public string CodeI
        {
            get { return GetProperty<string>(nameof(CodeI)); }
            set { SetProperty(value, nameof(CodeI)); }
        }

        [Browsable(false)]
        [VirtualColumn("parent_id")]
        public int? ContinentId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference("fk_rcountry_continent", nameof(ContinentId))]
        public Location Continent
        {
            get { return GetPropertyReference<Location>(nameof(ContinentId)); }
            set { SetPropertyReference(value, nameof(ContinentId)); }
        }

        [VirtualColumn("name", Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public override string Name
        {
            get { return GetName(nameof(Name)); }
            set { SetName(nameof(Name), value); }
        }

    }
}
