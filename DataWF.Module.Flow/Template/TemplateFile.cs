using DataWF.Common;
using DataWF.Data;
using System;
using System.IO;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    [Table("rtemplate_file", "Template", BlockSize = 100)]
    public class TemplateFile : DBItem
    {
        public static readonly DBTable<TemplateFile> DBTable = GetTable<TemplateFile>();
        public static readonly DBColumn DataLastWriteKey = DBTable.ParseProperty(nameof(DataLastWrite));

        public TemplateFile()
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Column("template_file", Keys = DBColumnKeys.File)]
        public byte[] Data
        {
            get => GetValue<byte[]>(Table.FileKey);
            set => SetValue(value, Table.FileKey);
        }

        [Column("template_file_name", 1024, Keys = DBColumnKeys.FileName | DBColumnKeys.View | DBColumnKeys.Code)]
        public string DataName
        {
            get => GetValue<string>(Table.FileNameKey);
            set => SetValue(value, Table.FileNameKey);
        }

        [Column("template_last_write", Keys = DBColumnKeys.FileLastWrite)]
        public DateTime? DataLastWrite
        {
            get => GetValue<DateTime?>(DataLastWriteKey) ?? Stamp;
            set => SetValue(value, DataLastWriteKey);
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
