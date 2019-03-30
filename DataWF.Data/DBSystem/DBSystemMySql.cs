using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

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
                    {DBDataType.DateTime, "datetime"},
                    {DBDataType.ByteArray, "varbinary{0}"},
                    {DBDataType.Blob, "blob"},
                    {DBDataType.LargeObject, "integer"},
                    {DBDataType.BigInt, "bigint"},
                    {DBDataType.Int, "integer"},
                    {DBDataType.ShortInt, "smallint"},
                    {DBDataType.TinyInt, "tinyint unsigned"},
                    {DBDataType.Float, "float(22,11)"},
                    {DBDataType.Double, "double"},
                    {DBDataType.Decimal, "numeric{0}"},
                    {DBDataType.TimeSpan, "bigint"},
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
            return MySqlClientFactory.Instance;
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
    }
}
