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
using System.ComponentModel;
using DataWF.Common;
using System;
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
        public static DBTable<Address> DBTable
        {
            get { return GetTable<Address>(); }
        }

        public Address()
        {
            Build(DBTable);
        }

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
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(LocationId))]
        public Location Location
        {
            get { return GetPropertyReference<Location>(); }
            set
            {
                if (value?.LocationType != LocationType.Region
                    && value?.LocationType != LocationType.City)
                    throw new ArgumentException("Location type mast be Region or Citi or Village");
                SetPropertyReference(value);
            }
        }

        [DataMember, Column("post_index", 20, Keys = DBColumnKeys.View), Index("daddress_post_index")]
        public string PostIndex
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        [DataMember, Column("street", 1024, Keys = DBColumnKeys.Culture | DBColumnKeys.View)]
        public string Street
        {
            get { return GetName(); }
            set { SetName(value); }
        }
    }
}
