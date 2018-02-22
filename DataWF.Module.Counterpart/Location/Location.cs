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

using System;
using DataWF.Data;
using System.ComponentModel;
using DataWF.Common;

namespace DataWF.Module.Counterpart
{
    public enum LocationType
    {
        None = 0,
        Continent = 1,
        Country = 2,
        Currency = 3,
        Region = 4,
        City = 5,
        Vilage = 6
    }

    public class LocationList : DBTableView<Location>
    {
        public LocationList()
            : base(Location.DBTable)
        { }
    }

    [Table("wf_customer", "rlocation", BlockSize = 2000)]
    public class Location : DBItem
    {
        public static DBTable<Location> DBTable
        {
            get { return DBService.GetTable<Location>(); }
        }

        public Location()
        {
            Build(DBTable);
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Column("typeid", Keys = DBColumnKeys.Type), Index("rlocation_typeid")]
        public LocationType? LocationType
        {
            get { return (LocationType?)GetValue<int?>(Table.TypeKey); }
            set { SetValue(value, Table.TypeKey); }
        }

        [Column("code", 40, Keys = DBColumnKeys.Code | DBColumnKeys.View), Index("rlocation_code", true)]
        public string Code
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { SetValue(value, Table.CodeKey); }
        }

        [Column("codei", 40, Keys = DBColumnKeys.View)]
        [Index("rlocation_codei")]
        public string CodeI
        {
            get { return GetProperty<string>(nameof(CodeI)); }
            set { SetProperty(value, nameof(CodeI)); }
        }

        [Browsable(false)]
        [Column("parentid", Keys = DBColumnKeys.Group)]
        [Index("rlocation_parentid")]
        public int? ParentId
        {
            get { return GetValue<int?>(Table.GroupKey); }
            set { SetValue(value, Table.GroupKey); }
        }

        [Reference("fk_rlocation_parentid", nameof(ParentId))]
        public Location Parent
        {
            get { return GetReference<Location>(Table.GroupKey); }
            set { SetReference(value, Table.GroupKey); }
        }

        [Column("name", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public override string Name
        {
            get { return GetName("name"); }
            set { SetName("name", value); }
        }

        public Location GetParent(LocationType parenttype)
        {
            if (LocationType == parenttype)
                return this;
            Location parent = Parent;
            while (parent != null)
            {
                if (parent.LocationType == parenttype)
                    break;
                parent = parent.Parent;
            }
            return parent;
        }
    }
}
