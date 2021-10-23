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
using System.Data.SqlTypes;
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
                    {DBDataType.DateTime, "datetime2"},
                    {DBDataType.ByteArray, "varbinary{0}"},
                    {DBDataType.ByteSerializable, "varbinary{0}"},
                    {DBDataType.Blob, "varbinary(max)"},
                    {DBDataType.BigInt, "bigint"},
                    {DBDataType.Int, "integer"},
                    {DBDataType.ShortInt, "smallint"},
                    {DBDataType.TinyInt, "tinyint"},
                    {DBDataType.Float, "real"},
                    {DBDataType.Double, "float"},
                    {DBDataType.Decimal, "numeric{0}"},
                    {DBDataType.TimeSpan, "time"},
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

        public override void Format(StringBuilder ddl, DBColumn column)
        {
            base.Format(ddl, column);
            if (column.DataType == typeof(DateTime?))
            {
                ddl.Append(" default NULL");
            }
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
                    ddl.AppendLine($"create sequence {sequence.Name} as {FormatType(sequence.DBDataType, sequence.Size, sequence.Scale)} start with {sequence.StartWith - 1} increment by {sequence.Increment};");
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
            var idparam = ParameterPrefix + table.PrimaryKey.SqlName;
            command.AppendLine($"select {idparam} = next value for {table.SequenceName};");
            FormatInsert(command, table, idparam, row); command.AppendLine(";");
            command.AppendLine($"select {idparam};");
        }

        public override object FillParameter(IDbCommand command, IDataParameter parameter, object value, DBColumn column)
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
                    if (column.Size > 0)
                        dbParameter.Precision = (byte)column.Size;
                    if (column.Scale > 0)
                        dbParameter.Scale = (byte)(column.Scale);
                    break;
            }
            if (column.DBDataType == DBDataType.TimeSpan)
            {
                dbParameter.SqlDbType = SqlDbType.Time;
            }
            else if (column.DBDataType == DBDataType.DateTime)
            {
                dbParameter.SqlDbType = SqlDbType.DateTime2;
                //dbParameter.SqlDbType = SqlDbType.DateTime;
                //if (value is DateTime date)
                //{
                //    if (date < System.Data.SqlTypes.SqlDateTime.MinValue.Value)
                //        value = System.Data.SqlTypes.SqlDateTime.MinValue.Value;
                //    else if (date > System.Data.SqlTypes.SqlDateTime.MaxValue.Value)
                //        value = System.Data.SqlTypes.SqlDateTime.MaxValue.Value;
                //}
            }

            if (column.IsPrimaryKey && value == null)
            {
                parameter.Direction = ParameterDirection.Output;
            }
            return base.FillParameter(command, parameter, value, column);
        }

        public override string FormatQColumn(DBColumn column, string tableAlias, string columnAlias = null)
        {
            if (column.ColumnType == DBColumnTypes.Internal
                || column.ColumnType == DBColumnTypes.Expression
                || column.ColumnType == DBColumnTypes.Code)
                return string.Empty;
            else if (column.ColumnType == DBColumnTypes.Query && column.Table.Type != DBTableType.View)
                return base.FormatQColumn(column, tableAlias);
            else
                return $"{tableAlias}{(tableAlias != null ? "." : string.Empty)}[{column.SqlName}]{(columnAlias != null ? " as \"" : string.Empty)}{columnAlias}{(columnAlias != null ? "\"" : string.Empty)}";
        }

        public override string FormatQTable(IDBTable table, string alias)
        {
            var schema = table.Schema?.Connection?.Schema;
            if (!string.IsNullOrEmpty(schema))
            {
                return $"[{schema}].[{table.SqlName}] {alias}";
            }
            return $"[{table.SqlName}] {alias}";
        }

        public override ValueTask<IDataParameter> CreateStreamParameter(IDbCommand command, DBColumn<byte[]> dataColumn, Stream stream)
        {
            var parameter = ((SqlCommand)command).Parameters.Add($"@{dataColumn.SqlName}", SqlDbType.Binary, -1);
            parameter.Value = stream;
            return new ValueTask<IDataParameter>(parameter);
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

        public override uint GetUInt(DbDataReader reader, int index)
        {
            return ((SqlDataReader)reader).GetFieldValue<uint>(index);
        }

        public override TimeSpan GetTimeSpan(DbDataReader reader, int index)
        {
            return ((SqlDataReader)reader).GetTimeSpan(index);
        }
    }
}
