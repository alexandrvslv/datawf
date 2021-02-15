using DataWF.Data;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Counterpart
{
    [Table("daddress", "Address", BlockSize = 100)]
    public sealed partial class Address : DBItem
    {
        private Location location;

        public Address(DBTable table) : base(table)
        { }

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
            get => GetValue<int?>(AddressTable.LocationIdKey);
            set => SetValue(value, AddressTable.LocationIdKey);
        }

        [Reference(nameof(LocationId))]
        public Location Location
        {
            get => GetReference(AddressTable.LocationIdKey, ref location);
            set
            {
                if (value?.LocationType != LocationType.Region
                    && value?.LocationType != LocationType.City)
                    throw new ArgumentException("Location type mast be Region or Citi or Village");
                SetReference(location = value, AddressTable.LocationIdKey);
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

        [CultureKey(nameof(Street), CultureName = "en_US")]
        public string StreetEN
        {
            get => GetValue<string>(AddressTable.StreetENKey);
            set => SetValue(value, AddressTable.StreetENKey);
        }

        [CultureKey(nameof(Street), CultureName = "ru_RU")]
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
            get => GetValue<int?>(AddressTable.ExternalIdKey);
            set => SetValue(value, AddressTable.ExternalIdKey);
        }
    }
}
