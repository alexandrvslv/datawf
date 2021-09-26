using DataWF.Common;
using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{  

    [Table("rtemplate_data", "Template", BlockSize = 100), InvokerGenerator]
    public partial class TemplateData : TemplateItem
    {
        private TemplateFile templateFile;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Index("rtemplate_data_index", true)]
        public override int? TemplateId { get => base.TemplateId; set => base.TemplateId = value; }

        [Browsable(false)]
        [Column("file_id", Keys = DBColumnKeys.View), Index("rtemplate_data_index", true)]
        public int? FileId
        {
            get => GetValue(Table.FileIdKey);
            set => SetValue(value, Table.FileIdKey);
        }

        [Reference(nameof(FileId))]
        public TemplateFile File
        {
            get => GetReference(Table.FileIdKey, ref templateFile);
            set => SetReference(templateFile = value, Table.FileIdKey);
        }

        [Column("auto_generate")]
        public bool? AutoGenerate
        {
            get => GetValue<bool?>(Table.AutoGenerateKey);
            set => SetValue(value, Table.AutoGenerateKey);
        }
    }
}
