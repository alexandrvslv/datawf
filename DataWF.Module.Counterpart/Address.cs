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

    [DataContract, Table("daddress", "Address", BlockSize = 100)]
    public class Address : DBItem
    {
        private static DBColumn locationKey = DBColumn.EmptyKey;
        private static DBColumn postIndexKey = DBColumn.EmptyKey;
        private static DBTable<Address> dbTable;

        public static DBColumn LocationKey => DBTable.ParseProperty(nameof(LocationId), locationKey);
        public static DBColumn PostIndexKey => DBTable.ParseProperty(nameof(PostIndex), postIndexKey);
        public static DBTable<Address> DBTable => dbTable ?? (dbTable = GetTable<Address>());

        public Address()
        { }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Browsable(false)]
        [DataMember, Column("location_id", Keys = DBColumnKeys.View), Index("daddress_location_id")]
        public int? LocationId
        {
            get { return GetValue<int?>(LocationKey); }
            set { SetValue(value, LocationKey); }
        }

        [Reference(nameof(LocationId))]
        public Location Location
        {
            get { return GetReference<Location>(LocationKey); }
            set
            {
                if (value?.LocationType != LocationType.Region
                    && value?.LocationType != LocationType.City)
                    throw new ArgumentException("Location type mast be Region or Citi or Village");
                SetReference(value, LocationKey);
            }
        }

        [DataMember, Column("post_index", 20, Keys = DBColumnKeys.View), Index("daddress_post_index")]
        public string PostIndex
        {
            get { return GetValue<string>(PostIndexKey); }
            set { SetValue(value, PostIndexKey); }
        }

        [DataMember, Column("street", 1024, Keys = DBColumnKeys.Culture | DBColumnKeys.View)]
        public string Street
        {
            get { return GetName(); }
            set { SetName(value); }
        }
    }
}
