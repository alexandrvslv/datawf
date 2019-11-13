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
using System.Runtime.Serialization;

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

    [Table("rlocation", "Address", BlockSize = 100)]
    public class Location : DBGroupItem
    {
        private static DBTable<Location> dbTable;
        private static DBColumn codeIKey = DBColumn.EmptyKey;
        private static DBColumn nameENKey = DBColumn.EmptyKey;
        private static DBColumn nameRUKey = DBColumn.EmptyKey;
        private static DBColumn externalIdKey = DBColumn.EmptyKey;

        public static DBColumn CodeIKey => DBTable.ParseProperty(nameof(CodeI), ref codeIKey);
        public static DBColumn NameENKey => DBTable.ParseProperty(nameof(NameEN), ref nameENKey);
        public static DBColumn NameRUKey => DBTable.ParseProperty(nameof(NameRU), ref nameRUKey);
        public static DBColumn ExternalIdKey => DBTable.ParseProperty(nameof(ExternalId), ref externalIdKey);
        public static DBTable<Location> DBTable => dbTable ?? (dbTable = GetTable<Location>());

        public Location()
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        public LocationType LocationType
        {
            get { return ItemType == null ? LocationType.Default : (LocationType)ItemType; }
        }

        [Column("code", 40, Keys = DBColumnKeys.Code | DBColumnKeys.Indexing), Index("rlocation_typeid_code", false)]
        public string Code
        {
            get => GetValue<string>(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
        }

        [Column("codei", 40)]
        [Index("rlocation_codei")]
        public string CodeI
        {
            get => GetValue<string>(CodeIKey);
            set => SetValue(value, CodeIKey);
        }

        [Browsable(false)]
        [Column("parent_id", Keys = DBColumnKeys.Group), Index("rlocation_parentid")]
        public int? ParentId
        {
            get => GetGroupValue<int?>();
            set => SetGroupValue(value);
        }

        [Reference(nameof(ParentId))]
        public Location Parent
        {
            get => GetGroupReference<Location>();
            set => SetGroupReference(value);
        }

        [Column("name", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }

        public string NameRU
        {
            get => GetValue<string>(NameRUKey);
            set => SetValue(value, NameRUKey);
        }

        public string NameEN
        {
            get => GetValue<string>(NameENKey);
            set => SetValue(value, NameENKey);
        }
        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue<int?>(ExternalIdKey);
            set => SetValue(value, ExternalIdKey);
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
