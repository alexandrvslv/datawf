using DataWF.Data;
using System.Linq;

namespace DataWF.Module.Flow
{
    [Table("rtemplate_reference", "Template")]
    public class TemplateReference : DBItem
    {
        public static readonly DBTable<TemplateReference> DBTable = GetTable<TemplateReference>();
        public static readonly DBColumn TemplateKey = DBTable.ParseProperty(nameof(TemplateId));
        public static readonly DBColumn ReferenceKey = DBTable.ParseProperty(nameof(ReferenceId));

        private Template template;
        private Template reference;

        public TemplateReference()
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Column("template_id"), Index("rtemplate_reference_index", true)]
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

        [Column("reference_id"), Index("rtemplate_reference_index", true)]
        public int? ReferenceId
        {
            get => GetValue<int?>(ReferenceKey);
            set => SetValue(value, ReferenceKey);
        }

        [Reference(nameof(ReferenceId))]
        public Template Reference
        {
            get => GetReference(ReferenceKey, ref reference);
            set => SetReference(reference = value, ReferenceKey);
        }


    }
}
