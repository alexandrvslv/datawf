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
using System.Collections.Generic;
using System.Data;

namespace DataWF.Data
{
    public class DBCommand
    {
        public static DBCommand Build(DBTable table, string procName, DBCommandTypes type, IEnumerable<DBColumn> columns = null)
        {
            if (table.PrimaryKey == null && (type == DBCommandTypes.Delete || type == DBCommandTypes.Update))
                return null;
            if (columns == null)
                columns = table.Columns;
            string commandText = procName;
            string prefix = table.Schema.System.ParameterPrefix;

            CommandType ctype = CommandType.StoredProcedure;
            if (string.IsNullOrEmpty(procName))
            {
                ctype = CommandType.Text;
                commandText = table.Schema.System.FormatCommand(table, type, null, columns);
            }
            var command = new DBCommand(commandText, ctype);

            if (type == DBCommandTypes.Delete)
            {
                command.Parameters.Add(new DBCommandParameter(table.PrimaryKey, prefix)
                {
                    Direction = ParameterDirection.Input
                });
            }
            else
            {
                DBCommandParameter paramId = null;
                if (table.PrimaryKey != null)
                {
                    paramId = new DBCommandParameter(table.PrimaryKey, prefix)
                    {
                        Direction = ParameterDirection.Input
                    };

                    if (type == DBCommandTypes.Insert || type == DBCommandTypes.InsertSequence)
                    {
                        command.Parameters.Add(paramId);
                    }
                    //else if (type == DBCommandTypes.InsertSequence)
                    //{
                    //command.Parameters.Add(paramId);
                    //paramId.Direction = ParameterDirection.InputOutput;
                    //}
                }
                foreach (var column in columns)
                {
                    if (column.ColumnType == DBColumnTypes.Default && column != table.PrimaryKey)
                    {
                        var prm = new DBCommandParameter(column, prefix);
                        if (ctype == CommandType.Text)
                            prm.Direction = ParameterDirection.Input;
                        command.Parameters.Add(prm);
                    }
                }
                if (paramId != null && type == DBCommandTypes.Update)
                {
                    command.Parameters.Add(paramId);
                }
            }
            return command;
        }

        public static IDbCommand CloneCommand(IDbCommand command, string text)
        {
            var clone = command.Connection.CreateCommand();
            if (command.Transaction != null)
                clone.Transaction = command.Transaction;
            clone.CommandTimeout = command.CommandTimeout;
            clone.CommandText = text;
            foreach (IDbDataParameter p in command.Parameters)
            {
                var clonep = clone.CreateParameter();
                clonep.Direction = p.Direction;
                clonep.ParameterName = p.ParameterName;
                clonep.DbType = p.DbType;
                clonep.Value = p.Value;
                clone.Parameters.Add(clonep);
            }
            return clone;
        }

        public DBCommand(IDbCommand value) : this(value.CommandText, value.CommandType)
        {
            for (int i = 0; i < value.Parameters.Count; i++)
            {
                Parameters.Add(new DBCommandParameter((IDbDataParameter)value.Parameters[i], i));
            }
        }

        public DBCommand(string text, CommandType type)
        {
            Text = text;
            Type = type;
        }

        public List<DBCommandParameter> Parameters { get; private set; } = new List<DBCommandParameter>();

        public string Text { get; set; }

        public CommandType Type { get; set; }

        public void FillCommand(IDbCommand command, DBItem row)
        {
            foreach (var param in Parameters)
            {
                var dbParameter = command.Parameters.Count >= Parameters.Count ? command.Parameters[param.Name] as IDataParameter : null;
                if (dbParameter == null)
                {
                    dbParameter = command.CreateParameter();
                    dbParameter.ParameterName = param.Name;
                    dbParameter.SourceColumn = param.ColumnName;
                    dbParameter.Direction = param.Direction;
                    if (param.DbType != 0)
                        dbParameter.DbType = param.DbType;

                    command.Parameters.Add(dbParameter);
                }
                param.Column.Schema.System.FillParameter(command, dbParameter, param.Column.GetParameterValue(row), param.Column);
            }
        }
    }
}