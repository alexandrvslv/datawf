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
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public class DirectoryFileProvider : DirectoryFileProviderBase
    {

        public DirectoryFileProvider(DBTable<DBFileInfo> fileTable)
        {
            FileTable = fileTable;
        }

        public DBTable<DBFileInfo> FileTable { get; set; }

        public override async Task<Stream> GetFile(long id, string name, DBTransaction transaction, int bufferSize = Helper.DefaultBufferSize)
        {
            var fileHandler = await GetFileInfo(id, transaction);
            var path = fileHandler?.Path ?? transaction.DbConnection.GetFilePath(id);
            return GetFile(path, bufferSize);
        }

        public override async Task<bool> DeleteFile(long id, DBTransaction transaction)
        {
            var fileHandler = (DBFileInfo)await GetFileInfo(id, transaction);
            var path = fileHandler?.Path ?? transaction.DbConnection.GetFilePath(id);
            if (fileHandler != null)
                await fileHandler.Delete(transaction);
            return await DeleteFile(path);
        }

        public override Task AddFile(long id, string name, Stream value, DBTransaction transaction, int bufferSize = Helper.DefaultBufferSize)
        {
            return AddFileInternal(id, name, value, transaction, bufferSize);
        }

        public async Task<DBFileInfo> AddFileInternal(long id, string name, Stream value, DBTransaction transaction, int bufferSize = Helper.DefaultBufferSize)
        {
            var path = transaction.DbConnection.GetFilePath(id);
            using (var sha256 = new SHA256Managed())
            {
                int length = await CopyStream(value, path, sha256, bufferSize);
                var fileHandler = new DBFileInfo()
                {
                    Id = id,
                    Storage = FileStorage.FileSystem,
                    Name = name,
                    Path = path,
                    Size = length,
                    HashSha256 = sha256.Hash
                };
                await fileHandler.Save(transaction);
                return fileHandler;
            }
        }

        public override Task<long> GetNextId(DBTransaction transaction)
        {
            return FileTable.Sequence.GetNextAsync(transaction);
        }

        public override async Task<IDBFileInfo> GetFileInfo(long id, DBTransaction transaction)
        {
            return await FileTable.LoadByIdAsync(id, DBLoadParam.Load, null, transaction);
        }
    }
}
