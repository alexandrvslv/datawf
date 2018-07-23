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
using System.Runtime.Serialization;
using System.Globalization;
using System.Collections.Generic;

namespace DataWF.Module.Counterpart
{
    public enum LocationType
    {
        Default = 0,
        Continent = 1,
        Country = 2,
        Currency = 3,
        Region = 4,
        City = 5
    }

    public class LocationList : DBTableView<Location>
    {
        public LocationList() : base()
        { }
    }

    [DataContract, Table("rlocation", "Address", BlockSize = 100)]
    public class Location : DBGroupItem
    {
        public static DBTable<Location> DBTable
        {
            get { return GetTable<Location>(); }
        }

        public Location()
        {
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        public LocationType LocationType
        {
            get { return ItemType == null ? LocationType.Default : (LocationType)ItemType; }
        }

        [DataMember, Column("code", 40, Keys = DBColumnKeys.Code | DBColumnKeys.Indexing), Index("rlocation_typeid_code", false)]
        public string Code
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { SetValue(value, Table.CodeKey); }
        }

        [DataMember, Column("codei", 40)]
        [Index("rlocation_codei")]
        public string CodeI
        {
            get { return GetProperty<string>(nameof(CodeI)); }
            set { SetProperty(value, nameof(CodeI)); }
        }

        [Browsable(false)]
        [DataMember, Column("parent_id", Keys = DBColumnKeys.Group), Index("rlocation_parentid")]
        public int? ParentId
        {
            get { return GetGroupValue<int?>(); }
            set { SetGroupValue(value); }
        }

        [Reference(nameof(ParentId))]
        public Location Parent
        {
            get { return GetGroupReference<Location>(); }
            set { SetGroupReference(value); }
        }

        [DataMember, Column("name", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public string Name
        {
            get { return GetName(); }
            set { SetName(value); }
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
