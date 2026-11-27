using DataWF;
using DataWF.Data;
using System;
using System.IO;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    [Table("rtemplate_file", "Template", BlockSize = 100), Invoker]
    public partial class TemplateFile : DBItem
    {
        [Column("unid", Keys = ColumnFlags.Primary)]
        public int Id
        {
            get => GetValue(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Column("template_file", Keys = ColumnFlags.File)]
        public byte[] Data
        {
            get => GetValue(Table.DataKey);
            set => SetValue(value, Table.DataKey);
        }

        [Column("template_file_name", 1024, Keys = ColumnFlags.FileName | ColumnFlags.View | ColumnFlags.Code)]
        public string DataName
        {
            get => GetValue(Table.DataNameKey);
            set => SetValue(value, Table.DataNameKey);
        }

        [Column("template_last_write", Keys = ColumnFlags.FileLastWrite)]
        public DateTime? DataLastWrite
        {
            get => GetValue(Table.DataLastWriteKey) ?? Stamp;
            set => SetValue(value, Table.DataLastWriteKey);
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
