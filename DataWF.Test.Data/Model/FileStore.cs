using DataWF.Data;
using System;

namespace DataWF.Test.Data
{
    [Table(TestORM.FileTableName, "Files")]
    public class FileStore : DBItem
    {
        public static DBTable<FileStore> DBTable => GetTable<FileStore>();
        public static readonly DBColumn IdKey = DBTable.ParseProperty(nameof(Id));
        public static readonly DBColumn FileRefKey = DBTable.ParseProperty(nameof(FileRef));
        public static readonly DBColumn FileNameKey = DBTable.ParseProperty(nameof(FileName));
        public static readonly DBColumn FileLastWriteKey = DBTable.ParseProperty(nameof(FileLastWrite));


        [Column("id", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(IdKey);
            set => SetValue(value, IdKey);
        }

        [Column("file_ref", Keys = DBColumnKeys.FileOID)]
        public long? FileRef
        {
            get => GetValue<long?>(FileRefKey);
            set => SetValue(value, FileRefKey);
        }

        [Column("file_name", 2048, Keys = DBColumnKeys.FileName)]
        public string FileName
        {
            get => GetValue<string>(FileNameKey);
            set => SetValue(value, FileNameKey);
        }

        [Column("file_last_write", Keys = DBColumnKeys.FileLastWrite | DBColumnKeys.UtcDate)]
        public DateTime? FileLastWrite
        {
            get => GetValue<DateTime?>(FileLastWriteKey);
            set => SetValue(value, FileLastWriteKey);
        }
    }
}
