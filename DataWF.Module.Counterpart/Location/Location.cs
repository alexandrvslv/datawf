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
        public LocationList(LocationTable table) : base(table)
        { }
    }

    public partial class LocationTable : DBTable<Location>
    {
    }

    [Table("rlocation", "Address", BlockSize = 100, Type = typeof(LocationTable))]
    public class Location : DBGroupItem
    {
        public Location()
        {
        }

        public LocationTable LocationTable => (LocationTable)Table;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(LocationTable.IdKey);
            set => SetValue(value, LocationTable.IdKey);
        }

        public LocationType LocationType
        {
            get { return ItemType == 0 ? LocationType.Default : (LocationType)ItemType; }
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
            get => GetValue<string>(LocationTable.CodeIKey);
            set => SetValue(value, LocationTable.CodeIKey);
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

        [CultureKey(Property = nameof(Name), CultureName = "ru_RU")]
        public string NameRU
        {
            get => GetValue<string>(LocationTable.NameRUKey);
            set => SetValue(value, LocationTable.NameRUKey);
        }

        [CultureKey(Property = nameof(Name), CultureName = "en_US")]
        public string NameEN
        {
            get => GetValue<string>(LocationTable.NameENKey);
            set => SetValue(value, LocationTable.NameENKey);
        }

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue<int?>(LocationTable.ExternalKey);
            set => SetValue(value, LocationTable.ExternalKey);
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
