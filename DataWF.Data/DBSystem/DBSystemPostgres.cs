using DataWF.Common;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public class DBSystemPostgres : DBSystem
    {
        public DBSystemPostgres()
        {
            Name = "Postgres";
            DataTypeMap = new Dictionary<DBDataType, string>(){
                    {DBDataType.String, "varchar{0}"},
                    {DBDataType.Clob, "text"},
                    {DBDataType.DateTime, "timestamp"},
                    {DBDataType.ByteArray, "bytea"},
                    {DBDataType.Blob, "bytea"},
                    {DBDataType.BigInt, "bigint"},
                    {DBDataType.Int, "integer"},
                    {DBDataType.ShortInt, "smallint"},
                    {DBDataType.TinyInt, "smallint"},
                    {DBDataType.Float, "float4"},
                    {DBDataType.Double, "float8"},
                    {DBDataType.Decimal, "numeric{0}"},
                    {DBDataType.TimeSpan, "interval"},
                    {DBDataType.Bool, "bool"},
                    {DBDataType.LargeObject, "oid"}
                };
        }

        public override IDbConnection CreateConnection(DBConnection connection)
        {
            return new NpgsqlConnection(GetConnectionString(connection));
        }

        public override string GetConnectionString(DBConnection connection)
        {
            return GetConnectionStringBuilder(connection).ConnectionString;
        }

        public override DbConnectionStringBuilder GetConnectionStringBuilder(DBConnection connection)
        {
            var nConnection = new NpgsqlConnectionStringBuilder()
            {
                Timeout = connection.TimeOut,
                CommandTimeout = connection.TimeOut,
                Username = connection.User,
                Password = connection.Password,
                Host = connection.Host,
                Database = connection.DataBase,
                Port = (int)connection.Port
            };
            if (connection.Pool != null)
            {
                nConnection.Pooling = (bool)connection.Pool;
            }
            //performance
            nConnection.ReadBufferSize = 81920;
            nConnection.WriteBufferSize = 81920;
            nConnection.SocketReceiveBufferSize = 8192 * 3;
            nConnection.SocketSendBufferSize = 8192 * 3;
            nConnection.Enlist = false;
            return nConnection;
        }

        public override DbProviderFactory GetFactory()
        {
            return NpgsqlFactory.Instance;
        }

        public override void DropDatabase(DBSchema schema)
        {
            var tempDatabase = schema.Connection.DataBase;
            try
            {
                schema.Connection.DataBase = "postgres";
                var ddl = new StringBuilder();
                Format(ddl, schema, DDLType.Drop);
                schema.Connection.ExecuteQuery(ddl.ToString(), true, DBExecuteType.NoReader);

                ddl.Clear();
                FormatTablespace(ddl, schema, DDLType.Drop);
                schema.Connection.ExecuteQuery(ddl.ToString(), true, DBExecuteType.NoReader);
            }
            catch (Exception ex) { Helper.OnException(ex); }
            finally { schema.Connection.DataBase = tempDatabase; }
        }

        public override void CreateDatabase(DBSchema schema, DBConnection connection)
        {
            //DropDatabase(schema);

            connection.DataBase = "postgres";

            var ddl = new StringBuilder();
            FormatTablespace(ddl, schema, DDLType.Create);
            connection.ExecuteGoQuery(ddl.ToString(), true);
            ddl.Clear();
            Format(ddl, schema, DDLType.Create);
            connection.ExecuteGoQuery(ddl.ToString(), true);

            connection.DataBase = schema.Name;

            CreateSchema(schema, connection);
        }

        public override string SequenceCurrentValue(DBSequence sequence)
        {
            return $"select currval('{sequence.Name}')";
        }

        public override string SequenceNextValue(DBSequence sequence)
        {
            return $"select nextval('{sequence.Name}')";
        }

        public override string SequenceInline(DBSequence sequence)
        {
            return $"nextval('{sequence.Name}')";
        }

        public override void FormatInsertSequence(StringBuilder command, DBTable table, DBItem row)
        {
            FormatInsert(command, table, SequenceInline(table.Sequence), row); command.AppendLine(";");
            command.AppendLine(SequenceCurrentValue(table.Sequence));
        }

        public override void Format(StringBuilder ddl, DBSequence sequence, DDLType ddlType)
        {
            switch (ddlType)
            {
                case DDLType.Create://if not exists  pg 9.5 or above
                    ddl.AppendLine($"create sequence {sequence.Name} start with {sequence.Current} increment by {sequence.Increment}");
                    break;
                case DDLType.Alter:
                    ddl.AppendLine($"alter sequence {sequence.Name} restart with {sequence.Current + 1} increment by {sequence.Increment}");
                    break;
                case DDLType.Drop:
                    ddl.AppendLine($"drop sequence {sequence.Name}");
                    break;
            }
        }

        public void FormatTablespace(StringBuilder ddl, DBSchema schema, DDLType ddlType)
        {
            if (ddlType == DDLType.Create)
            {

                ddl.AppendLine($"create tablespace ts_{schema.DataBase} owner {schema.Connection.User} location '{schema.Connection.Path}';");
            }
            else if (ddlType == DDLType.Drop)
            {
                ddl.AppendLine($"drop tablespace ts_{schema.DataBase};");
            }
        }

        public override void Format(StringBuilder ddl, DBSchema schema, DDLType ddlType)
        {
            if (ddlType == DDLType.Create)
            {

                ddl.AppendLine($@"create database {schema.DataBase} 
  with  owner {schema.Connection.User}
        encoding = 'UTF8' 
        tablespace = ts_{schema.DataBase};");

            }
            else if (ddlType == DDLType.Drop)
            {
                ddl.AppendLine($"drop database {schema.DataBase};");
            }
        }

        public override void FormatAlter(StringBuilder ddl, DBColumn column)
        {
            ddl.AppendLine($"alter table {column.Table.SqlName} alter column {column.SqlName} TYPE {FormatType(column)};");
            if (column.IsNotNull || column.IsPrimaryKey)
            {
                ddl.AppendLine($"alter table {column.Table.SqlName} alter column {column.SqlName} SET not null;");
            }

        }

        public override string FormatCreateView(string name)
        {
            return $"drop view if exists {name}; create view {name} as";
        }

        public override void WriteValue(IDbCommand command, IDataParameter parameter, object value, DBColumn column)
        {
            base.WriteValue(command, parameter, value, column);
            if (column != null)
            {
                if (column.DBDataType == DBDataType.TimeSpan)
                {
                    parameter.DbType = DbType.Time;
                }
                else if (column.DBDataType == DBDataType.LargeObject)
                {
                    ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Oid;
                }
                else if (column.DataType == typeof(uint))
                {
                    ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer;
                }
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
                else if (from == typeof(short))
                {
                    if (temp == typeof(byte))
                        value = (byte)(short)value;
                    else
                        throw new InvalidCastException($"From {from} to {temp}");
                }
            }
            return value;
        }

        public override string FormatException(Exception exception, DBTable table, DBItem item)
        {
            var lines = new List<string>();
            while (exception != null)
            {
                var text = exception is PostgresException pex
                    ? FormatMessage(pex, table, item)
                    : exception.Message;
                if (lines.FindIndex(p => p.Equals(text, StringComparison.Ordinal)) < 0)
                    lines.Add(text);
                exception = exception.InnerException;
            }
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }
            return sb.ToString();
        }

        private string FormatMessage(PostgresException pex, DBTable table, DBItem dbItem)
        {
            var text = string.IsNullOrEmpty(pex.Detail) ? pex.MessageText : pex.Detail;
            var refTable = string.IsNullOrEmpty(pex.TableName) ? null : DBService.Schems.ParseTable(pex.TableName);
            var builder = new StringBuilder();
            text = text.Replace("character varying", "Text");
            foreach (var item in text.Split(new char[] { ',', ' ', '"', '(', ')' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (table != null)
                {
                    var column = refTable?.ParseColumnProperty(item) ?? table?.ParseColumnProperty(item);
                    if (column != null)
                    {
                        builder.Append(column.DisplayName);
                        builder.Append(' ');
                        continue;
                    }
                }

                builder.Append(item);
                builder.Append(' ');
            }

            return builder.ToString();
        }

        public override async Task<uint> SetLOB(Stream value, DBTransaction transaction)
        {
            if (value.CanSeek)
            {
                value.Position = 0;
            }
            var manager = new NpgsqlLargeObjectManager((NpgsqlConnection)transaction.Connection);
            var bufferSize = 81920 * 2;
            var buffer = new byte[bufferSize];

            var oid = await manager.CreateAsync(0, CancellationToken.None);

            using (var lobStream = await manager.OpenReadWriteAsync(oid, CancellationToken.None))
            {
                //await value.CopyToAsync(lobStream);
                int count;
                while ((count = value.Read(buffer, 0, bufferSize)) != 0)
                {
                    lobStream.Write(buffer, 0, count);
                }
            }
            return oid;
        }

        public override async Task<Stream> GetLOB(uint oid, DBTransaction transaction)
        {
            var manager = new NpgsqlLargeObjectManager((NpgsqlConnection)transaction.Connection);
            return await manager.OpenReadAsync(oid, CancellationToken.None);
        }

        public async Task<Stream> GetLOBBuffering(uint oid, DBTransaction transaction)
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

        public override async Task DeleteLOB(uint oid, DBTransaction transaction)
        {
            var manager = new NpgsqlLargeObjectManager((NpgsqlConnection)transaction.Connection);
            await manager.UnlinkAsync(oid, CancellationToken.None);
        }

        //public override void ReadSequential(DBItem item, DBColumn column, Stream stream, int bufferSize = 81920)
        //{
        //    var Conn = (NpgsqlConnection)CreateConnection(item.Table.Schema.Connection);
        //    // Retrieve a Large Object Manager for this connection
        //    var manager = new NpgsqlLargeObjectManager(Conn);

        //    // Create a new empty file, returning the identifier to later access it
        //    //uint oid = manager.OpenRead();

        //    // Reading and writing Large Objects requires the use of a transaction
        //    using (var transaction = Conn.BeginTransaction())
        //    {
        //        // Open the file for reading and writing
        //        using (var managerStream = manager.OpenRead(oid))
        //        {
        //            managerStream.CopyTo(stream, bufferSize);
        //            // buf2 now contains 1, 2, 3
        //        }
        //        // Save the changes to the object
        //        transaction.Commit();
        //    }

        //}
    }
}