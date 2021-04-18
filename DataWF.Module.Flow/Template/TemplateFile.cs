using DataWF.Common;
using DataWF.Data;
using System;
using System.IO;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    [Table("rtemplate_file", "Template", BlockSize = 100), InvokerGenerator]
    public partial class TemplateFile : DBItem
    {
        public TemplateFile(DBTable table) : base(table)
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue<int>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Column("template_file", Keys = DBColumnKeys.File)]
        public byte[] Data
        {
            get => GetValue<byte[]>(Table.DataKey);
            set => SetValue(value, Table.DataKey);
        }

        [Column("template_file_name", 1024, Keys = DBColumnKeys.FileName | DBColumnKeys.View | DBColumnKeys.Code)]
        public string DataName
        {
            get => GetValue<string>(Table.DataNameKey);
            set => SetValue(value, Table.DataNameKey);
        }

        [Column("template_last_write", Keys = DBColumnKeys.FileLastWrite)]
        public DateTime? DataLastWrite
        {
            get => GetValue<DateTime?>(Table.DataLastWriteKey) ?? Stamp;
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
