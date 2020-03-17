using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DataWF.Common;
using Microsoft.Data.Sqlite;

namespace DataWF.Data
{
    public class DBSystemSQLite : DBSystem
    {
        public DBSystemSQLite()
        {
            Name = "SQLite";
            DataTypeMap = new Dictionary<DBDataType, string>(){
                    {DBDataType.String, "varchar{0}"},
                    {DBDataType.Clob, "clob"},
                    {DBDataType.DateTime, "datetime"},
                    {DBDataType.ByteArray, "blob"},
                    {DBDataType.LargeObject, "integer"},
                    {DBDataType.Blob, "blob"},
                    {DBDataType.BigInt, "bigint"},
                    {DBDataType.Int, "integer"},
                    {DBDataType.ShortInt, "smallint"},
                    {DBDataType.TinyInt, "tinyint"},
                    {DBDataType.Float, "float"},
                    {DBDataType.Double, "double"},
                    {DBDataType.Decimal, "numeric"},
                    {DBDataType.TimeSpan, "bigint"},
                    {DBDataType.Bool, "bool"},
                };
        }

        public override IDbConnection CreateConnection(DBConnection connection)
        {
            return new SqliteConnection(GetConnectionString(connection));
        }

        public override string GetConnectionString(DBConnection connection)
        {
            return GetConnectionStringBuilder(connection).ConnectionString;
        }

        public override void DropDatabase(DBSchema schema)
        {
            var file = schema.Connection.DataBase;
            if (File.Exists(file))
            {
                try { File.Delete(file); }
                catch (Exception ex) { Helper.OnException(ex); }
            }
        }

        public override DbConnectionStringBuilder GetConnectionStringBuilder(DBConnection connection)
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = connection.DataBase,
                Cache = SqliteCacheMode.Shared,
                Mode = SqliteOpenMode.ReadWriteCreate
            };
            //builder.Pooling = connection.Pool;
            //builder.Timeout = connection.TimeOut;
            //builder.Enlist = false;
            //((SqliteConnectionStringBuilder)builder).JournalMode = SQLiteJournalModeEnum.Off;
            //((SqliteConnectionStringBuilder)builder).SyncMode = SynchronizationModes.Off;
            //if (!string.IsNullOrEmpty(connection.Password))
            //{
            //    builder.HexPassword = Encoding.UTF8.GetBytes(connection.Password);
            //}
            return builder;
        }

        public override DbProviderFactory GetFactory()
        {
            return SqliteFactory.Instance;
        }

        public override string SequenceCurrentValue(DBSequence sequence)
        {
            return $"select seq from db_sequence where name = '{sequence.Name}';";
        }

        public override string SequenceNextValue(DBSequence sequence)
        {
            return $@"update db_sequence set seq = seq + {sequence.Increment} where name = '{sequence.Name}';
select seq from db_sequence where name = '{sequence.Name}';";
        }

        public override string SequenceInline(DBSequence sequence)
        {
            throw new NotSupportedException();
        }

        public override void FormatInsertSequence(StringBuilder command, DBTable table, DBItem row)
        {
            throw new NotSupportedException();
        }

        public override void Format(StringBuilder ddl, DBSequence sequence, DDLType ddlType)
        {
            switch (ddlType)
            {
                case DDLType.Create:
                    ddl.AppendLine($"insert into db_sequence(name, seq) values('{sequence.Name}', {sequence.Current - 1});");
                    break;
                case DDLType.Alter:
                    ddl.AppendLine($"update db_sequence set seq = {sequence.Current} where name = '{sequence.Name}';");
                    break;
                case DDLType.Drop:
                    ddl.AppendLine($"delete from db_sequence where name = '{sequence.Name}';");
                    break;
            }
        }

        public override void Format(StringBuilder ddl, DBSchema schema, DDLType ddlType)
        {
            if (ddlType == DDLType.Create)
            {
                ddl.AppendLine($"create table db_sequence(name varchar(512) not null primary key, seq long);");
                ddl.AppendLine($"create table db_lob(oid bigint not null primary key, lob_data blob);");
            }
            else if (ddlType == DDLType.Drop)
            {
                ddl.AppendLine($"drop table db_sequence;");
                ddl.AppendLine($"drop table db_lob;");
            }
        }

        public override object ReadValue(DBColumn column, object value)
        {
            if (value == DBNull.Value)
            {
                return null;
            }
            var from = value.GetType();
            if (from != column.DataType)
            {
                var temp = column.DataType;
                if (temp.IsEnum)
                {
                    temp = temp.GetEnumUnderlyingType();
                    if (from == temp)
                        return value;
                }
                if (from == typeof(long))
                {
                    if (temp == typeof(int))
                        value = (int)(long)value;
                    else if (temp == typeof(short))
                        value = (short)(long)value;
                    else if (temp == typeof(byte))
                        value = (byte)(long)value;
                    else if (temp == typeof(bool))
                        value = (long)value == 1;
                    else
                        throw new InvalidCastException($"From {from} to {temp}");
                }
                else if (from == typeof(decimal))
                {
                    if (temp == typeof(double))
                        value = (double)(decimal)value;
                    else if (temp == typeof(float))
                        value = (float)(decimal)value;
                    else
                        throw new InvalidCastException($"From {from} to {temp}");
                }
                else if (from == typeof(double))
                {
                    if (temp == typeof(float))
                        value = (float)(double)value;
                    else if (temp == typeof(decimal))
                        value = (decimal)(double)value;
                    else
                        throw new InvalidCastException($"From {from} to {temp}");
                }
                else if (from == typeof(string))
                {
                    if (temp == typeof(DateTime))
                        value = DateTime.TryParse((string)value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? (object)date : null;
                }
                else
                    throw new InvalidCastException($"From {from} to {temp}");
            }
            return value;
        }

        public override IEnumerable<DBTableInfo> GetTablesInfo(DBConnection connection, string schemaName = null, string tableName = null)
        {
            var filterTable = tableName != null ? $" and tbl_name = '{tableName}'" : "";
            var list = connection.ExecuteQResult($"select * from sqlite_master where type = 'table'{filterTable}");
            int iName = list.GetIndex("tbl_name");
            foreach (object[] item in list.Values)
            {
                var table = new DBTableInfo()
                {
                    Name = item[iName].ToString(),
                };
                GetColumnsInfo(connection, table);
                yield return table;
            }
        }

        public override void GetColumnsInfo(DBConnection connection, DBTableInfo table)
        {
            var list = connection.ExecuteQResult($"PRAGMA table_info({table.Name})");
            int iName = list.GetIndex("name");
            int iType = list.GetIndex("type");
            int iNull = list.GetIndex("notnull");
            int iDefault = list.GetIndex("dflt_value");

            foreach (object[] item in list.Values)
            {
                table.Columns.Add(new DBColumnInfo()
                {
                    Name = item[iName].ToString(),
                    DataType = item[iType].ToString(),
                    NotNull = item[iNull].ToString().Equals("1"),
                    Default = item[iDefault].ToString(),
                });
            }
        }

        public override async Task DeleteLOB(uint oid, DBTransaction transaction)
        {
            var command = (SqliteCommand)transaction.AddCommand($"delete from db_lob where oid = $oid");
            command.Parameters.AddWithValue($"$oid", (long)oid);
            await transaction.ExecuteQueryAsync(command);
        }

        public override async Task<Stream> GetLOB(uint oid, DBTransaction transaction, int bufferSize = 81920)
        {
            var command = (SqliteCommand)transaction.AddCommand($"select oid, lob_data from db_lob where oid = $oid");
            command.Parameters.AddWithValue($"$oid", (long)oid);
            transaction.Reader = (IDataReader)await transaction.ExecuteQueryAsync(command, DBExecuteType.Reader, CommandBehavior.SequentialAccess);
            if (await transaction.ReadAsync())
            {
                return ((SqliteDataReader)transaction.Reader).GetStream(1);
            }
            throw new Exception("No Data Found!");
        }

        public override async Task<uint> SetLOB(Stream value, DBTransaction transaction)
        {
            var command = (SqliteCommand)transaction.AddCommand(@"insert into db_lob (lob_data) values (zeroblob($length));
select last_insert_rowid();");
            command.Parameters.AddWithValue("$length", value.Length);
            var oid = (long)await transaction.ExecuteQueryAsync(command);

            // Open a stream to write the data
            using (var blobStream = new SqliteBlob((SqliteConnection)transaction.Connection, "db_lob", "lob_data", oid))
            {
                await value.CopyToAsync(blobStream);
            }
            return (uint)oid;
        }

        public override async Task<object> ExecuteQueryAsync(IDbCommand command, DBExecuteType type, CommandBehavior behavior)
        {
            var sqliteCommand = (SqliteCommand)command;
            switch (type)
            {
                case DBExecuteType.Scalar:
                    return await sqliteCommand.ExecuteScalarAsync();
                case DBExecuteType.Reader:
                    return await sqliteCommand.ExecuteReaderAsync(behavior);
                case DBExecuteType.NoReader:
                    return await sqliteCommand.ExecuteNonQueryAsync();
            }
            return null;
        }

        public override Task<bool> ReadAsync(IDataReader reader)
        {
            var sqlReader = (SqliteDataReader)reader;
            return sqlReader.ReadAsync();
        }

        public override uint GetOID(IDataReader reader, int index)
        {
            return ((SqliteDataReader)reader).GetFieldValue<uint>(index);
        }

        public override TimeSpan GetTimeSpan(IDataReader reader, int index)
        {
            return ((SqliteDataReader)reader).GetTimeSpan(index);
        }
    }
}
