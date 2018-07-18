using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using DataWF.Common;
using Npgsql;

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
            return new NpgsqlConnectionStringBuilder()
            {
                Timeout = connection.TimeOut,
                Username = connection.User,
                Password = connection.Password,
                Host = connection.Host,
                Database = connection.DataBase,
                Port = (int)connection.Port
            };
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
                    ddl.AppendLine($"alter sequence {sequence.Name} restart with {sequence.Current} increment by {sequence.Increment}");
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
            ddl.Append($"alter table {column.Table.SqlName} alter column ");
            ddl.Append($"{column.SqlName} TYPE ");
            ddl.Append(FormatType(column));
            if (column.IsNotNull || column.IsPrimaryKey)
            {
                ddl.Append(" not null");
            }
            ddl.AppendLine();
        }

        public override string FormatCreateView(string name)
        {
            return "create or replace view " + name + " as";
        }

        public override void WriteValue(DBColumn column, object value, IDataParameter parameter, IDbConnection connection)
        {
            base.WriteValue(column, value, parameter, connection);
            if (column.DBDataType == DBDataType.TimeSpan)
            {
                parameter.DbType = DbType.Time;
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
    }
}