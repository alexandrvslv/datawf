using DataWF.Common;
using DataWF.Data;

namespace DataWF.Module.Flow
{
    [Table("rtemplate_property", "Template"), InvokerGenerator]
    public partial class TemplateProperty : TemplateItem
    {

        public TemplateProperty(DBTable table) : base(table)
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Column("property_name", 1024), Index("rtemplate_property_index", true)]
        public string PropertyName
        {
            get => GetValue<string>(Table.PropertyNameKey);
            set => SetValue(value, Table.PropertyNameKey);
        }

    }
}
