/*
 DBCommand.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>
 
  

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

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
                var param = new DBCommandParameter(table.PrimaryKey, prefix);
                param.Direction = ParameterDirection.Input;
                command.Parameters.Add(param);
            }
            else
            {
                DBCommandParameter paramId = null;
                if (table.PrimaryKey != null)
                {
                    paramId = new DBCommandParameter(table.PrimaryKey, prefix);
                    paramId.Direction = ParameterDirection.Input;

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
                param.Column.Schema.System.WriteValue(param.Column, row[param.Column], dbParameter, command.Connection);
            }
        }
    }
}