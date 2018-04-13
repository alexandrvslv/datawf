using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
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
                    {DBDataType.DateTime, "timestamp(6)"},
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
                case DDLType.Create:
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

        public override string FormatCreateView(string name)
        {
            return "create or replace view " + Name + " as";
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