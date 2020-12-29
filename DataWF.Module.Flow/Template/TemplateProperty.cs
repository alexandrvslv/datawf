using DataWF.Common;
using DataWF.Data;

namespace DataWF.Module.Flow
{
    [Table("rtemplate_property", "Template"), InvokerGenerator]
    public partial class TemplateProperty : TemplateItem
    {

        public TemplateProperty(DBTable table) : base(table)
        { }

        public TemplatePropertyTable<TemplateProperty> TemplatePropertyTable => (TemplatePropertyTable<TemplateProperty>)Table;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(TemplatePropertyTable.IdKey);
            set => SetValue(value, TemplatePropertyTable.IdKey);
        }

        [Column("property_name", 1024), Index("rtemplate_property_index", true)]
        public string PropertyName
        {
            get => GetValue<string>(TemplatePropertyTable.PropertyNameKey);
            set => SetValue(value, TemplatePropertyTable.PropertyNameKey);
        }

    }
}
