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
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public class DBSystemMySql : DBSystem
    {
        public DBSystemMySql()
        {
            ParameterPrefix = "@";
            Name = "MySql";
            DataTypeMap = new Dictionary<DBDataType, string>(){
                    {DBDataType.String, "varchar{0}"},
                    {DBDataType.Clob, "text"},
                    {DBDataType.DateTime, "datetime(6)"},
                    {DBDataType.ByteArray, "varbinary{0}"},
                    {DBDataType.ByteSerializable, "varbinary{0}"},
                    {DBDataType.Blob, "longblob"},
                    {DBDataType.BigInt, "bigint"},
                    {DBDataType.Int, "integer"},
                    {DBDataType.ShortInt, "smallint"},
                    {DBDataType.TinyInt, "tinyint unsigned"},
                    {DBDataType.Float, "real"},
                    {DBDataType.Double, "double precision"},
                    {DBDataType.Decimal, "numeric{0}"},
                    {DBDataType.TimeSpan, "time"},
                    {DBDataType.Bool, "bool"},
                };
        }

        public override IDbConnection CreateConnection(DBConnection connection)
        {
            return new MySqlConnection(GetConnectionString(connection));
        }

        public override string GetConnectionString(DBConnection connection)
        {
            return GetConnectionStringBuilder(connection).ConnectionString;
        }

        public override DbConnectionStringBuilder GetConnectionStringBuilder(DBConnection connection)
        {
            return new MySqlConnectionStringBuilder
            {
                ConnectionTimeout = (uint)connection.TimeOut,
                UserID = connection.User,
                Password = connection.Password,
                Server = connection.Host,
                Database = connection.DataBase,
                Port = connection.Port,
                SslMode = MySqlSslMode.None
            };
        }

        public override DbProviderFactory GetFactory()
        {
            return MySqlConnectorFactory.Instance;
        }

        public override string SequenceCurrentValue(DBSequence sequence)
        {
            return $"select seq from db_sequence where name = '{sequence.Name}'";
        }

        public override string SequenceNextValue(DBSequence sequence)
        {
            return $@"update db_sequence set seq = seq + {sequence.Increment} where name = '{sequence.Name}';
select seq from db_sequence where name = '{sequence.Name}';";
        }

        public override void Format(StringBuilder ddl, DBSequence sequence, DDLType ddlType)
        {
            switch (ddlType)
            {
                case DDLType.Create:
                    ddl.AppendLine($"insert into db_sequence(name, seq) values('{sequence.Name}', {sequence.StartWith - 1});");
                    break;
                case DDLType.Alter:
                    ddl.AppendLine($"update db_sequence set seq = {sequence.Current} where name = '{sequence.Name}';");
                    break;
                case DDLType.Drop:
                    ddl.AppendLine($"delete from db_sequence where name = '{sequence.Name}';");
                    break;
            }
        }

        public override string SequenceInline(DBSequence sequence)
        {
            throw new System.NotSupportedException();
        }

        public override void FormatInsertSequence(StringBuilder command, DBTable table, DBItem row)
        {
            command.AppendLine($"update db_sequence set seq = seq + {table.Sequence.Increment} where name = '{table.Sequence.Name}';");
            var idparam = $"({SequenceCurrentValue(table.Sequence)})";
            FormatInsert(command, table, idparam, row); command.AppendLine(";");
            command.AppendLine(SequenceCurrentValue(table.Sequence));
        }

        public override void FormatAlter(StringBuilder ddl, DBColumn column)
        {
            ddl.Append($"alter table {column.Table.SqlName} alter ");
            Format(ddl, column);
        }

        public override void Format(StringBuilder ddl, DBSchema schema, DDLType ddlType)
        {
            base.Format(ddl, schema, ddlType);
            if (ddlType == DDLType.Create)
            {
                ddl.AppendLine($"use {schema.DataBase};");
                ddl.AppendLine($"create table db_sequence(name varchar(512) not null primary key, seq long);");
            }
        }

        public override object FillParameter(IDbCommand command, IDataParameter parameter, object value, DBColumn column)
        {
            if (column != null)
            {
                switch (column.DBDataType)
                {
                    case DBDataType.Float:
                        ((MySqlParameter)parameter).MySqlDbType = MySqlDbType.Float;
                        break;
                    case DBDataType.Double:
                        ((MySqlParameter)parameter).MySqlDbType = MySqlDbType.Double;
                        break;
                }
            }
            return base.FillParameter(command, parameter, value, column);
        }

        public override Task<bool> DeleteBlobDatabase(long id, DBTransaction transaction)
        {
            return DeleteBlobTable(id, transaction);
        }

        public override Task<Stream> GetBlobDatabase(long id, DBTransaction transaction, int bufferSize = 81920)
        {
            return GetBlobTable(id, transaction, bufferSize);
        }

        public override Task SetBlobDatabase(long id, Stream value, DBTransaction transaction)
        {
            return SetBlobTable(id, value, transaction);
        }

        public override async Task SetBlobTable(long id, Stream value, DBTransaction transaction)
        {
            var table = (FileDataTable)transaction.Schema.GetTable<FileData>();
            var command = (MySqlCommand)transaction.AddCommand($@"insert into {table.Name} ({table.IdKey.SqlName}, {table.DataKey.SqlName}) 
values (@{table.IdKey.SqlName}, @{table.DataKey.SqlName});");
            command.Parameters.Add($"@{table.IdKey.SqlName}", MySqlDbType.Int64).Value = id;
            command.Parameters.Add($"@{table.DataKey.SqlName}", MySqlDbType.LongBlob).Value = await Helper.GetBufferedBytesAsync(value);//Double buffering!!!
            await transaction.ExecuteQueryAsync(command);
        }

        public override async Task<object> ExecuteQueryAsync(IDbCommand command, DBExecuteType type, CommandBehavior behavior)
        {
            var mysqlCommand = (MySqlCommand)command;
            switch (type)
            {
                case DBExecuteType.Scalar:
                    return await mysqlCommand.ExecuteScalarAsync();
                case DBExecuteType.Reader:
                    return await mysqlCommand.ExecuteReaderAsync(behavior);
                case DBExecuteType.NoReader:
                    return await mysqlCommand.ExecuteNonQueryAsync();
            }
            return null;
        }

        public override uint GetUInt(DbDataReader reader, int index)
        {
            return ((MySqlDataReader)reader).GetUInt32(index);
        }

        public override TimeSpan GetTimeSpan(DbDataReader reader, int index)
        {
            return ((MySqlDataReader)reader).GetTimeSpan(index);
        }
    }
}
