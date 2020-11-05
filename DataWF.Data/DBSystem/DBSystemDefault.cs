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
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public class DBSystemDefault : DBSystem
    {
        public override IDbConnection CreateConnection(DBConnection connection)
        {
            throw new NotImplementedException();
        }

        public override Task<object> ExecuteQueryAsync(IDbCommand command, DBExecuteType type, CommandBehavior behavior)
        {
            throw new NotImplementedException();
        }

        public override void Format(StringBuilder ddl, DBSequence sequence, DDLType ddlType)
        {
            throw new NotImplementedException();
        }

        public override void FormatInsertSequence(StringBuilder command, DBTable table, DBItem row)
        {
            throw new NotImplementedException();
        }

        public override string GetConnectionString(DBConnection connection)
        {
            throw new NotImplementedException();
        }

        public override DbConnectionStringBuilder GetConnectionStringBuilder(DBConnection connection)
        {
            throw new NotImplementedException();
        }

        public override DbProviderFactory GetFactory()
        {
            throw new NotImplementedException();
        }

        public override Task<bool> ReadAsync(IDataReader reader)
        {
            throw new NotImplementedException();
        }

        public override string SequenceCurrentValue(DBSequence sequence)
        {
            throw new NotImplementedException();
        }

        public override string SequenceInline(DBSequence sequence)
        {
            throw new NotImplementedException();
        }

        public override string SequenceNextValue(DBSequence sequence)
        {
            throw new NotImplementedException();
        }

        public override Task<Stream> GetBLOB(long oid, DBTransaction transaction, int bufferSize = 81920)
        {
            throw new NotImplementedException();
        }

        public override Task DeleteBLOB(long oid, DBTransaction transaction)
        {
            throw new NotImplementedException();
        }

        public override Task<long> SetBLOB(Stream value, DBTransaction transaction)
        {
            throw new NotImplementedException();
        }

        public override Stream GetStream(IDataReader reader, int column)
        {
            throw new NotImplementedException();
        }
    }
}
