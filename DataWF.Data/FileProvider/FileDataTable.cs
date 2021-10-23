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
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public partial class FileDataTable : IFileProvider
    {
        public virtual async Task<Stream> GetFile(long id, DBTransaction transaction, int bufferSize = 80 * 1024)
        {
            var command = transaction.AddCommand($"select {DataKey.SqlName} from {SqlName} where {IdKey.SqlName} = {System.ParameterPrefix}{IdKey.SqlName}");
            System.CreateParameter(command, IdKey, id);
            var reader = await transaction.ExecuteReaderAsync(command, CommandBehavior.SequentialAccess);
            if (await reader.ReadAsync())
            {
                return reader.GetStream(0);
            }
            throw new Exception("No Data Found!");
        }

        public virtual async Task<bool> DeleteFile(long id, DBTransaction transaction)
        {
            var command = transaction.AddCommand($"delete from {SqlName} where {IdKey.SqlName} = {System.ParameterPrefix}{IdKey.SqlName}");
            System.CreateParameter(command, IdKey, id);
            var result = await transaction.ExecuteQueryAsync(command, DBExecuteType.Scalar, CommandBehavior.Default);
            return Convert.ToInt32(result) != 0;
        }

        public async Task<long> AddFile(Stream value, DBTransaction transaction, int bufferSize = 80 * 1024)
        {
            var id = await GetNextId(transaction);
            await AddFile(id, value, transaction, bufferSize);
            return id;
        }

        public virtual async Task AddFile(long id, Stream value, DBTransaction transaction, int bufferSize = 80 * 1024)
        {
            var command = transaction.AddCommand($@"insert into {SqlName} ({IdKey.SqlName}, {DataKey.SqlName}) 
values ({System.ParameterPrefix}{IdKey.SqlName}, {System.ParameterPrefix}{DataKey.SqlName});");
            System.CreateParameter(command, IdKey, id);
            await System.CreateStreamParameter(command, DataKey, value);
            await transaction.ExecuteQueryAsync(command);
        }

        public Task<long> GetNextId(DBTransaction transaction)
        {
            return Sequence.GetNextAsync(transaction);
        }
    }
}
