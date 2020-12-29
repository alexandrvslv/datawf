using DataWF.Common;
using DataWF.Data;
using System;
using System.IO;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    [Table("rtemplate_file", "Template", BlockSize = 100), InvokerGenerator]
    public partial class TemplateFile : TemplateItem
    {
        public TemplateFile(DBTable table) : base(table)
        {
        }

        public TemplateFileTable<TemplateFile> TemplateFileTable => (TemplateFileTable<TemplateFile>)Table;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue<int>(TemplateFileTable.IdKey);
            set => SetValue(value, TemplateFileTable.IdKey);
        }

        [Column("template_file", Keys = DBColumnKeys.File)]
        public byte[] Data
        {
            get => GetValue<byte[]>(TemplateFileTable.DataKey);
            set => SetValue(value, TemplateFileTable.DataKey);
        }

        [Column("template_file_name", 1024, Keys = DBColumnKeys.FileName | DBColumnKeys.View | DBColumnKeys.Code)]
        public string DataName
        {
            get => GetValue<string>(TemplateFileTable.DataNameKey);
            set => SetValue(value, TemplateFileTable.DataNameKey);
        }

        [Column("template_last_write", Keys = DBColumnKeys.FileLastWrite)]
        public DateTime? DataLastWrite
        {
            get => GetValue<DateTime?>(TemplateFileTable.DataLastWriteKey) ?? Stamp;
            set => SetValue(value, TemplateFileTable.DataLastWriteKey);
        }

        public string FileType => Path.GetExtension(DataName);

        public Stream GetMemoryStream(DBTransaction transaction)
        {
            return GetZipMemoryStream(table.FileKey, transaction);
        }

        public FileStream GetFileStream(DBTransaction transaction)
        {
            return GetZipFileStream(table.FileKey, Helper.GetDocumentsFullPath(DataName, nameof(TemplateFile) + Id), transaction);
        }
    }
}
