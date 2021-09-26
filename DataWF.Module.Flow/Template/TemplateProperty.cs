using DataWF.Common;
using DataWF.Data;

namespace DataWF.Module.Flow
{
    [Table("rtemplate_property", "Template")]
    public partial class TemplateProperty : TemplateItem
    {

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Index("rtemplate_property_index", true)]
        public override int? TemplateId { get => base.TemplateId; set => base.TemplateId = value; }

        [Column("property_name", 1024), Index("rtemplate_property_index", true)]
        public string PropertyName
        {
            get => GetValue(Table.PropertyNameKey);
            set => SetValue(value, Table.PropertyNameKey);
        }

    }
}
