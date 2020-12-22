﻿//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

using System.Text.Json.Serialization;

namespace DataWF.Data
{
    [Table("file_data", "General", Keys = DBTableKeys.NoLogs | DBTableKeys.Private)]
    public class FileData : DBItem
    {
        public static readonly DBTable<FileData> DBTable = GetTable<FileData>();
        public static readonly DBColumn IdKey = DBTable.ParseProperty(nameof(Id));
        public static readonly DBColumn DataKey = DBTable.ParseProperty(nameof(Data));
        public static readonly DBColumn SizeKey = DBTable.ParseProperty(nameof(Size));
        public static readonly DBColumn HashKey = DBTable.ParseProperty(nameof(Hash));
        public static readonly DBColumn PathKey = DBTable.ParseProperty(nameof(Path));
        public static readonly DBColumn StorageKey = DBTable.ParseProperty(nameof(Storage));

        public FileData()
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public long Id
        {
            get => GetValue<long>(IdKey);
            set => SetValue(value, IdKey);
        }

        [Column("file_data", Keys = DBColumnKeys.File)]
        public byte[] Data
        {
            get => GetValue<byte[]>(DataKey);
            set => SetValue(value, DataKey);
        }

        [Column("file_size")]
        public int? Size
        {
            get => GetValue<int?>(SizeKey);
            set => SetValue(value, SizeKey);
        }

        [Column("file_hash", size: 256)]
        public byte[] Hash
        {
            get => GetValue<byte[]>(HashKey);
            set => SetValue(value, HashKey);
        }

        [Column("file_storage")]
        public FileStorage Storage
        {
            get => GetValue<FileStorage>(StorageKey);
            set => SetValue(value, StorageKey);
        }

        [Column("file_path", size: 2048)]
        public string Path
        {
            get => GetValue<string>(PathKey);
            set => SetValue(value, PathKey);
        }

    }
}
