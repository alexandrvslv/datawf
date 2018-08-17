using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Text;

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
                    {DBDataType.Blob, "varbinary(max)"},
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
            DBService.OnExecute(DBExecuteType.CreateConnection, "New Connection", TimeSpan.Zero, connection.ToString());
            return new SqlConnection(GetConnectionString(connection));
        }

        public override string GetConnectionString(DBConnection connection)
        {
            return GetConnectionStringBuilder(connection).ConnectionString;
        }

        public override DbConnectionStringBuilder GetConnectionStringBuilder(DBConnection connection)
        {
            var builder = new SqlConnectionStringBuilder();
            builder.ConnectTimeout = connection.TimeOut;
            builder.IntegratedSecurity = connection.IntegratedSecurity;
            if (!connection.IntegratedSecurity)
            {
                builder.UserID = connection.User;
                builder.Password = connection.Password;
            }
            builder.DataSource = connection.Host + (connection.Port == 0 ? string.Empty : "," + connection.Port);
            builder.InitialCatalog = connection.DataBase;
            builder.Pooling = connection.Pool;
            builder.Encrypt = connection.Encrypt;
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

        public override void WriteValue(DBColumn column, object value, IDataParameter parameter, IDbConnection connection)
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
            base.WriteValue(column, value, parameter, connection);
        }

        public override string FormatQColumn(DBColumn column)
        {
            if (column.ColumnType == DBColumnTypes.Internal 
                || column.ColumnType == DBColumnTypes.Expression
                || column.ColumnType == DBColumnTypes.Code)
                return string.Empty;
            else if (column.ColumnType == DBColumnTypes.Query && column.Table.Type != DBTableType.View)
                return base.FormatQColumn(column);
            else
                return $"[{column.Name}]";
        }

        public override string FormatQTable(DBTable table)
        {
            var schema = table.Schema?.Connection?.Schema;
            if (!string.IsNullOrEmpty(schema))
            {
                return $"[{schema}].[{table.SqlName}]";
            }
            return $"[{table.SqlName}]";
        }
    }
}
