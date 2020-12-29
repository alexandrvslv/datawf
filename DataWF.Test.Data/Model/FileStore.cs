using DataWF.Data;
using System;

namespace DataWF.Test.Data
{
    [Table(TestORM.FileTableName, "Files")]
    public sealed class FileStore : DBItem
    {
        public FileStore(DBTable table) : base(table)
        {
        }

        public FileStoreTable FileStoreTable => (FileStoreTable)Table;

        [Column("id", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue<int>(FileStoreTable.IdKey);
            set => SetValue(value, FileStoreTable.IdKey);
        }

        [Column("file_ref", Keys = DBColumnKeys.FileOID)]
        public long? FileRef
        {
            get => GetValue<long?>(FileStoreTable.FileRefKey);
            set => SetValue(value, FileStoreTable.FileRefKey);
        }

        [Column("file_name", 2048, Keys = DBColumnKeys.FileName)]
        public string FileName
        {
            get => GetValue<string>(FileStoreTable.FileNameKey);
            set => SetValue(value, FileStoreTable.FileNameKey);
        }

        [Column("file_last_write", Keys = DBColumnKeys.FileLastWrite | DBColumnKeys.UtcDate)]
        public DateTime? FileLastWrite
        {
            get => GetValue<DateTime?>(FileStoreTable.FileLastWriteKey);
            set => SetValue(value, FileStoreTable.FileLastWriteKey);
        }
    }
}
