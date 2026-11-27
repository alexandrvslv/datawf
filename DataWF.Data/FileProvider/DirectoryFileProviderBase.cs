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
    public abstract class DirectoryFileProviderBase : IFileProvider
    {

        public static async Task<int> CopyStream(Stream value, string path, HashAlgorithm hash, int bufferSize = Helper.DefaultBufferSize)
        {
            using (var fileStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite, bufferSize, true))
            {
                return await CopyStream(value, fileStream, hash, bufferSize);
            }

        }

        public static async Task<int> CopyStream(Stream value, FileStream fileStream, HashAlgorithm hash, int bufferSize = Helper.DefaultBufferSize)
        {
            var length = 0;

            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var read = 0;
            while ((read = await value.ReadAsync(buffer, 0, bufferSize)) > 0)
            {
                length += read;
                await fileStream.WriteAsync(buffer, 0, read);
                hash.TransformBlock(buffer, 0, read, null, 0);
            }
            hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            ArrayPool<byte>.Shared.Return(buffer);
            fileStream.Position = 0;
            return length;
        }

        public virtual Task<Stream> GetFile(long id, string name, DBTransaction transaction, int bufferSize = Helper.DefaultBufferSize)
        {
            var path = transaction.DbConnection.GetFilePath(id);
            return Task.FromResult<Stream>(GetFile(path, bufferSize));
        }

        protected FileStream GetFile(string path, int bufferSize)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true);
        }

        public virtual async Task<bool> DeleteFile(long id, DBTransaction transaction)
        {
            var path = transaction.DbConnection.GetFilePath(id);
            return await DeleteFile(path);
        }

        protected async Task<bool> DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, 1, FileOptions.DeleteOnClose | FileOptions.Asynchronous))
                    await stream.FlushAsync();
                return true;
            }
            return false;
        }

        public async Task<long> AddFile(string name, Stream value, DBTransaction transaction, int bufferSize = Helper.DefaultBufferSize)
        {
            var id = await GetNextId(transaction);
            await AddFile(id, name, value, transaction, bufferSize);
            return id;
        }


        public virtual async Task AddFile(long id, string name, Stream value, DBTransaction transaction, int bufferSize = Helper.DefaultBufferSize)
        {
            var path = transaction.DbConnection.GetFilePath(id);
            using (var fileStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite, bufferSize, true))
            {
                await value.CopyToAsync(fileStream, bufferSize);
            }
        }

        public abstract Task<long> GetNextId(DBTransaction transaction);

        public abstract Task<IDBFileInfo> GetFileInfo(long id, DBTransaction transaction);
    }
}
