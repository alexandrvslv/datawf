using DataWF.Common;
using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Counterpart
{
    public class LocationList : DBTableView<Location>
    {
        public LocationList(LocationTable<Location> table) : base(table)
        { }
    }

    [Table("rlocation", "Address", BlockSize = 100, Type = typeof(LocationTable<>)), InvokerGenerator]
    public partial class Location : DBGroupItem
    {
        public Location(DBTable table) : base(table)
        {
        }

        public ILocationTable LocationTable => (ILocationTable)Table;

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
            get => GetValue<string>(LocationTable.CodeKey);
            set => SetValue(value, LocationTable.CodeKey);
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

        [CultureKey(nameof(Name), CultureName = "ru_RU")]
        public string NameRU
        {
            get => GetValue<string>(LocationTable.NameRUKey);
            set => SetValue(value, LocationTable.NameRUKey);
        }

        [CultureKey(nameof(Name), CultureName = "en_US")]
        public string NameEN
        {
            get => GetValue<string>(LocationTable.NameENKey);
            set => SetValue(value, LocationTable.NameENKey);
        }

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue<int?>(LocationTable.ExternalIdKey);
            set => SetValue(value, LocationTable.ExternalIdKey);
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
