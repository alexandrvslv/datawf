using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{

    [Table("rtemplate_data", "Template", BlockSize = 100)]
    public class TemplateData : DBItem
    {
        public static readonly DBTable<TemplateData> DBTable = GetTable<TemplateData>();
        public static readonly DBColumn TemplateKey = DBTable.ParseProperty(nameof(TemplateId));
        public static readonly DBColumn FileKey = DBTable.ParseProperty(nameof(FileId));
        public static readonly DBColumn AutoGenerateKey = DBTable.ParseProperty(nameof(AutoGenerate));

        private Template template;
        private TemplateFile templateFile;

        public TemplateData()
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Browsable(false)]
        [Column("template_id"), Index("rtemplate_data_index", true)]
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

        [Browsable(false)]
        [Column("file_id", Keys = DBColumnKeys.View), Index("rtemplate_data_index", true)]
        public int? FileId
        {
            get => GetValue<int?>(FileKey);
            set => SetValue(value, FileKey);
        }

        [Reference(nameof(FileId))]
        public TemplateFile File
        {
            get => GetReference(FileKey, ref templateFile);
            set => SetReference(templateFile = value, FileKey);
        }

        [Column("auto_generate")]
        public bool? AutoGenerate
        {
            get => GetValue<bool?>(AutoGenerateKey);
            set => SetValue(value, AutoGenerateKey);
        }
    }
}
