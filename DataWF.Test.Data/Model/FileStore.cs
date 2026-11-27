using DataWF.Data;
using System;

namespace DataWF.Test.Data
{
    [Table(TestORM.FileTableName, "Files")]
    public sealed partial class FileStore : DBItem
    {
        [Column("id", Keys = ColumnFlags.Primary)]
        public int Id
        {
            get => GetValue<int>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Column("file_ref", Keys = ColumnFlags.FileOID)]
        public long? FileRef
        {
            get => GetValue<long?>(Table.FileRefKey);
            set => SetValue(value, Table.FileRefKey);
        }

        [Column("file_name", 2048, Keys = ColumnFlags.FileName)]
        public string FileName
        {
            get => GetValue<string>(Table.FileNameKey);
            set => SetValue(value, Table.FileNameKey);
        }

        [Column("file_last_write", Keys = ColumnFlags.FileLastWrite | ColumnFlags.UtcDate)]
        public DateTime? FileLastWrite
        {
            get => GetValue<DateTime?>(Table.FileLastWriteKey);
            set => SetValue(value, Table.FileLastWriteKey);
        }
    }
}
