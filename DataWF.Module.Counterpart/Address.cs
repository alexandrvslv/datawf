using DataWF.Data;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Counterpart
{
    public class AddressList : DBTableView<Address>
    {
        public AddressList(AddressTable table) : base(table)
        { }
    }

    public partial class AddressTable : DBTable<Address>
    {

    }

    [Table("daddress", "Address", BlockSize = 100)]
    public class Address : DBItem
    {
        private Location location;

        public Address()
        { }

        public AddressTable AddressTable => (AddressTable)Table;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(AddressTable.IdKey);
            set => SetValue(value, AddressTable.IdKey);
        }

        [Browsable(false)]
        [Column("location_id", Keys = DBColumnKeys.View), Index("daddress_location_id")]
        public int? LocationId
        {
            get => GetValue<int?>(AddressTable.LocationKey);
            set => SetValue(value, AddressTable.LocationKey);
        }

        [Reference(nameof(LocationId))]
        public Location Location
        {
            get => GetReference(AddressTable.LocationKey, ref location);
            set
            {
                if (value?.LocationType != LocationType.Region
                    && value?.LocationType != LocationType.City)
                    throw new ArgumentException("Location type mast be Region or Citi or Village");
                SetReference(location = value, AddressTable.LocationKey);
            }
        }

        [Column("post_index", 20, Keys = DBColumnKeys.View), Index("daddress_post_index")]
        public string PostIndex
        {
            get => GetValue<string>(AddressTable.PostIndexKey);
            set => SetValue(value, AddressTable.PostIndexKey);
        }

        [Column("street", 1024, Keys = DBColumnKeys.Culture | DBColumnKeys.View)]
        public string Street
        {
            get => GetName();
            set => SetName(value);
        }

        [CultureKey(Property = nameof(Street), CultureName = "en_US")]
        public string StreetEN
        {
            get => GetValue<string>(AddressTable.StreetENKey);
            set => SetValue(value, AddressTable.StreetENKey);
        }

        [CultureKey(Property = nameof(Street), CultureName = "ru_RU")]
        public string StreetRU
        {
            get => GetValue<string>(AddressTable.StreetRUKey);
            set => SetValue(value, AddressTable.StreetRUKey);
        }

        [Column("floor")]
        public string Floor
        {
            get => GetValue<string>(AddressTable.FloorKey);
            set => SetValue(value, AddressTable.FloorKey);
        }

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue<int?>(AddressTable.ExternalKey);
            set => SetValue(value, AddressTable.ExternalKey);
        }
    }
}
