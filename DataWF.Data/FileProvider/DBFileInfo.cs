//  The MIT License (MIT)
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

using DataWF;
using System;
using System.Data;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [Table("file_info", "General", Keys = DBTableKeys.NoLogs | DBTableKeys.Private)]
    public sealed partial class DBFileInfo : DBItem, IDBFileInfo
    {
        [Column("unid", Keys = ColumnFlags.Primary | ColumnFlags.FileOID)]
        public long Id
        {
            get => GetValue(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Column("file_name", size: 1024, Keys = ColumnFlags.FileName)]
        public string Name
        {
            get => GetValue<string>(Table.NameKey);
            set => SetValue(value, Table.NameKey);
        }

        [Column("file_data", Keys = ColumnFlags.File)]
        public byte[] Data
        {
            get => GetValue<byte[]>(Table.DataKey);
            set => SetValue(value, Table.DataKey);
        }

        [Column("file_size")]
        public int? Size
        {
            get => GetValue(Table.SizeKey);
            set => SetValue(value, Table.SizeKey);
        }

        [Column("file_hash", size: 256)]
        public byte[] HashSha256
        {
            get => GetValue<byte[]>(Table.HashSha256Key);
            set => SetValue(value, Table.HashSha256Key);
        }

        [Column("file_storage")]
        public FileStorage Storage
        {
            get => GetValue(Table.StorageKey);
            set => SetValue(value, Table.StorageKey);
        }

        [Column("file_path", size: 2048)]
        public string Path
        {
            get => GetValue(Table.PathKey);
            set => SetValue(value, Table.PathKey);
        }
    }
}
