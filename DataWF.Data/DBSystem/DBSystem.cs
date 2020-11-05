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
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public abstract partial class DBSystem
    {
        public static readonly DBSystem MSSql = new DBSystemMSSql();
        public static readonly DBSystem MySql = new DBSystemMySql();
        public static readonly DBSystem Oracle = new DBSystemOracle();
        public static readonly DBSystem Postgres = new DBSystemPostgres();
        public static readonly DBSystem SQLite = new DBSystemSQLite();
        public static readonly DBSystem Default = new DBSystemDefault();

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

        public IDbCommand CreateCommand(DBConnection connection, string query = null, CommandType commandType = CommandType.Text)
        {
            IDbCommand command = GetFactory().CreateCommand();
            command.CommandTimeout = connection.TimeOut;
            command.CommandType = commandType;
            if (query != null)
            {
                command.CommandText = query;
            }
            return command;
        }


        public virtual IDataParameter CreateParameter(IDbCommand command, string name, object value, DBColumn column)
        {
            IDataParameter parameter = null;
            foreach (IDataParameter param in command.Parameters)
                if (param.ParameterName == name)
                {
                    parameter = param;
                    break;
                }
            if (parameter == null)
            {
                parameter = command.CreateParameter();
                parameter.ParameterName = name;
                command.Parameters.Add(parameter);
            }
            parameter.Direction = ParameterDirection.Input;

            FillParameter(command, parameter, value, column);
            return parameter;
        }

        public static IEnumerable<DBSystem> GetSystems()
        {
            return new[] { DBSystem.MSSql, DBSystem.MySql, DBSystem.Oracle, DBSystem.Postgres, DBSystem.SQLite };
        }

        public abstract Task<bool> ReadAsync(IDataReader reader);

        public abstract Stream GetStream(IDataReader reader, int column);

        public virtual IEnumerable<DBTableInfo> GetTablesInfo(DBConnection connection, string schemaName = null, string tableName = null)
        {
            var tableFilter = !string.IsNullOrEmpty(tableName) ? $" and table_name = '{tableName}'" : string.Empty;
            var schemaFilter = !string.IsNullOrEmpty(schemaName) ? $" where table_schema = '{schemaName}'{tableFilter}" : string.Empty;
            QResult list = connection.ExecuteQResult($"select * from information_schema.tables{schemaFilter} order by table_name");
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
                GetColumnsInfo(connection, table);
                GetConstraintInfo(connection, table);
                yield return table;
            }
        }

        public virtual void GetColumnsInfo(DBConnection connection, DBTableInfo tableInfo)
        {
            var query = string.Format("select * from information_schema.columns where table_name='{0}'{1} order by ordinal_position",
                                      tableInfo.Name,
                                      string.IsNullOrEmpty(tableInfo.Schema) ? null : $" and table_schema = '{tableInfo.Schema}'");
            QResult list = connection.ExecuteQResult(query);
            int iName = list.GetIndex("column_name");
            int iType = list.GetIndex("data_type");
            int iPrec = list.GetIndex("numeric_precision");
            int iScale = list.GetIndex("numeric_scale");
            int iLeng = list.GetIndex("character_maximum_length");
            int iNull = list.GetIndex("is_nullable");
            int iDefault = list.GetIndex("column_default");

            foreach (object[] item in list.Values)
            {
                tableInfo.Columns.Add(new DBColumnInfo()
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
        }

        public virtual void GetConstraintInfo(DBConnection connection, DBTableInfo tableInfo)
        {
            var query = $@"select a.constraint_schema
,a.constraint_name
,a.table_schema
,a.table_name
,a.constraint_type
,b.column_name
from information_schema.table_constraints a
  left join information_schema.key_column_usage b
     on b.constraint_schema = a.constraint_schema
	 and b.constraint_name = a.constraint_name
where a.table_name='{tableInfo.Name}'{(string.IsNullOrEmpty(tableInfo.Schema) ? null : $" and a.table_schema = '{tableInfo.Schema}'")}";
            QResult list = connection.ExecuteQResult(query);
            int iName = list.GetIndex("constraint_name");
            int iType = list.GetIndex("constraint_type");
            int iColu = list.GetIndex("column_name");

            foreach (object[] item in list.Values)
            {
                tableInfo.Constraints.Add(new DBConstraintInfo()
                {
                    Name = item[iName].ToString(),
                    Type = item[iType].ToString(),
                    Column = item[iColu].ToString()
                });
            }
        }

        public virtual async Task DeleteBLOB(long id, DBTransaction transaction)
        {
            var command = transaction.AddCommand($"delete from {FileData.DBTable.Name} where {FileData.IdKey.Name} = {ParameterPrefix}{FileData.IdKey.Name}");
            CreateParameter(command, $"{ParameterPrefix}{FileData.IdKey.Name}", id, FileData.IdKey);
            await ExecuteQueryAsync(command, DBExecuteType.Scalar, CommandBehavior.Default);
        }

        public virtual async Task<Stream> GetBLOB(long id, DBTransaction transaction, int bufferSize = 81920)
        {
            var command = transaction.AddCommand($"select {FileData.IdKey.Name}, {FileData.DataKey.Name} from {FileData.DBTable.Name} where {FileData.IdKey.Name} = {ParameterPrefix}{FileData.IdKey.Name}");
            CreateParameter(command, $"{ParameterPrefix}{FileData.IdKey.Name}", id, FileData.IdKey);
            transaction.Reader = (IDataReader)await transaction.ExecuteQueryAsync(command, DBExecuteType.Reader, CommandBehavior.SequentialAccess);
            if (await transaction.ReadAsync())
            {
                return transaction.GetStream(1);
            }
            throw new Exception("No Data Found!");
        }

        public virtual async Task<long> SetBLOB(Stream value, DBTransaction transaction)
        {
            var result = FileData.DBTable.Sequence.GetNext(transaction);
            await SetBLOB(result, value, transaction);
            return result;
        }

        public virtual async Task SetBLOB(long id, Stream value, DBTransaction transaction)
        {
            var command = transaction.AddCommand($@"insert into {FileData.DBTable.Name} ({FileData.IdKey.Name}, {FileData.DataKey.Name}) values ({ParameterPrefix}{FileData.IdKey.Name}, {ParameterPrefix}{FileData.DataKey.Name});");
            CreateParameter(command, $"{ParameterPrefix}{FileData.IdKey.Name}", id, FileData.IdKey);
            CreateParameter(command, $"{ParameterPrefix}{FileData.DataKey.Name}", await Helper.GetBytesAsync(value), FileData.DataKey);//Double buffering!!!
            await transaction.ExecuteQueryAsync(command);
        }

        public virtual void CreateDatabase(DBSchema schema, DBConnection connection)
        {
            //DropDatabase(schema);

            var ddl = new StringBuilder();
            Format(ddl, schema, DDLType.Create);
            connection.ExecuteGoQuery(ddl.ToString(), true);

            if (string.IsNullOrEmpty(connection.DataBase))
            {
                connection.DataBase = schema.Name;
            }
            CreateSchema(schema, connection);
        }

        public virtual void CreateSchema(DBSchema schema, DBConnection connection)
        {
            var ddl = new StringBuilder();
            Format(ddl, schema);
            connection.ExecuteGoQuery(ddl.ToString(), true);
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

        public override string ToString()
        {
            return Name ?? base.ToString();
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

        public virtual void DropDatabase(DBSchema schema)
        {
            try
            {
                schema.Connection.ExecuteQuery(schema.FormatSql(DDLType.Drop), true, DBExecuteType.NoReader);
            }
            catch (Exception ex) { Helper.OnException(ex); }
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

        public virtual void Format(StringBuilder ddl, DBColumn column, DDLType ddlType, bool inlineConstraints = false)
        {
            var constraints = column.Table.Constraints.GetByColumn(column)
                .Union(column.Table.Foreigns.GetByColumn(column)).ToList();

            switch (ddlType)
            {
                case DDLType.Create:
                    FormatCreate(ddl, column);
                    if (constraints.Count > 0)
                    {
                        if (inlineConstraints)
                        {
                            foreach (var item in constraints)
                            {
                                Format(ddl, item);
                                ddl.AppendLine();
                            }
                        }
                        else
                        {
                            ddl.AppendLine("go");
                            foreach (var item in constraints)
                            {
                                Format(ddl, item, DDLType.Create);
                                ddl.AppendLine("go");
                            }
                        }
                    }
                    break;
                case DDLType.Alter:
                    if (constraints.Count > 0 && !inlineConstraints)
                    {
                        foreach (var item in constraints)
                        {
                            Format(ddl, item, DDLType.Drop);
                            ddl.AppendLine("go");
                        }
                    }
                    FormatAlter(ddl, column);
                    if (constraints.Count > 0 && !inlineConstraints)
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
                    if (constraints.Count > 0 && !inlineConstraints)
                    {
                        foreach (var item in constraints)
                        {
                            Format(ddl, item, DDLType.Drop);
                            ddl.AppendLine("go");
                        }
                        ddl.AppendLine("go");
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
            if (constraint.Type == DBConstraintType.Primary)
            {
                ddl.AppendFormat("constraint {0} primary key ({1})", constraint.Name, constraint.Columns.Names);
            }
            if (constraint.Type == DBConstraintType.Unique)
            {
                ddl.AppendFormat("constraint {0} unique ({1})", constraint.Name, constraint.Columns.Names);
            }
            if (constraint.Type == DBConstraintType.Default)
            {
                ddl.AppendFormat("constraint {0} default {1}", constraint.Name, constraint.Value);
            }
            if (constraint.Type == DBConstraintType.Check)
            {
                ddl.AppendFormat("constraint {0} check ({1})", constraint.Name, constraint.Value);
            }
            if (constraint.Type == DBConstraintType.Foreign)
            {
                Format(ddl, (DBForeignKey)constraint);
            }
            ddl.AppendLine();
        }

        public virtual void PrepareStatements(IDbCommand command)
        {
        }

        public abstract Task<object> ExecuteQueryAsync(IDbCommand command, DBExecuteType type, CommandBehavior behavior);

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

        public virtual string FormatCreateView(string name)
        {
            return "create view " + name + " as";
        }

        public virtual void Format(StringBuilder ddl, IDBVirtualTable virtualTable, DDLType ddlType)
        {
            var table = virtualTable as DBTable;
            if (ddlType == DDLType.Create)
            {
                ddl.AppendLine(FormatCreateView(table.Name));
                ddl.Append("select ");
                foreach (var column in table.Columns)
                {
                    if (column.ColumnType == DBColumnTypes.Default)
                        ddl.Append($"a.{column.Name} as {column.Name}");
                    else if (column.ColumnType == DBColumnTypes.Query)
                        ddl.Append($"({column.Query}) as {column.Name}");
                    else
                        continue;
                    ddl.Append(", ");
                }
                ddl.Length -= 2;
                ddl.AppendLine();
                ddl.AppendLine($"from {table.SqlName} a where {table.Query};");
            }
        }

        public virtual string FormatException(Exception exception, DBTable table, DBItem item)
        {
            var sb = new StringBuilder();
            while (exception != null)
            {
                sb.AppendLine(exception.Message);
                exception = exception.InnerException;
            }
            return sb.ToString();
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
                                if (constraint.Column.ColumnType == DBColumnTypes.Default)
                                {
                                    ddl.AppendLine(",");
                                    Format(ddl, constraint);
                                }
                            }
                            foreach (var relation in table.Foreigns)
                            {
                                if (relation.Column.ColumnType == DBColumnTypes.Default)
                                {
                                    ddl.AppendLine(",");
                                    Format(ddl, relation);
                                }
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
                if (table is IDBVirtualTable)
                    continue;
                Format(ddl, table, DDLType.Create, schema.Connection.System == DBSystem.SQLite, false);
                ddl.AppendLine("go");
            }

            if (schema.Connection.System != DBSystem.SQLite)
            {
                foreach (var constraint in schema.GetConstraints())
                {
                    if (constraint.Table is IDBVirtualTable || constraint.Column.ColumnType != DBColumnTypes.Default)
                        continue;
                    Format(ddl, constraint, DDLType.Create);
                    ddl.AppendLine("go");
                }

                foreach (var foreign in schema.GetForeigns())
                {
                    if (foreign.Table is IDBVirtualTable || foreign.Column.ColumnType != DBColumnTypes.Default)
                        continue;
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
                if (index.Table is IDBVirtualTable)
                    continue;
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
                        command.Append(FormatText(row[column]));
                    }
                }
                command.Append(", ");
            }
            command.Length -= 2;
            command.Append(")");
        }

        public abstract void FormatInsertSequence(StringBuilder command, DBTable table, DBItem row);

        public virtual void FormatUpdate(StringBuilder command, DBTable table, DBItem row, IEnumerable<DBColumn> columns)
        {
            bool flag = false;
            command.AppendFormat("update {0} set ", table.SqlName);
            foreach (DBColumn column in columns)
            {
                if (column.ColumnType == DBColumnTypes.Default && !column.IsPrimaryKey)
                {
                    command.AppendFormat("{0}={1}, ", column.SqlName, row == null ? ParameterPrefix + column.Name : FormatText(row[column]));
                    flag = true;
                }
            }
            if (flag)
            {
                command.Length -= 2;
                command.AppendFormat(" where {0}={1}", table.PrimaryKey.SqlName, row == null ? ParameterPrefix + table.PrimaryKey.Name : FormatText(row.PrimaryId));
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
                    row == null ? prefix + table.PrimaryKey.Name : FormatText(row.PrimaryId));
            }
        }

        public virtual string FormatCommand(DBTable table, DBCommandTypes type, DBItem row, IEnumerable<DBColumn> columns = null)
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
            var rows = fill ? table.LoadItems().ToList() : table.SelectItems("").ToList();
            if (table.GroupKey != null)
            {
                ListHelper.QuickSort(rows, new TreeComparer<DBGroupItem>());
            }
            return DMLInsert(rows, "");
        }

        public string DMLInsert(IEnumerable<DBItem> rows, string sep)
        {
            var builder = new StringBuilder();
            foreach (var row in rows)
            {
                FormatInsert(builder, row.Table, row);
                builder.AppendLine(sep);
            }
            return builder.ToString();
        }

        public virtual object FillParameter(IDbCommand command, IDataParameter parameter, object value, DBColumn column)
        {
            if (value == null)
            {
                value = DBNull.Value;
            }
            else if (column != null && column.DataType.IsEnum)
            {
                value = (int)value;
            }
            else if (value is IByteSerializable serializable)
            {
                value = serializable.Serialize();
            }
            parameter.Value = value;
            return value;
        }

        public virtual uint GetUInt(IDataReader reader, int index)
        {
            return (uint)reader.GetValue(index);
        }

        public virtual TimeSpan GetTimeSpan(IDataReader reader, int index)
        {
            return (TimeSpan)reader.GetValue(index);
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

        public virtual string FormatQColumn(DBColumn column, string tableAlias)
        {
            if (column.ColumnType == DBColumnTypes.Internal
                || column.ColumnType == DBColumnTypes.Expression
                || column.ColumnType == DBColumnTypes.Code)
                return string.Empty;
            else if (column.ColumnType == DBColumnTypes.Query && column.Table.Type != DBTableType.View)
                return string.Format("({0}) as \"{1}\"", column.Query, column.Name);
            else
                return $"{tableAlias}{(tableAlias != null ? "." : string.Empty)}{column.SqlName}";
        }

        public virtual string FormatQTable(DBTable table, string alias)
        {
            var schema = table.Schema?.Connection?.Schema;
            if (!string.IsNullOrEmpty(schema))
            {
                return $"{schema}.{table.SqlName} {alias}";
            }
            return $"{table.SqlName} {alias}";
        }

        public virtual string FormatText(object value)
        {
            if (value == null)
                return "null";

            if (value is DBItem)
                value = ((DBItem)value).PrimaryId;
            else if (value.GetType().IsEnum)
                value = (int)value;

            if (value == null)
                return "null";
            else if (value is string)
                return "'" + ((string)value).Replace("'", "''") + "'";
            else if (value is DateTime)
            {
                if (((DateTime)value).TimeOfDay == TimeSpan.Zero)
                    return "'" + ((DateTime)value).ToString("yyyy-MM-dd") + "'";
                else
                    return "'" + ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff") + "'";
            }
            else if (value is byte[])
            {
                var sBuilder = new StringBuilder();
                var data = (byte[])value;
                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                sBuilder.Append("0x");
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                return sBuilder.ToString();
            }
            else
                return value.ToString().Replace(",", ".");
        }

        public virtual Stream ReadSequential(DBItem item, DBColumn column, DBTransaction transaction)
        {
            var command = transaction.AddCommand(item.Table.CreatePrimaryKeyCommmand(item.PrimaryId, new[] { column }));
            transaction.Reader = (IDataReader)transaction.ExecuteQuery(command, DBExecuteType.Reader, CommandBehavior.SequentialAccess);
            if (transaction.Reader.Read())
            {
                return new DataReaderStream(transaction.Reader, false);
            }
            return null;
        }

        public virtual void ReadSequential(DBItem item, DBColumn column, Stream stream, int bufferSize = 81920)
        {
            using (var transaction = new DBTransaction(item.Table.Connection, null, true))
            {
                ReadSequential(item, column, stream, transaction, bufferSize);
            }
        }

        public virtual void ReadSequential(DBItem item, DBColumn column, Stream stream, DBTransaction transaction, int bufferSize = 81920)
        {
            var command = transaction.AddCommand(item.Table.CreatePrimaryKeyCommmand(item.PrimaryId, new[] { column }));
            using (transaction.Reader = (IDataReader)transaction.ExecuteQuery(command, DBExecuteType.Reader, CommandBehavior.SequentialAccess))
            {
                if (transaction.Reader.Read())
                {
                    if (transaction.Reader.IsDBNull(0))
                    {
                        throw new Exception("No Data Found!");
                    }
                    using (var dbStream = transaction.GetStream(1))
                    {
                        dbStream.CopyTo(stream, bufferSize);
                    }
                    //var buffer = new byte[bufferSize];
                    //int position = 0;
                    //int readed;
                    //while ((readed = (int)transaction.Reader.GetBytes(0, position, buffer, 0, bufferSize)) > 0)
                    //{
                    //    stream.Write(buffer, 0, readed);
                    //    position += readed;
                    //}
                }
                transaction.Reader.Close();
            }
            transaction.Reader = null;
            stream.Position = 0;
        }

    }
}
