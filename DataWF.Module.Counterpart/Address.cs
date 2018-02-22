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

namespace DataWF.Module.Counterpart
{
    public class AddressList : DBTableView<Address>
    {
        public AddressList()
            : base(Address.DBTable)
        { }
    }

    [Table("wf_customer", "daddress", BlockSize = 2000)]
    public class Address : DBItem
    {
        public static DBTable<Address> DBTable
        {
            get { return DBService.GetTable<Address>(); }
        }

        public Address()
        {
            Build(DBTable);
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Browsable(false)]
        [Column("locationid"), Index("daddress_locationid")]
        public int? LocationId
        {
            get { return GetProperty<int?>(nameof(LocationId)); }
            set { SetProperty(value, nameof(LocationId)); }
        }

        [Reference("fk_daddress_locationid", nameof(LocationId))]
        public Location Location
        {
            get { return GetReference<Location>(ParseProperty(nameof(LocationId))); }
            set
            {
                if (value.LocationType != LocationType.Region ||
                    value.LocationType != LocationType.City ||
                    value.LocationType != LocationType.Vilage)
                    throw new ArgumentException("Location type mast be Region or Citi or Village");
                SetPropertyReference(value, nameof(LocationId));
            }
        }

        [Column("postindex", 20), Index("daddress_postindex")]
        public string PostIndex
        {
            get { return GetProperty<string>(nameof(PostIndex)); }
            set { SetProperty(value, nameof(PostIndex)); }
        }

        [Column("street", 1024, Keys = DBColumnKeys.Culture)]
        public string Street
        {
            get { return GetName("street"); }
            set { SetName("street", value); }
        }
    }
}
