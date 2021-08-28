using DataWF.Common;
using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Counterpart
{

    [Table("rlocation", "Address", BlockSize = 100, Type = typeof(LocationTable<>))]
    public partial class Location : DBGroupItem
    {
        public Location(DBTable table) : base(table)
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue<int>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
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
            get => GetValue<string>(Table.CodeIKey);
            set => SetValue(value, Table.CodeIKey);
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
            get => GetValue<string>(Table.NameRUKey);
            set => SetValue(value, Table.NameRUKey);
        }

        [CultureKey(nameof(Name), CultureName = "en_US")]
        public string NameEN
        {
            get => GetValue<string>(Table.NameENKey);
            set => SetValue(value, Table.NameENKey);
        }

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue<int?>(Table.ExternalIdKey);
            set => SetValue(value, Table.ExternalIdKey);
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
