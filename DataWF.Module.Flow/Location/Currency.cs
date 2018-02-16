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

namespace DataWF.Module.Flow
{
    [VirtualTable("flow", "rcurrency", typeof(Location), "typeid = 3")]
    public class Currency : DBVirtualItem
    {
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
        [VirtualColumn("parentid", Keys = DBColumnKeys.Group)]
        public int? CountryId
        {
            get { return GetValue<int?>(Table.GroupKey); }
            set { SetValue(value, Table.GroupKey); }
        }

        [Reference("fk_rcurrency_parentid", nameof(CountryId))]
        public Country Country
        {
            get { return GetReference<Country>(Table.GroupKey); }
            set { SetReference(value, Table.GroupKey); }
        }

        [VirtualColumn("name", Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public override string Name
        {
            get { return GetName("name"); }
            set { SetName("name", value); }
        }

    }
}
