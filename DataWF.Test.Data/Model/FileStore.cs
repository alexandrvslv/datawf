using DataWF.Data;
using System;

namespace DataWF.Test.Data
{
    [Table(TestORM.FileTableName, "Files")]
    public sealed partial class FileStore : DBItem
    {
        [Column("id", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue<int>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Column("file_ref", Keys = DBColumnKeys.FileOID)]
        public long? FileRef
        {
            get => GetValue<long?>(Table.FileRefKey);
            set => SetValue(value, Table.FileRefKey);
        }

        [Column("file_name", 2048, Keys = DBColumnKeys.FileName)]
        public string FileName
        {
            get => GetValue<string>(Table.FileNameKey);
            set => SetValue(value, Table.FileNameKey);
        }

        [Column("file_last_write", Keys = DBColumnKeys.FileLastWrite | DBColumnKeys.UtcDate)]
        public DateTime? FileLastWrite
        {
            get => GetValue<DateTime?>(Table.FileLastWriteKey);
            set => SetValue(value, Table.FileLastWriteKey);
        }
    }
}
