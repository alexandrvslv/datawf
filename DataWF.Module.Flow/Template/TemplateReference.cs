using DataWF.Common;
using DataWF.Data;
using System.Linq;

namespace DataWF.Module.Flow
{
    [Table("rtemplate_reference", "Template"), InvokerGenerator]
    public partial class TemplateReference : TemplateItem
    {
        private Template reference;

        public TemplateReference(DBTable table) : base(table)
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue<int>(TemplateReferenceTable.IdKey);
            set => SetValue(value, TemplateReferenceTable.IdKey);
        }

        [Column("reference_id"), Index("rtemplate_reference_index", true)]
        public int? ReferenceId
        {
            get => GetValue<int?>(TemplateReferenceTable.ReferenceIdKey);
            set => SetValue(value, TemplateReferenceTable.ReferenceIdKey);
        }

        [Reference(nameof(ReferenceId))]
        public Template Reference
        {
            get => GetReference(TemplateReferenceTable.ReferenceIdKey, ref reference);
            set => SetReference(reference = value, TemplateReferenceTable.ReferenceIdKey);
        }


    }
}
