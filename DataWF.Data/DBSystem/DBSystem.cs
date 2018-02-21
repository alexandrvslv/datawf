using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using DataWF.Common;

namespace DataWF.Data
{
    public abstract class DBSystem
    {
        public static readonly DBSystem MSSql = new DBSystemMSSql();
        public static readonly DBSystem MySql = new DBSystemMySql();
        public static readonly DBSystem Oracle = new DBSystemOracle();
        public static readonly DBSystem Postgres = new DBSystemPostgres();
        public static readonly DBSystem SQLite = new DBSystemSQLite();

        public string Name { get; internal set; }

        public string ParameterPrefix { get; internal set; } = ":";

        public Dictionary<DBDataType, string> DataTypeMap { get; internal set; }

        public abstract DbConnectionStringBuilder GetConnectionStringBuilder(DBConnection connection);

        public abstract string GetConnectionString(DBConnection connection);

        public abstract IDbConnection CreateConnection(DBConnection connection);

        public abstract DbProviderFactory GetFactory();

        public IDbDataAdapter CreateDataAdapter()
        {
            return GetFactory().CreateDataAdapter();
        }

        public IDbCommand CreateCommand(DBConnection connection)
        {
            IDbCommand command = GetFactory().CreateCommand();
            command.CommandTimeout = connection.TimeOut;
            return command;
        }

        public virtual List<DBTableInfo> GetTablesInfo(DBConnection connection, string schemaName = null, string tableName = null)
        {
            var tableFilter = !string.IsNullOrEmpty(tableName) ? $" and table_name = '{tableName}'" : string.Empty;
            var schemaFilter = !string.IsNullOrEmpty(schemaName) ? $" where table_schema = '{schemaName}'{tableFilter}" : string.Empty;
            QResult list = DBService.ExecuteQResult(connection, $"select * from information_schema.tables{schemaFilter}");
            var infos = new List<DBTableInfo>();
            int iSchema = list.GetIndex("table_schema");
            int iName = list.GetIndex("table_name");
            int iIndex = list.GetIndex("table_type");
            foreach (object[] item in list.Values)
            {
                var table = new DBTableInfo()
                {
                    Schema = item[iSchema].ToString(),
                    Name = item[iName].ToString(),
                    View = item[iIndex].ToString().IndexOf("view", StringComparison.OrdinalIgnoreCase) >= 0,
                };
                table.Columns = GetColumnsInfo(connection, table);
                infos.Add(table);
            }

            return infos;
        }

        public virtual List<DBColumnInfo> GetColumnsInfo(DBConnection connection, DBTableInfo tableInfo)
        {
            var query = string.Format("select * from information_schema.columns where table_name='{0}'{1} order by ordinal_position",
                                      tableInfo.Name,
                                      string.IsNullOrEmpty(tableInfo.Schema) ? null : $" and table_schema = '{tableInfo.Schema}'");
            QResult list = DBService.ExecuteQResult(connection, query);
            var infos = new List<DBColumnInfo>();
            int iName = list.GetIndex("column_name");
            int iType = list.GetIndex("data_type");
            int iPrec = list.GetIndex("numeric_precision");
            int iScale = list.GetIndex("numeric_scale");
            int iLeng = list.GetIndex("character_maximum_length");
            int iNull = list.GetIndex("is_nullable");
            int iDefault = list.GetIndex("column_default");

            foreach (object[] item in list.Values)
            {
                infos.Add(new DBColumnInfo()
                {
                    Name = item[iName].ToString(),
                    DataType = item[iType].ToString(),
                    Precision = item[iPrec].ToString(),
                    Scale = item[iScale].ToString(),
                    Length = item[iLeng].ToString(),
                    NotNull = iNull >= 0 && item[iNull].Equals("NO"),
                    Default = item[iDefault].ToString(),
                });
            }

            return infos;
        }

        public IDbDataParameter CreateParameter()
        {
            return GetFactory().CreateParameter();
        }

        public IDbCommand GetCommand(IDbDataAdapter adapter, DBCommandTypes type)
        {
            IDbCommand command = null;
            var builder = GetFactory().CreateCommandBuilder();
            builder.DataAdapter = (DbDataAdapter)adapter;
            if (type == DBCommandTypes.Delete)
            {
                command = builder.GetDeleteCommand();
            }
            else if (type == DBCommandTypes.Insert)
            {
                command = builder.GetInsertCommand();
            }
            else
            {
                command = builder.GetUpdateCommand();
            }
            adapter.SelectCommand.Connection.Close();
            return command;
        }

        public abstract string SequenceInline(DBSequence sequence);

        public abstract string SequenceNextValue(DBSequence sequence);

        public abstract string SequenceCurrentValue(DBSequence sequence);

        public abstract void Format(StringBuilder ddl, DBSequence sequence, DDLType ddlType);

        public virtual void Format(StringBuilder ddl, DBColumn column)
        {
            ddl.Append($"{column.SqlName} ");
            ddl.Append(FormatType(column));

            if (column.IsNotNull || column.IsPrimaryKey)
            {
                ddl.Append(" not null");
            }
        }

        public string FormatType(DBColumn column)
        {
            return FormatType(column.DBDataType, column.Size, column.Scale);
        }

        public string FormatType(DBDataType dbtype, int size, int scale)
        {
            DataTypeMap.TryGetValue(dbtype, out var type);
            if (type.Contains("{0}"))
            {
                type = string.Format(type, size > 0 ? $"({size}{(scale > 0 ? $",{scale}" : string.Empty)})" : string.Empty);
            }
            return type;
        }

        public virtual void FormatCreate(StringBuilder ddl, DBColumn column)
        {
            ddl.Append($"alter table {column.Table.SqlName} add column ");
            Format(ddl, column);
            ddl.AppendLine();
        }

        public virtual void FormatAlter(StringBuilder ddl, DBColumn column)
        {
            ddl.Append($"alter table {column.Table.SqlName} alter column ");
            Format(ddl, column);
            ddl.AppendLine();
        }

        public virtual void FormatDrop(StringBuilder ddl, DBColumn column)
        {
            ddl.AppendLine($"alter table {column.Table.SqlName} drop column {column.SqlName}");
        }

        public virtual void Format(StringBuilder ddl, DBColumn column, DDLType ddlType)
        {
            var constraints = column.Table.Constraints.GetByColumn(column).ToList();
            switch (ddlType)
            {
                case DDLType.Create:
                    FormatCreate(ddl, column);
                    if (constraints.Count > 0)
                    {
                        ddl.AppendLine("go");
                        foreach (var item in constraints)
                        {
                            Format(ddl, item, DDLType.Create);
                            ddl.AppendLine("go");
                        }
                    }
                    break;
                case DDLType.Alter:
                    if (constraints.Count > 0)
                    {
                        foreach (var item in constraints)
                        {
                            Format(ddl, item, DDLType.Drop);
                            ddl.AppendLine("go");
                        }
                    }
                    FormatAlter(ddl, column);
                    if (constraints.Count > 0)
                    {
                        ddl.AppendLine("go");
                        foreach (var item in constraints)
                        {
                            Format(ddl, item, DDLType.Create);
                            ddl.AppendLine("go");
                        }
                    }
                    break;
                case DDLType.Drop:
                    if (constraints.Count > 0)
                    {
                        ddl.AppendLine("go"); foreach (var item in constraints)
                        {
                            Format(ddl, item, DDLType.Drop);
                            ddl.AppendLine("go");
                        }
                    }
                    FormatDrop(ddl, column);
                    break;
            }
        }

        public virtual void Format(StringBuilder ddl, DBProcedure procedure, DDLType ddlType)
        {
            switch (ddlType)
            {
                case DDLType.Create:
                    ddl.AppendLine(procedure.Source);
                    break;
                case DDLType.Alter:
                    Format(ddl, procedure, DDLType.Drop);
                    ddl.AppendLine("go");
                    Format(ddl, procedure, DDLType.Create);
                    ddl.AppendLine("go");
                    break;
                case DDLType.Drop:
                    if (procedure.ProcedureType == ProcedureTypes.StoredProcedure)
                        ddl.AppendLine($"drop procedure {procedure.Name}");
                    else
                        ddl.AppendLine($"drop function {procedure.Name}");
                    break;
            }
        }

        public virtual void Format(StringBuilder ddl, DBConstraint constraint, DDLType ddlType)
        {
            switch (ddlType)
            {
                case DDLType.Create:
                    ddl.Append($"alter table {constraint.Table.SqlName} add ");
                    Format(ddl, constraint);
                    break;
                case DDLType.Alter:
                    ddl.AppendLine($"alter table {constraint.Table.SqlName} drop constraint {constraint.Name}");
                    ddl.AppendLine("go");
                    ddl.Append($"alter table {constraint.Table.SqlName} add ");
                    Format(ddl, constraint);
                    ddl.AppendLine("go");
                    break;
                case DDLType.Drop:
                    ddl.AppendLine($"alter table {constraint.Table.SqlName} drop constraint {constraint.Name};");
                    break;
            }
        }

        public virtual void Format(StringBuilder ddl, DBConstraint constraint)
        {
            if (constraint.Type == DBConstaintType.Primary)
            {
                ddl.AppendFormat("constraint {0} primary key ({1})", constraint.Name, constraint.Columns.Names);
            }
            if (constraint.Type == DBConstaintType.Unique)
            {
                ddl.AppendFormat("constraint {0} unique ({1})", constraint.Name, constraint.Columns.Names);
            }
            if (constraint.Type == DBConstaintType.Default)
            {
                ddl.AppendFormat("constraint {0} default {1}", constraint.Name, constraint.Value);
            }
            if (constraint.Type == DBConstaintType.Check)
            {
                ddl.AppendFormat("constraint {0} check ({1})", constraint.Name, constraint.Value);
            }
            if (constraint.Type == DBConstaintType.Foreign)
            {
                Format(ddl, (DBForeignKey)constraint);
            }
            ddl.AppendLine();
        }

        public virtual void Format(StringBuilder ddl, DBForeignKey constraint)
        {
            DBTable refer = constraint.ReferenceTable;
            if (refer is IDBVirtualTable)
                refer = ((IDBVirtualTable)refer).BaseTable;
            ddl.AppendFormat(" constraint {0} foreign key ({1}) references {2}({3})",
                             constraint.Name,
                             constraint.Column.Name,
                             refer.Name,
                             constraint.Reference.Name);
        }

        public virtual void Format(StringBuilder ddl, DBIndex index, DDLType ddlType)
        {
            switch (ddlType)
            {
                case DDLType.Create:
                    ddl.AppendLine($"create {(index.Unique ? "unique " : string.Empty)}index {index.Name} on {index.Table.SqlName}({index.Columns.Names})");
                    break;
                case DDLType.Alter:
                    Format(ddl, index, DDLType.Drop);
                    ddl.AppendLine("go");
                    Format(ddl, index, DDLType.Create);
                    ddl.AppendLine("go");
                    break;
                case DDLType.Drop:
                    ddl.AppendLine($"drop index {index.OldName ?? index.Name}");
                    break;
            }
        }

        public virtual void Format(StringBuilder ddl, DBTable table, DDLType ddlType, bool constraints = true, bool indexes = true)
        {
            ddl.AppendLine($"-- -Generate {ddlType} for table {table.Name}");
            switch (ddlType)
            {
                case DDLType.Create:
                    if (table.Type == DBTableType.View)
                    {
                        ddl.AppendLine($"create view {table.SqlName} as");
                        ddl.Append(table.Query);
                    }
                    else if (table.Type == DBTableType.Table)
                    {
                        ddl.AppendLine($"create table {table.SqlName} (");
                        foreach (DBColumn column in table.Columns)
                        {
                            if (column.ColumnType == DBColumnTypes.Default)
                            {
                                Format(ddl, column);
                                ddl.AppendLine();
                                ddl.Append(",");
                            }
                        }
                        ddl.Length -= 1;
                        if (constraints)
                        {
                            foreach (var constraint in table.Constraints)
                            {
                                ddl.Append(",");
                                Format(ddl, constraint);
                            }
                            foreach (var relation in table.Foreigns)
                            {
                                ddl.Append(",");
                                Format(ddl, relation);
                            }
                        }
                        ddl.AppendLine(")");
                        if (indexes)
                        {
                            ddl.AppendLine("go");
                            foreach (var item in table.Indexes)
                            {
                                Format(ddl, item, DDLType.Create);
                                ddl.AppendLine("go");
                            }
                        }
                    }
                    break;
                case DDLType.Alter:
                    Format(ddl, table, DDLType.Drop);
                    ddl.AppendLine("go");
                    Format(ddl, table, DDLType.Create, constraints, indexes);
                    ddl.AppendLine("go");
                    break;
                case DDLType.Drop:
                    foreach (var item in table.Indexes)
                    {
                        Format(ddl, item, DDLType.Drop);
                        ddl.AppendLine("go");
                    }
                    foreach (var item in table.Foreigns)
                    {
                        Format(ddl, item, DDLType.Drop);
                        ddl.AppendLine("go");
                    }
                    foreach (var item in table.Constraints)
                    {
                        Format(ddl, item, DDLType.Drop);
                        ddl.AppendLine("go");
                    }
                    ddl.AppendLine($"drop {(table.Type == DBTableType.View ? "view" : "table")} {table.OldName ?? table.Name}");
                    break;
            }
        }

        public virtual void Format(StringBuilder ddl, DBSchema schema, DDLType ddlType)
        {
            if (ddlType == DDLType.Create)
            {
                var ifexist = schema.Connection.System == DBSystem.MySql
                                        ? "if not exists" : string.Empty;
                ddl.AppendLine($"create database {ifexist} {schema.DataBase};");
            }
            else if (ddlType == DDLType.Drop)
            {
                var ifexist = schema.Connection.System == DBSystem.Postgres
                                        ? "if exists" : string.Empty;
                ddl.AppendLine($"drop database {ifexist} {schema.DataBase};");
            }
        }

        public virtual void Format(StringBuilder ddl, DBSchema schema)
        {
            foreach (var table in schema.Tables)
            {
                Format(ddl, table, DDLType.Create, schema.Connection.System == DBSystem.SQLite, false);
                ddl.AppendLine("go");
            }

            if (schema.Connection.System != DBSystem.SQLite)
            {
                foreach (var constraint in schema.GetConstraints())
                {
                    Format(ddl, constraint, DDLType.Create);
                    ddl.AppendLine("go");
                }

                foreach (var foreign in schema.GetForeigns())
                {
                    Format(ddl, foreign, DDLType.Create);
                    ddl.AppendLine("go");
                }
            }

            foreach (var sequence in schema.Sequences)
            {
                Format(ddl, sequence, DDLType.Create);
                ddl.AppendLine("go");
            }

            foreach (var index in schema.GetIndexes())
            {
                Format(ddl, index, DDLType.Create);
                ddl.AppendLine("go");
            }

            foreach (var procedure in schema.Procedures)
            {
                if (procedure.ProcedureType == ProcedureTypes.StoredProcedure
                   || procedure.ProcedureType == ProcedureTypes.StoredFunction)
                {
                    Format(ddl, procedure, DDLType.Create);
                    ddl.AppendLine("go");
                }
            }
        }

        public string FormatInsert(DBItem row)
        {
            var command = new StringBuilder();
            FormatInsert(command, row.Table, row);
            return command.ToString();
        }

        public virtual void FormatInsert(StringBuilder command, DBTable table, DBItem row = null)
        {
            var id = $"{ParameterPrefix}{table.PrimaryKey?.Name}";
            if (table.PrimaryKey != null && row != null && row.PrimaryId == null)
            {
                id = SequenceInline(table.Sequence);
            }
            FormatInsert(command, table, id, row);
        }

        public virtual void FormatInsert(StringBuilder command, DBTable table, string id, DBItem row = null)
        {
            command.AppendFormat("insert into {0}(", table.SqlName);
            foreach (var column in table.Columns)
            {
                if (column.ColumnType == DBColumnTypes.Default && (column.Keys & DBColumnKeys.Increment) != DBColumnKeys.Increment)
                {
                    command.Append(column.SqlName);
                    command.Append(", ");
                }
            }
            command.Length -= 2;
            command.Append(")\n    values(");
            foreach (var column in table.Columns)
            {
                if (column.ColumnType != DBColumnTypes.Default || (column.Keys & DBColumnKeys.Increment) == DBColumnKeys.Increment)
                    continue;
                if (row == null)
                {
                    if (column.IsPrimaryKey && !string.IsNullOrEmpty(id))
                    {
                        command.Append(id);
                    }
                    else
                    {
                        command.Append(ParameterPrefix);
                        command.Append(column.Name);
                    }
                }
                else
                {
                    if (column.IsPrimaryKey && row.PrimaryId == null && table.Sequence != null)
                    {
                        command.Append(id);
                    }
                    else if (row[column] == null && column.DefaultValue != null)
                    {
                        command.Append(column.DefaultValue);
                    }
                    else
                    {
                        command.Append(DBService.FormatToSqlText(row[column]));
                    }
                }
                command.Append(", ");
            }
            command.Length -= 2;
            command.Append(")");
        }

        public abstract void FormatInsertSequence(StringBuilder command, DBTable table, DBItem row);

        public virtual void FormatUpdate(StringBuilder command, DBTable table, DBItem row, IList<DBColumn> columns)
        {
            bool flag = false;
            command.AppendFormat("update {0} set ", table.SqlName);
            foreach (DBColumn column in columns)
            {
                if (column.ColumnType == DBColumnTypes.Default && !column.IsPrimaryKey)
                {
                    command.AppendFormat("{0}={1}, ", column.SqlName, row == null ? ParameterPrefix + column.Name : DBService.FormatToSqlText(row[column]));
                    flag = true;
                }
            }
            if (flag)
            {
                command.Remove(command.Length - 2, 2);
                command.AppendFormat(" where {0}={1}", table.PrimaryKey.SqlName, row == null ? ParameterPrefix + table.PrimaryKey.Name : DBService.FormatToSqlText(row.PrimaryId));
            }
        }

        public virtual void FormatDelete(StringBuilder command, DBTable table, DBItem row)
        {
            var prefix = ParameterPrefix;
            if (table.PrimaryKey != null)
            {
                command.AppendFormat("delete from {0} where {1}={2}",
                    table.SqlName,
                    table.PrimaryKey.Name,
                    row == null ? prefix + table.PrimaryKey.Name : DBService.FormatToSqlText(row.PrimaryId));
            }
        }

        public virtual string FormatCommand(DBTable table, DBCommandTypes type, DBItem row, IList<DBColumn> columns = null)
        {
            var command = new StringBuilder();
            if (columns == null)
                columns = table.Columns;
            switch (type)
            {
                case DBCommandTypes.Insert:
                    FormatInsert(command, table, row);
                    break;
                case DBCommandTypes.InsertSequence:
                    FormatInsertSequence(command, table, row);
                    break;
                case DBCommandTypes.Delete:
                    FormatDelete(command, table, row);
                    break;
                case DBCommandTypes.Update:
                    FormatUpdate(command, table, row, columns);
                    break;
            }
            return command.ToString();
        }

        public string FormatInsert(DBTable table, bool fill)
        {
            IList rows = fill ? table.LoadItems().ToList() : table.SelectItems("").ToList();
            if (table.GroupKey != null)
            {
                ListHelper.QuickSort(rows, new TreeComparer(null));
            }
            return DMLInsert(rows, "");
        }

        public string DMLInsert(IEnumerable rows, string sep)
        {
            var builder = new StringBuilder();
            foreach (DBItem row in rows)
            {
                FormatInsert(builder, row.Table, row);
                builder.AppendLine(sep);
            }
            return builder.ToString();
        }

        public virtual void WriteValue(DBColumn column, object value, IDataParameter parameter, IDbConnection connection)
        {
            if (value == null)
            {
                value = DBNull.Value;
            }
            else if (column.DataType.IsEnum)
            {
                value = (int)value;
            }
            parameter.Value = value;
        }

        public virtual void ReadValue(DBItem item, IDataParameter parameter)
        {
            item[parameter.SourceColumn] = parameter.Value;
        }

        public virtual void UploadCommand(DBItem item, IDbCommand command)
        {
            foreach (IDbDataParameter parameter in command.Parameters)
            {
                if (parameter.Value != null)
                {
                    if (parameter.Direction == ParameterDirection.InputOutput || parameter.Direction == ParameterDirection.Output)
                    {
                        ReadValue(item, parameter);
                    }
                }
            }
        }

        public virtual object ReadValue(DBColumn column, object value)
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
                else
                {
                    throw new InvalidCastException($"From {from} to {temp}");
                    //value = Convert.ChangeType(value, temp, CultureInfo.InvariantCulture);
                }
            }
            return value;
        }

    }
}
