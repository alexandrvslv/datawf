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

using DataWF.Common;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [Table("file_data", "General", Keys = DBTableKeys.NoLogs | DBTableKeys.Private, Type = typeof(FileDataTable)), InvokerGenerator]
    public sealed partial class FileData : DBItem
    {
        public FileData(DBTable table) : base(table)
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public long Id
        {
            get => GetValue<long>(FileDataTable.IdKey);
            set => SetValue(value, FileDataTable.IdKey);
        }

        [Column("file_data", Keys = DBColumnKeys.File)]
        public byte[] Data
        {
            get => GetValue<byte[]>(FileDataTable.DataKey);
            set => SetValue(value, FileDataTable.DataKey);
        }

        [Column("file_size")]
        public int? Size
        {
            get => GetValue<int?>(FileDataTable.SizeKey);
            set => SetValue(value, FileDataTable.SizeKey);
        }

        [Column("file_hash", size: 256)]
        public byte[] Hash
        {
            get => GetValue<byte[]>(FileDataTable.HashKey);
            set => SetValue(value, FileDataTable.HashKey);
        }

        [Column("file_storage")]
        public FileStorage Storage
        {
            get => GetValue<FileStorage>(FileDataTable.StorageKey);
            set => SetValue(value, FileDataTable.StorageKey);
        }

        [Column("file_path", size: 2048)]
        public string Path
        {
            get => GetValue<string>(FileDataTable.PathKey);
            set => SetValue(value, FileDataTable.PathKey);
        }
    }
}
