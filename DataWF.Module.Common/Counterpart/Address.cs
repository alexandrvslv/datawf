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

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue<int>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Browsable(false)]
        [Column("location_id", Keys = DBColumnKeys.View), Index("daddress_location_id")]
        public int? LocationId
        {
            get => GetValue<int?>(Table.LocationIdKey);
            set => SetValue(value, Table.LocationIdKey);
        }

        [Reference(nameof(LocationId))]
        public Location Location
        {
            get => GetReference(Table.LocationIdKey, ref location);
            set
            {
                if (value?.LocationType != LocationType.Region
                    && value?.LocationType != LocationType.City)
                    throw new ArgumentException("Location type mast be Region or Citi or Village");
                SetReference(location = value, Table.LocationIdKey);
            }
        }

        [Column("post_index", 20, Keys = DBColumnKeys.View), Index("daddress_post_index")]
        public string PostIndex
        {
            get => GetValue<string>(Table.PostIndexKey);
            set => SetValue(value, Table.PostIndexKey);
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
            get => GetValue<string>(Table.StreetENKey);
            set => SetValue(value, Table.StreetENKey);
        }

        [CultureKey(nameof(Street), CultureName = "ru_RU")]
        public string StreetRU
        {
            get => GetValue<string>(Table.StreetRUKey);
            set => SetValue(value, Table.StreetRUKey);
        }

        [Column("floor")]
        public string Floor
        {
            get => GetValue<string>(Table.FloorKey);
            set => SetValue(value, Table.FloorKey);
        }

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue<int?>(Table.ExternalIdKey);
            set => SetValue(value, Table.ExternalIdKey);
        }
    }
}
