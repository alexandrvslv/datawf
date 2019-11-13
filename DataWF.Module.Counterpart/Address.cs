/*
 Address.cs
 
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
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Counterpart
{
    public class AddressList : DBTableView<Address>
    {
        public AddressList() : base()
        { }
    }

    [Table("daddress", "Address", BlockSize = 100)]
    public class Address : DBItem
    {
        public static readonly DBTable<Address> DBTable = GetTable<Address>();

        public static readonly DBColumn LocationKey = DBTable.ParseProperty(nameof(LocationId));
        public static readonly DBColumn PostIndexKey = DBTable.ParseProperty(nameof(PostIndex));
        public static readonly DBColumn StreetENKey = DBTable.ParseProperty(nameof(StreetEN));
        public static readonly DBColumn StreetRUKey = DBTable.ParseProperty(nameof(StreetRU));
        public static readonly DBColumn FloorKey = DBTable.ParseProperty(nameof(Floor));
        public static readonly DBColumn ExternalIdKey = DBTable.ParseProperty(nameof(ExternalId));
        private Location location;

        public Address()
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Browsable(false)]
        [Column("location_id", Keys = DBColumnKeys.View), Index("daddress_location_id")]
        public int? LocationId
        {
            get => GetValue<int?>(LocationKey);
            set => SetValue(value, LocationKey);
        }

        [Reference(nameof(LocationId))]
        public Location Location
        {
            get => GetReference(LocationKey, ref location);
            set
            {
                if (value?.LocationType != LocationType.Region
                    && value?.LocationType != LocationType.City)
                    throw new ArgumentException("Location type mast be Region or Citi or Village");
                SetReference(location = value, LocationKey);
            }
        }

        [Column("post_index", 20, Keys = DBColumnKeys.View), Index("daddress_post_index")]
        public string PostIndex
        {
            get => GetValue<string>(PostIndexKey);
            set => SetValue(value, PostIndexKey);
        }

        [Column("street", 1024, Keys = DBColumnKeys.Culture | DBColumnKeys.View)]
        public string Street
        {
            get => GetName();
            set => SetName(value);
        }

        public string StreetEN
        {
            get => GetValue<string>(StreetENKey);
            set => SetValue(value, StreetENKey);
        }

        public string StreetRU
        {
            get => GetValue<string>(StreetRUKey);
            set => SetValue(value, StreetRUKey);
        }

        [Column("floor")]
        public string Floor
        {
            get => GetValue<string>(FloorKey);
            set => SetValue(value, FloorKey);
        }

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue<int?>(ExternalIdKey);
            set => SetValue(value, ExternalIdKey);
        }
    }
}
