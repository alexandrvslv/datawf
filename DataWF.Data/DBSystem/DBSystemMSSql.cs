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
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public class DBSystemMSSql : DBSystem
    {
        public DBSystemMSSql()
        {
            ParameterPrefix = "@";
            Name = "MSSql";
            DataTypeMap = new Dictionary<DBDataType, string>(){
                    {DBDataType.String, "nvarchar{0}"},
                    {DBDataType.Clob, "nvarchar(max)"},
                    {DBDataType.DateTime, "datetime"},
                    {DBDataType.ByteArray, "varbinary{0}"},
                    {DBDataType.ByteSerializable, "varbinary{0}"},
                    {DBDataType.Blob, "varbinary(max)"},
                    {DBDataType.LargeObject, "integer"},
                    {DBDataType.BigInt, "bigint"},
                    {DBDataType.Int, "integer"},
                    {DBDataType.ShortInt, "smallint"},
                    {DBDataType.TinyInt, "tinyint"},
                    {DBDataType.Float, "float(24)"},
                    {DBDataType.Double, "float(53)"},
                    {DBDataType.Decimal, "decimal{0}"},
                    {DBDataType.TimeSpan, "bigint"},
                    {DBDataType.Bool, "bit"},
                };
        }

        public override IDbConnection CreateConnection(DBConnection connection)
        {
            return new SqlConnection(GetConnectionString(connection));
        }

        public override string GetConnectionString(DBConnection connection)
        {
            return GetConnectionStringBuilder(connection).ConnectionString;
        }

        public override DbConnectionStringBuilder GetConnectionStringBuilder(DBConnection connection)
        {
            var builder = new SqlConnectionStringBuilder
            {
                ConnectTimeout = connection.TimeOut,
                IntegratedSecurity = connection.IntegratedSecurity,
                DataSource = connection.Host + (connection.Port == 0 ? string.Empty : "," + connection.Port),
                InitialCatalog = connection.DataBase,
                Encrypt = connection.Encrypt
            };
            if (!connection.IntegratedSecurity)
            {
                builder.UserID = connection.User;
                builder.Password = connection.Password;
            }
            if (connection.Pool != null)
            {
                builder.Pooling = connection.Pool.Value;
            }

            return builder;
        }

        public override DbProviderFactory GetFactory()
        {
            return SqlClientFactory.Instance;
        }

        public override string SequenceCurrentValue(DBSequence sequence)
        {
            return $@"select current_value from sys.sequences where name = '{sequence.Name}'";
        }

        public override string SequenceNextValue(DBSequence sequence)
        {
            return $"select next value for {sequence.Name};";
        }

        public override void Format(StringBuilder ddl, DBSequence sequence, DDLType ddlType)
        {
            switch (ddlType)
            {
                case DDLType.Create:
                    ddl.AppendLine($"create sequence {sequence.Name} as {FormatType(sequence.DBDataType, sequence.Size, sequence.Scale)} start with {sequence.Current} increment by {sequence.Increment};");
                    break;
                case DDLType.Alter:
                    ddl.AppendLine($"alter sequence {sequence.Name} restart with {sequence.Current} increment by {sequence.Increment};");
                    break;
                case DDLType.Drop:
                    ddl.AppendLine($"drop sequence {sequence.Name};");
                    break;
            }
        }

        public override void Format(StringBuilder ddl, DBSchema schema, DDLType ddlType)
        {
            if (ddlType == DDLType.Create)
            {
                var dataFile = Path.Combine(schema.Connection.Path, $"{schema.DataBase}.mdf");
                ddl.AppendLine($"create database {schema.DataBase}");
                ddl.AppendLine("on");
                ddl.AppendLine($"(name = {schema.DataBase}_dat,");
                ddl.AppendLine($"filename = '{dataFile}',");
                ddl.AppendLine("size = 10, maxsize = unlimited, filegrowth = 5MB);");
                ddl.AppendLine($"alter database {schema.DataBase} set recovery simple;");
                if (!schema.Sequences.Contains("db_lob_seq"))
                {
                    var lobSequence = new DBSequence("db_lob_seq");
                    schema.Sequences.Add(lobSequence);
                    Format(ddl, lobSequence, DDLType.Create);
                }
                ddl.AppendLine($"create table [db_lob]([oid] bigint not null primary key, [lob_data] varbinary(max));");
            }
            else if (ddlType == DDLType.Drop)
            {
                //ddl.AppendLine($"alter database {schema.DataBase} remove file {schema.DataBase}_dat;");
                ddl.AppendLine($"drop database {schema.DataBase};");
            }
        }

        public override void FormatCreate(StringBuilder ddl, DBColumn column)
        {
            ddl.AppendLine($"alter table {column.Table.SqlName} add ");
            Format(ddl, column);
            ddl.AppendLine(";");
        }

        public override string SequenceInline(DBSequence sequence)
        {
            return $"next value for {sequence.Name}";
        }

        public override void FormatInsertSequence(StringBuilder command, DBTable table, DBItem row)
        {
            var idparam = ParameterPrefix + table.PrimaryKey.Name;
            command.AppendLine($"select {idparam} = next value for {table.SequenceName};");
            FormatInsert(command, table, idparam, row); command.AppendLine(";");
            command.AppendLine($"select {idparam};");
        }

        public override object WriteValue(IDbCommand command, IDataParameter parameter, object value, DBColumn column)
        {
            var dbParameter = (SqlParameter)parameter;
            switch (dbParameter.DbType)
            {
                case DbType.StringFixedLength:
                    dbParameter.DbType = DbType.String;
                    dbParameter.Size = column.Size;
                    break;
                case DbType.Binary:
                    if (column.Size > 0)
                        dbParameter.Size = column.Size;
                    break;
                case DbType.Decimal:
                    if (column.Size != 0)
                        dbParameter.Precision = (byte)column.Size;
                    if (column.Scale != 0)
                        dbParameter.Scale = (byte)column.Scale;
                    break;
            }

            if (column.IsPrimaryKey && value == null)
            {
                parameter.Direction = ParameterDirection.Output;
            }
            return base.WriteValue(command, parameter, value, column);
        }

        public override string FormatQColumn(DBColumn column, string tableAlias)
        {
            if (column.ColumnType == DBColumnTypes.Internal
                || column.ColumnType == DBColumnTypes.Expression
                || column.ColumnType == DBColumnTypes.Code)
                return string.Empty;
            else if (column.ColumnType == DBColumnTypes.Query && column.Table.Type != DBTableType.View)
                return base.FormatQColumn(column, tableAlias);
            else
                return $"{tableAlias}{(tableAlias != null ? "." : string.Empty)}[{column.SqlName}]";
        }

        public override string FormatQTable(DBTable table, string alias)
        {
            var schema = table.Schema?.Connection?.Schema;
            if (!string.IsNullOrEmpty(schema))
            {
                return $"[{schema}].[{table.SqlName}] {alias}";
            }
            return $"[{table.SqlName}] {alias}";
        }

        public override async Task DeleteLOB(uint oid, DBTransaction transaction)
        {
            var command = (SqlCommand)transaction.AddCommand($"delete from db_lob where oid = @oid");
            command.Parameters.AddWithValue($"@oid", (long)oid);
            await transaction.ExecuteQueryAsync(command);
        }

        public override async Task<Stream> GetLOB(uint oid, DBTransaction transaction, int bufferSize = 81920)
        {
            var command = (SqlCommand)transaction.AddCommand($"select oid, lob_data from db_lob where oid = @oid");
            command.Parameters.AddWithValue($"@oid", (long)oid);
            transaction.Reader = (IDataReader)await transaction.ExecuteQueryAsync(command, DBExecuteType.Reader, CommandBehavior.SequentialAccess);
            if (await transaction.ReadAsync())
            {
                return ((SqlDataReader)transaction.Reader).GetStream(1);
            }
            throw new Exception("No Data Found!");
        }

        public override async Task<uint> SetLOB(Stream value, DBTransaction transaction)
        {
            var command = (SqlCommand)transaction.AddCommand(@"select @oid = next value for db_lob_seq;
insert into db_lob (oid, lob_data) values (@oid, @lob_data);
select @oid;");
            command.Parameters.Add("@lob_data", SqlDbType.Binary, -1).Value = value;
            var oid = (long)await transaction.ExecuteQueryAsync(command, DBExecuteType.Scalar);

            //command = (SqlCommand)transaction.AddCommand("select current_value from sys.sequences where name = 'db_lob_seq'");
            //transaction.ExecuteQuery(command, DBExecuteType.Scalar);

            return (uint)oid;
        }

        public override async Task<object> ExecuteQueryAsync(IDbCommand command, DBExecuteType type, CommandBehavior behavior)
        {
            var sqlCommand = (SqlCommand)command;
            switch (type)
            {
                case DBExecuteType.Scalar:
                    return await sqlCommand.ExecuteScalarAsync();
                case DBExecuteType.Reader:
                    return await sqlCommand.ExecuteReaderAsync(behavior);
                case DBExecuteType.NoReader:
                    return await sqlCommand.ExecuteNonQueryAsync();
            }
            return null;
        }

        public override Task<bool> ReadAsync(IDataReader reader)
        {
            var sqlReader = (SqlDataReader)reader;
            return sqlReader.ReadAsync();
        }

        public override uint GetOID(IDataReader reader, int index)
        {
            return ((SqlDataReader)reader).GetFieldValue<uint>(index);
        }

        public override TimeSpan GetTimeSpan(IDataReader reader, int index)
        {
            return ((SqlDataReader)reader).GetTimeSpan(index);
        }
    }
}
