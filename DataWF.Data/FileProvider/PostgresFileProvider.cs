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
using Npgsql;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public class PostgresFileProvider : IFileProvider
    {
        public static readonly PostgresFileProvider Instance = new PostgresFileProvider();

        public async Task<long> AddFile(Stream value, DBTransaction transaction, int bufferSize = 81920)
        {
            var id = await GetNextId(transaction);
            await AddFileBuffered((uint)id, value, transaction, bufferSize);
            return id;
        }

        public Task AddFile(long id, Stream value, DBTransaction transaction, int bufferSize = 81920)
        {
            return AddFileBuffered((uint)id, value, transaction, bufferSize);
        }

        public async Task AddFileBuffered(uint id, Stream value, DBTransaction transaction, int bufferSize = 81920)
        {
            if (value.CanSeek)
            {
                value.Position = 0;
            }
            var read = 0;
            var buffer = new byte[bufferSize];
            var tempFileName = Helper.GetDocumentsFullPath(Path.GetRandomFileName(), "Temp");
            try
            {
                using (var tempStream = new FileStream(tempFileName, FileMode.Create, FileAccess.ReadWrite))
                {
                    while ((read = await value.ReadAsync(buffer, 0, bufferSize)) != 0)
                    {
                        await tempStream.WriteAsync(buffer, 0, read);
                    }
                }

                var manager = new NpgsqlLargeObjectManager((NpgsqlConnection)transaction.Connection);

                await manager.ImportRemoteAsync(tempFileName, id, CancellationToken.None);
            }
            finally
            {
                File.Delete(tempFileName);
            }
        }

        public async Task<uint> AddFileDirect(uint id, Stream value, DBTransaction transaction, int bufferSize = 81920)
        {
            if (value.CanSeek)
            {
                value.Position = 0;
            }
            var oid = (uint)id;
            var manager = new NpgsqlLargeObjectManager((NpgsqlConnection)transaction.Connection);
            var buffer = new byte[bufferSize];

            await manager.CreateAsync(id, CancellationToken.None);

            using (var lobStream = await manager.OpenReadWriteAsync(oid, CancellationToken.None))
            {
                //await value.CopyToAsync(lobStream);
                int read;
                while ((read = await value.ReadAsync(buffer, 0, bufferSize)) != 0)
                {
                    await lobStream.WriteAsync(buffer, 0, read);
                }
            }
            return oid;
        }

        public async Task<bool> DeleteFile(long id, DBTransaction transaction)
        {
            var oid = (uint)id;
            var manager = new NpgsqlLargeObjectManager((NpgsqlConnection)transaction.Connection);
            try
            {
                await manager.UnlinkAsync(oid, CancellationToken.None);
                return true;
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
                return false;
            }
        }

        public Task<Stream> GetFile(long id, DBTransaction transaction, int bufferSize = 81920)
        {
            return GetFileDirect((uint)id, transaction);
        }

        public async Task<Stream> GetFileDirect(uint oid, DBTransaction transaction)
        {
            var manager = new NpgsqlLargeObjectManager((NpgsqlConnection)transaction.Connection);
            return await manager.OpenReadAsync(oid, CancellationToken.None);
        }

        public async Task<Stream> GetFileBuffered(uint oid, DBTransaction transaction)
        {
            var outStream = new MemoryStream();
            var manager = new NpgsqlLargeObjectManager((NpgsqlConnection)transaction.Connection);
            var bufferSize = 81920;
            var buffer = new byte[bufferSize];
            using (var lobStream = await manager.OpenReadAsync(oid, CancellationToken.None))
            {
                int count;
                while ((count = await lobStream.ReadAsync(buffer, 0, bufferSize)) != 0)
                {
                    outStream.Write(buffer, 0, count);
                }
            }
            outStream.Position = 0;
            return outStream;
        }

        public async Task<long> GetNextId(DBTransaction transaction)
        {
            var manager = new NpgsqlLargeObjectManager((NpgsqlConnection)transaction.Connection);
            var id = await manager.CreateAsync(0, CancellationToken.None);
            manager.Unlink(id);
            return (long)id;
        }
    }
}
