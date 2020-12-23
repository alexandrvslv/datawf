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
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public class DBSystemOracle : DBSystem
    {
        public DBSystemOracle()
        {
            Name = "Oracle";
            DataTypeMap = new Dictionary<DBDataType, string>(){
                {DBDataType.String, "nvarchar2{0}"},
                {DBDataType.Clob, "nclob"},
                {DBDataType.DateTime, "timestamp(6)"},
                {DBDataType.ByteArray, "raw{0}"},
                {DBDataType.ByteSerializable, "raw{0}"},
                {DBDataType.Blob, "blob"},
                {DBDataType.BigInt, "number(18)"},
                {DBDataType.Int, "number(9)"},
                {DBDataType.ShortInt, "number(4)"},
                {DBDataType.TinyInt, "number(2)"},
                {DBDataType.Float, "binary_float"},
                {DBDataType.Double, "binary_double"},
                {DBDataType.Decimal, "number{0}"},
                {DBDataType.TimeSpan, "timespan"},
                {DBDataType.Bool, "number(1)"},
            };
        }

        public override IDbConnection CreateConnection(DBConnection connection)
        {
            return new OracleConnection(GetConnectionString(connection));
        }

        public override string GetConnectionString(DBConnection connection)
        {
            return GetConnectionStringBuilder(connection).ConnectionString;
        }

        public override DbConnectionStringBuilder GetConnectionStringBuilder(DBConnection connection)
        {
            var builder = new OracleConnectionStringBuilder
            {
                ConnectionTimeout = connection.TimeOut,
                UserID = connection.User,
                Password = connection.Password,
                DataSource = string.Format("(DESCRIPTION = (ADDRESS_LIST = (ADDRESS = (PROTOCOL = TCP)(HOST = {0})(PORT = {1})))(CONNECT_DATA = (SID = {2})))",
                                               connection.Host, connection.Port, connection.DataBase)
            };
            return builder;
        }

        public override DbProviderFactory GetFactory()
        {
            return OracleClientFactory.Instance;
        }

        public override void CreateDatabase(DBSchema schema, DBConnection connection)
        {
            //DropDatabase(schema);

            var ddl = new StringBuilder();
            Format(ddl, schema, DDLType.Create);
            connection.ExecuteGoQuery(ddl.ToString(), true);

            if (connection.Schema?.Length > 0)
            {
                connection.User = schema.Name;
            }
            if (string.IsNullOrEmpty(connection.DataBase))
            {
                connection.DataBase = schema.Name;
            }

            ddl.Clear();
            Format(ddl, schema);
            connection.ExecuteGoQuery(ddl.ToString(), true);
        }

        public override void Format(StringBuilder ddl, DBSchema schema, DDLType ddlType)
        {
            string tsname = $"ts{schema.Name}";
            if (ddlType == DDLType.Create)
            {
                ddl.Append($"begin\n");
                ddl.Append($" execute immediate 'create tablespace {tsname} datafile ''{tsname}.dat'' size 10M autoextend on';\n");
                ddl.Append($" execute immediate 'create temporary tablespace {tsname}_temp tempfile ''{tsname}_temp.dat'' size 5M autoextend on';\n");
                ddl.Append($" execute immediate 'create user {schema.Name} identified by {schema.Name} default tablespace {tsname} temporary tablespace {tsname}_temp';\n");
                ddl.Append($" execute immediate 'grant create session to {schema.Name}';\n");
                ddl.Append($" execute immediate 'grant create table to {schema.Name}';\n");
                ddl.Append($" execute immediate 'grant create view to {schema.Name}';\n");
                ddl.Append($" execute immediate 'grant create sequence to {schema.Name}';\n");
                ddl.Append($" execute immediate 'grant create any procedure to {schema.Name}';\n");
                ddl.Append($" execute immediate 'grant unlimited tablespace to {schema.Name}';\n");
                ddl.Append($"end;");
            }
            else if (ddlType == DDLType.Drop)
            {
                ddl.Append($"begin\n");
                ddl.Append($" execute immediate 'drop user {schema.Name} CASCADE';\n");
                ddl.Append($" execute immediate 'drop tablespace {tsname} including contents and datafiles';\n");
                ddl.Append($" execute immediate 'drop tablespace {tsname}_temp including contents and datafiles';\n");
                ddl.Append($"end;");
            }
        }

        public override string SequenceCurrentValue(DBSequence sequence)
        {
            return $"select {sequence.Name}.currval from dual";
        }

        public override string SequenceNextValue(DBSequence sequence)
        {
            return $"select {sequence.Name}.nextval from dual";
        }

        public override string SequenceInline(DBSequence sequence)
        {
            return $"{sequence.Name}.nextval";
        }

        public override void Format(StringBuilder ddl, DBSequence sequence, DDLType ddlType)
        {
            switch (ddlType)
            {
                case DDLType.Create:
                    ddl.AppendLine($"create sequence {sequence.Name} start with {sequence.StartWith - 1} increment by {sequence.Increment}");
                    break;
                case DDLType.Alter:
                    ddl.AppendLine($"alter sequence {sequence.Name} restart with {sequence.Current} increment by {sequence.Increment}");
                    break;
                case DDLType.Drop:
                    ddl.AppendLine($"drop sequence {sequence.Name}");
                    break;
            }
        }

        public override void FormatAlter(StringBuilder ddl, DBColumn column)
        {
            ddl.Append($"alter table {column.Table.SqlName} modify ");
            Format(ddl, column);
        }

        public override void FormatInsertSequence(StringBuilder command, DBTable table, DBItem row)
        {
            command.AppendLine($"begin");
            command.AppendLine($"select {SequenceInline(table.Sequence)} into :{table.PrimaryKey.SqlName} from dual;");
            FormatInsert(command, table, $":{table.PrimaryKey.SqlName}", row); command.AppendLine(";");
            command.Append("end;");
        }

        public override IEnumerable<DBTableInfo> GetTablesInfo(DBConnection connection, string schemaName, string tableName = null)
        {
            var filter = schemaName != null ? $" where owner = '{schemaName.ToUpper()}'{(tableName != null ? $" and table_name = '{tableName.ToUpper()}'" : null)}" : null;
            QResult list = connection.ExecuteQResult($"select * from all_tables{filter}");
            int iSchema = list.GetIndex("owner");
            int iName = list.GetIndex("table_name");
            foreach (object[] item in list.Values)
            {
                var table = new DBTableInfo()
                {
                    Schema = item[iSchema].ToString(),
                    Name = item[iName].ToString(),
                };
                GetColumnsInfo(connection, table);
                yield return table;
            }
        }

        public override void GetColumnsInfo(DBConnection connection, DBTableInfo tableInfo)
        {
            var query = string.Format("select * from all_tab_cols where table_name = '{0}'{1} order by column_id",
                                      tableInfo.Name,
                                      string.IsNullOrEmpty(tableInfo.Schema) ? null : $" and owner = '{tableInfo.Schema}'");
            QResult list = connection.ExecuteQResult(query);
            int iName = list.GetIndex("column_name");
            int iType = list.GetIndex("data_type");
            int iPrec = list.GetIndex("data_precision");
            int iScale = list.GetIndex("data_scale");
            int iLeng = list.GetIndex("data_length");
            int iNull = list.GetIndex("nullable");
            int iDefault = list.GetIndex("data_default");

            foreach (object[] item in list.Values)
            {
                tableInfo.Columns.Add(new DBColumnInfo()
                {
                    Name = item[iName].ToString(),
                    DataType = item[iType].ToString(),
                    Precision = item[iPrec].ToString(),
                    Scale = item[iScale].ToString(),
                    Length = item[iLeng].ToString(),
                    NotNull = item[iNull].Equals("N"),
                    Default = item[iDefault].ToString(),
                });
            }
        }


        public override object FillParameter(IDbCommand command, IDataParameter parameter, object value, DBColumn column)
        {
            value = base.FillParameter(command, parameter, value, column);

            if (value == null || value == DBNull.Value)
            {
                if (column.IsPrimaryKey)
                    parameter.Direction = ParameterDirection.InputOutput;
                return value;
            }
            var dbParameter = (OracleParameter)parameter;
            switch (column.DBDataType)
            {
                case DBDataType.ByteArray:
                case DBDataType.ByteSerializable:
                    dbParameter.Direction = ParameterDirection.Input;
                    dbParameter.OracleDbType = OracleDbType.Raw;
                    if (value != null)
                    {
                        value =
                        dbParameter.Value = new OracleBinary((byte[])value);
                    }
                    break;
                case DBDataType.Blob:
                    dbParameter.Direction = ParameterDirection.Input;
                    dbParameter.OracleDbType = OracleDbType.Blob;
                    if (value != null)
                    {
                        var blob = new OracleBlob((OracleConnection)command.Connection);
                        blob.Write((byte[])value, 0, ((byte[])value).Length);
                        blob.Position = 0L;
                        value =
                        dbParameter.Value = blob;
                    }
                    break;
                case DBDataType.Clob:
                    dbParameter.Direction = ParameterDirection.Input;
                    dbParameter.OracleDbType = OracleDbType.NClob;
                    if (value != null)
                    {
                        var clob = new OracleClob((OracleConnection)command.Connection, false, true);
                        clob.Write(((string)value).ToCharArray(), 0, ((string)value).Length);
                        clob.Position = 0L;
                        value =
                        dbParameter.Value = clob;
                    }
                    break;
                case DBDataType.String:
                    dbParameter.Size = column.Size;
                    break;
                case DBDataType.Decimal:
                    if (column.Size > 0)
                        dbParameter.Precision = (byte)column.Size;
                    if (column.Scale > 0)
                        dbParameter.Scale = (byte)column.Scale;
                    break;
                case DBDataType.Float:
                    dbParameter.OracleDbType = OracleDbType.BinaryFloat;
                    break;
                case DBDataType.Double:
                    dbParameter.OracleDbType = OracleDbType.BinaryDouble;
                    break;
                case DBDataType.Bool:
                    dbParameter.DbType = DbType.Decimal;
                    dbParameter.Precision = 1;
                    value =
                    dbParameter.Value = (bool)value ? 1 : 0;
                    break;
                    ////case DBDataType.ShortInt:
                    //dbParameter.DbType = DbType.Decimal;
                    //dbParameter.Precision = 5;
                    //break;
                    //case DBDataType.Int:
                    //dbParameter.DbType = DbType.Decimal;
                    //dbParameter.Precision = 10;
                    //break;
            }
            return value;
        }

        public override object ReadValue(DBColumn column, object value)
        {
            if (value == DBNull.Value)
            {
                return null;
            }
            var from = value?.GetType();
            if (from != column.DataType)
            {
                var temp = column.DataType;
                if (temp.IsEnum)
                {
                    temp = temp.GetEnumUnderlyingType();
                    if (from == temp)
                        return value;
                }
                if (from == typeof(short))
                {
                    if (temp == typeof(byte))
                        value = (byte)(short)value;
                    else if (temp == typeof(bool))
                        value = (short)value == 1;
                    else
                        throw new InvalidCastException($"From {from} to {temp}");
                }
                else
                {
                    throw new InvalidCastException($"From {from} to {temp}");
                }
            }
            return value;
        }

        public override void ReadValue(DBItem item, IDataParameter parameter)
        {
            var type = parameter.Value.GetType();
            if (type == typeof(OracleTimeStamp))
                item[parameter.SourceColumn] = ((OracleTimeStamp)parameter.Value).Value;
            else if (type == typeof(OracleDate))
                item[parameter.SourceColumn] = ((OracleDate)parameter.Value).Value;
            else
                item[parameter.SourceColumn] = parameter.Value;
        }

        public override void UploadCommand(DBItem item, IDbCommand command)
        {
            foreach (IDbDataParameter parameter in command.Parameters)
            {
                var type = parameter.Value?.GetType();
                if (type == typeof(OracleBlob))
                    ((OracleBlob)parameter.Value).Dispose();
                else if (type == typeof(OracleClob))
                    ((OracleClob)parameter.Value).Dispose();
            }
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
            using (var blob = new OracleBlob((OracleConnection)transaction.Connection))
            {
                await value.CopyToAsync(blob);
                var command = (OracleCommand)transaction.AddCommand($@"insert into {FileData.DBTable.Name} ({FileData.IdKey.SqlName}, {FileData.DataKey.SqlName}) 
values (:{FileData.IdKey.SqlName}, :{FileData.DataKey.SqlName})");
                command.Parameters.Add($":{FileData.IdKey.SqlName}", OracleDbType.Long, id, ParameterDirection.Input);
                command.Parameters.Add($":{FileData.DataKey.SqlName}", OracleDbType.Blob, -1).Value = blob;
                await transaction.ExecuteQueryAsync(command, DBExecuteType.NoReader);
            }
        }

        public override async Task<object> ExecuteQueryAsync(IDbCommand command, DBExecuteType type, CommandBehavior behavior)
        {
            var oracleCommand = (OracleCommand)command;
            switch (type)
            {
                case DBExecuteType.Scalar:
                    return await oracleCommand.ExecuteScalarAsync();
                case DBExecuteType.Reader:
                    return await oracleCommand.ExecuteReaderAsync(behavior);
                case DBExecuteType.NoReader:
                    return await oracleCommand.ExecuteNonQueryAsync();
            }
            return null;
        }

        public override uint GetUInt(DbDataReader reader, int index)
        {
            return ((OracleDataReader)reader).GetFieldValue<uint>(index);
        }

        public override TimeSpan GetTimeSpan(DbDataReader reader, int index)
        {
            return ((OracleDataReader)reader).GetTimeSpan(index);
        }
    }
}