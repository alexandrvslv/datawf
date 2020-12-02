using DataWF.Data;

namespace DataWF.Module.Flow
{
    [Table("rtemplate_property", "Template")]
    public class TemplateProperty : DBItem
    {
        public static readonly DBTable<TemplateProperty> DBTable = GetTable<TemplateProperty>();
        public static readonly DBColumn TemplateKey = DBTable.ParseProperty(nameof(TemplateId));
        public static readonly DBColumn PropertyNameKey = DBTable.ParseProperty(nameof(PropertyName));

        private Template template;

        public TemplateProperty()
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Column("template_id"), Index("rtemplate_property_index", true)]
        public int? TemplateId
        {
            get => GetValue<int?>(TemplateKey);
            set => SetValue(value, TemplateKey);
        }

        [Reference(nameof(TemplateId))]
        public Template Template
        {
            get => GetReference(TemplateKey, ref template);
            set => SetReference(template = value, TemplateKey);
        }

        [Column("property_name", 1024), Index("rtemplate_property_index", true)]
        public string PropertyName
        {
            get => GetValue<string>(PropertyNameKey);
            set => SetValue(value, PropertyNameKey);
        }

    }
}
