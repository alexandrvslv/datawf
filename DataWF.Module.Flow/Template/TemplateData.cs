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

        public TemplateData(DBTable table) : base(table)
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(TemplateDataTable.IdKey);
            set => SetValue(value, TemplateDataTable.IdKey);
        }

        [Browsable(false)]
        [Column("file_id", Keys = DBColumnKeys.View), Index("rtemplate_data_index", true)]
        public int? FileId
        {
            get => GetValue<int?>(TemplateDataTable.FileIdKey);
            set => SetValue(value, TemplateDataTable.FileIdKey);
        }

        [Reference(nameof(FileId))]
        public TemplateFile File
        {
            get => GetReference(TemplateDataTable.FileIdKey, ref templateFile);
            set => SetReference(templateFile = value, TemplateDataTable.FileIdKey);
        }

        [Column("auto_generate")]
        public bool? AutoGenerate
        {
            get => GetValue<bool?>(TemplateDataTable.AutoGenerateKey);
            set => SetValue(value, TemplateDataTable.AutoGenerateKey);
        }
    }
}
