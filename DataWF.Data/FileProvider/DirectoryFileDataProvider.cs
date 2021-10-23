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

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public class DirectoryFileDataProvider : BasicDirectoryFileProvider
    {
        public DirectoryFileDataProvider(FileDataTable fileTable)
        {
            FileTable = fileTable;
        }

        public FileDataTable FileTable { get; set; }

        public override async Task<Stream> GetFile(long id, DBTransaction transaction, int bufferSize = 80 * 1024)
        {
            var fileHandler = await FileTable.LoadByIdAsync<long>(id, DBLoadParam.Load, transaction);
            var path = fileHandler?.Path ?? transaction.DbConnection.GetFilePath(id);
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true);
        }

        public override async Task<bool> DeleteFile(long id, DBTransaction transaction)
        {
            var fileHandler = await FileTable.LoadByIdAsync<long>(id, DBLoadParam.Load, transaction);
            var path = fileHandler?.Path ?? transaction.DbConnection.GetFilePath(id);
            if (fileHandler != null)
                await fileHandler.Delete(transaction);
            return await DeleteFile(path);
        }

        public override async Task AddFile(long id, Stream value, DBTransaction transaction, int bufferSize = 80 * 1024)
        {
            var path = transaction.DbConnection.GetFilePath(id);
            using (var sha256 = new SHA256Managed())
            {
                var length = 0;
                using (var fileStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite, bufferSize, true))
                {
                    var buffer = new byte[bufferSize];
                    var read = 0;
                    while ((read = await value.ReadAsync(buffer, 0, bufferSize)) > 0)
                    {
                        length += read;
                        await fileStream.WriteAsync(buffer, 0, read);
                        sha256.TransformBlock(buffer, 0, read, null, 0);
                    }
                    sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                }
                var fileHandler = new FileData(FileTable)
                {
                    Id = id,
                    Storage = FileStorage.FileSystem,
                    Path = path,
                    Size = length,
                    Hash = sha256.Hash
                };
                await fileHandler.Save(transaction);
            }
        }

        public override Task<long> GetNextId(DBTransaction transaction)
        {
            return FileTable.GetNextId(transaction);
        }
    }
}
