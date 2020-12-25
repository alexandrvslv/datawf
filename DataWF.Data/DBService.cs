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
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public delegate object ExecuteDelegate(ExecuteEventArgs arg);

    public delegate void DBExecuteDelegate(DBExecuteEventArg arg);

    public delegate void DBItemEditEventHandler(DBItemEventArgs arg);

    public class ExecuteEventArgs : CancelEventArgs
    {
        public ExecuteEventArgs(DBSchema schema, DBTable table, string query)
        {
            Schema = schema;
            Table = table;
            Query = query;
        }

        public DBSchema Schema { get; set; }

        public DBTable Table { get; set; }

        public string Query { get; set; }
    }

    public class DBExecuteEventArg : EventArgs
    {
        public DBExecuteEventArg()
        {
        }

        public object Rezult { get; set; }

        public TimeSpan Time { get; set; }

        public string Query { get; set; }

        public DBExecuteType Type { get; set; }
    }

    /// <summary>
    /// Service for connection
    /// </summary>
    public static class DBService
    {
        //private static object[] fabricparam = new object[1];
        //private static Type[] param = new Type[] { typeof(DBItem) };
        //private static Type[] param2 = new Type[] { typeof(DBRow) };
        public static char[] DotSplit = { '.' };
        private static readonly DBConnectionList connections = new DBConnectionList();
        private static readonly DBSchemaList schems = new DBSchemaList();

        public static ActionInvoker<DBSchemaChange, DBSchemaItem> DBSchemaChangeItemInvoker = new ActionInvoker<DBSchemaChange, DBSchemaItem>(
            nameof(DBSchemaChange.Item), p => p.Item, (p, v) => p.Item = v);
        public static SelectableList<DBSchemaChange> Changes = new SelectableList<DBSchemaChange>();

        public static event EventHandler<DBSchemaChangedArgs> DBSchemaChanged;

        private static readonly HashSet<Func<DBTransaction, ValueTask>> transactionCommitHandler = new HashSet<Func<DBTransaction, ValueTask>>();
        private static readonly HashSet<Func<DBItemEventArgs, ValueTask>> itemAcceptHandler = new HashSet<Func<DBItemEventArgs, ValueTask>>();
        private static readonly HashSet<Func<DBItemEventArgs, ValueTask>> itemRejectHandler = new HashSet<Func<DBItemEventArgs, ValueTask>>();
        private static readonly HashSet<Func<DBItemEventArgs, ValueTask>> itemUpdatedHandler = new HashSet<Func<DBItemEventArgs, ValueTask>>();
        private static readonly HashSet<Func<DBItemEventArgs, ValueTask>> itemLoginingHandler = new HashSet<Func<DBItemEventArgs, ValueTask>>();
        private static readonly HashSet<Func<DBItemEventArgs, ValueTask>> itemUpdatingHandler = new HashSet<Func<DBItemEventArgs, ValueTask>>();

        public static void AddTransactionCommit(Func<DBTransaction, ValueTask> function)
        {
            transactionCommitHandler.Add(function);
        }

        public static void RemoveTransactionCommit(Func<DBTransaction, ValueTask> function)
        {
            transactionCommitHandler.Remove(function);
        }

        internal static void OnTransactionCommit(DBTransaction transaction)
        {
            foreach (var function in transactionCommitHandler)
            {
                _ = function(transaction);
            }
        }

        public static void AddItemUpdating(Func<DBItemEventArgs, ValueTask> function)
        {
            itemUpdatingHandler.Add(function);
        }

        public static void RemoveItemUpdating(Func<DBItemEventArgs, ValueTask> function)
        {
            itemUpdatingHandler.Remove(function);
        }

        internal static void OnUpdating(DBItemEventArgs e)
        {
            foreach (var function in itemUpdatingHandler)
            {
                _ = function(e);
            }
        }

        public static void AddItemLoging(Func<DBItemEventArgs, ValueTask> function)
        {
            itemLoginingHandler.Add(function);
        }

        public static void RemoveItemLoging(Func<DBItemEventArgs, ValueTask> function)
        {
            itemLoginingHandler.Remove(function);
        }

        internal static void OnLogItem(DBItemEventArgs e)
        {
            foreach (var function in itemLoginingHandler)
            {
                _ = function(e);
            }
        }

        public static void AddItemUpdated(Func<DBItemEventArgs, ValueTask> function)
        {
            itemUpdatedHandler.Add(function);
        }

        public static void RemoveItemUpdated(Func<DBItemEventArgs, ValueTask> function)
        {
            itemUpdatedHandler.Remove(function);
        }

        internal static void OnUpdated(DBItemEventArgs e)
        {
            foreach (var function in itemUpdatedHandler)
            {
                _ = function(e);
            }
        }

        public static void AddRowAccept(Func<DBItemEventArgs, ValueTask> function)
        {
            itemAcceptHandler.Add(function);
        }

        public static void RemoveRowAccept(Func<DBItemEventArgs, ValueTask> function)
        {
            itemAcceptHandler.Remove(function);
        }

        internal static void OnAccept(DBItemEventArgs e)
        {
            foreach (var function in itemAcceptHandler)
            {
                _ = function(e);
            }
        }

        public static void AddRowReject(Func<DBItemEventArgs, ValueTask> function)
        {
            itemRejectHandler.Add(function);
        }

        public static void RemoveRowReject(Func<DBItemEventArgs, ValueTask> function)
        {
            itemRejectHandler.Remove(function);
        }

        internal static void OnReject(DBItemEventArgs e)
        {
            foreach (var function in itemRejectHandler)
            {
                _ = function(e);
            }
        }

        public static void Save()
        {
            Save("data.xml");
        }

        public static void Save(string file)
        {
            foreach (var schema in schems)
            {
                schema.Tables.ApplyDefaultSort();
            }
            Serialization.Serialize(connections, "connections.xml");
            Serialization.Serialize(schems, file);
        }

        public static void Load()
        {
            Load("data.xml");
        }

        public static void Load(string file)
        {
            Serialization.Deserialize("connections.xml", connections);
            schems.HandleChanges = false;
            Serialization.Deserialize(file, schems);
            schems.HandleChanges = true;
            Helper.LogWorkingSet("Schema");
            Changes.Clear();
        }

        public static DBConnectionList Connections => connections;

        public static DBSchemaList Schems => schems;

        public static void OnDBSchemaChanged(DBSchemaItem item, DDLType type)
        {
            if (type == DDLType.Default
                || !item.Containers.Any()
                || item.Schema == null
                || !item.Schema.Containers.Any()
                || item.Schema.IsSynchronizing)
                return;
            if (item is IDBTableContent tabled)
            {
                if (tabled.Table is IDBVirtualTable || !tabled.Table.Containers.Any())
                    return;
                if (item is DBColumn column && column.ColumnType != DBColumnTypes.Default)
                    return;
            }
            DBSchemaChange change = null;

            var list = Changes.Select(DBSchemaChangeItemInvoker, CompareType.Equal, item).ToList();

            if (list.Count > 0)
            {
                change = list[0];
                if (change.Change != type)
                {
                    if (change.Change == DDLType.Create && type == DDLType.Alter)
                        return;
                    change = null;
                }
            }

            if (change == null)
            {
                change = new DBSchemaChange() { Item = item, Change = type, Order = Changes.Count };
                Changes.Add(change);
            }
            if (item is DBTable table && type != DDLType.Drop)
            {
                foreach (var column in table.Columns)
                {
                    if (column.ColumnType != DBColumnTypes.Default)
                        continue;
                    OnDBSchemaChanged(column, DDLType.Create);
                }

                foreach (var constraint in table.Constraints)
                {
                    if (constraint.Column?.ColumnType != DBColumnTypes.Default)
                        continue;
                    OnDBSchemaChanged(constraint, DDLType.Create);
                }

                foreach (var foreign in table.Foreigns)
                {
                    if (foreign.Column?.ColumnType != DBColumnTypes.Default)
                        continue;
                    OnDBSchemaChanged(foreign, DDLType.Create);
                }
            }

            DBSchemaChanged?.Invoke(item, new DBSchemaChangedArgs { Item = item, Type = type });
        }

        public static void CommitChanges()
        {
            if (Changes.Count == 0)
                return;
            foreach (var schema in schems)
            {
                var isSqlite = schema.Connection.System == DBSystem.SQLite;
                foreach (var item in GetChanges(schema))
                {
                    string val = item.Generate(isSqlite);
                    if (item.Check && !string.IsNullOrEmpty(val))
                    {
                        CommitChanges(schema, item, val);
                    }
                    item.Item.OldName = null;
                }
            }
            Serialization.Serialize(Changes, $"SchemaDiff_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.xml");
            Changes.Clear();
            Save();
        }

        private static IEnumerable<DBSchemaChange> GetChanges(DBSchema schema)
        {
            var isSqlite = schema.Connection.System == DBSystem.SQLite;
            var chages = Changes.Where(p => p.Item.Schema == schema).ToList();
            chages.Sort((a, b) =>
            {
                if (a.Item is DBTable tableA && !(tableA is IDBVirtualTable))
                {
                    if (b.Item is DBTable tableB && !(tableB is IDBVirtualTable))
                    {
                        return DBTableComparer.Instance.Compare(tableA, tableB, true);
                    }
                    else
                    {
                        return -1;
                    }
                }
                else if (b.Item is DBTable table && !(table is IDBVirtualTable))
                {
                    return 1;
                }
                else if (a.Item is DBColumn columnA)
                {
                    if (b.Item is DBColumn columnB)
                    {
                        return a.Order.CompareTo(b.Order);
                    }
                    else
                    {
                        return -1;
                    }
                }
                else if (b.Item is DBColumn)
                {
                    return 1;
                }
                return a.Order.CompareTo(b.Order);
            });
            foreach (var item in chages)
            {
                if (item.Item is DBConstraint && isSqlite)
                {
                    continue;
                }
                yield return item;
            }
        }

        public static void CommitChanges(DBSchema schema, DBSchemaChange item, string commands)
        {
            foreach (var command in schema.Connection.SplitGoQuery(commands))
            {
                try
                {
                    Console.WriteLine($"sqlinfo: {item}");
                    Console.WriteLine(command);
                    schema.Connection.ExecuteQuery(command);
                    Console.WriteLine($"sqlinfo: success");
                }
                catch (Exception ex)
                {
                    if (ex is Npgsql.PostgresException postgresException)
                    {
                        if (string.Equals(postgresException.SqlState, "42701", StringComparison.Ordinal)
                           || string.Equals(postgresException.SqlState, "42P07", StringComparison.Ordinal)
                           || string.Equals(postgresException.SqlState, "42710", StringComparison.Ordinal)
                           || string.Equals(postgresException.SqlState, "42P16", StringComparison.Ordinal))
                        {
                            Console.WriteLine($"sqlinfo: skip already exist");
                            continue;
                        }
                    }
                    if (ex is SqliteException sqliteException)
                    {
                        if (sqliteException.SqliteErrorCode == 1
                            || (sqliteException.SqliteErrorCode == 19 && ex.Message.IndexOf("db_sequence.name", StringComparison.OrdinalIgnoreCase) > -1))
                        {
                            Console.WriteLine($"sqlinfo: skip already exist");
                            continue;
                        }
                    }
                    //TODO MSSql, MySql, Oracle, Sqlite
                    if (ex.Message.IndexOf("already exist", StringComparison.OrdinalIgnoreCase) >= 0
                        || ex.Message.IndexOf("duplicate column name", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine($"sqlinfo: skip already exist");
                        continue;
                    }
                    throw;
                }
            }
            Console.WriteLine();
        }

        public static string BuildChangesQuery(DBSchema schema)
        {
            var builder = new StringBuilder();
            var isSqlite = schema.Connection.System == DBSystem.SQLite;
            foreach (var item in GetChanges(schema))
            {
                string val = item.Generate(isSqlite);
                if (item.Check && !string.IsNullOrEmpty(val))
                {
                    builder.Append("-- ");
                    builder.AppendLine(item.ToString());
                    builder.AppendLine(val);
                    builder.AppendLine("go");
                    builder.AppendLine();
                }
                item.Item.OldName = null;
                Changes.Remove(item);
            }
            return builder.ToString();
        }

        public static int GetIntValue(object value)
        {
            if (value == null)
                return 0;
            if (value is int)
                return (int)value;
            if (value is int?)
                return ((int?)value).Value;
            return int.TryParse(value.ToString(), out int result) ? result : 0;
        }

        public static void RefreshToString()
        {
            foreach (DBSchema schema in Schems)
            {
                foreach (DBTable table in schema.Tables)
                {
                    foreach (DBItem row in table)
                    {
                        row.cacheToString = string.Empty;
                    }
                }
            }
        }

        public static int CompareDBTable(DBTable x, DBTable y)
        {
            if (x == y)
                return 0;
            if (x.Type != y.Type)
            {
                if (x.Type == DBTableType.Table)
                    return -1;
                else
                    return 1;
            }
            var xpars = new List<DBTable>();
            x.GetAllParentTables(xpars);
            var ypars = new List<DBTable>();
            y.GetAllParentTables(ypars);
            var xchil = new List<DBTable>();
            x.GetAllChildTables(xchil);
            var ychil = new List<DBTable>();
            y.GetAllChildTables(ychil);

            if (xpars.Contains(y))
                return 1;
            else if (ypars.Contains(x))
                return -1;
            else
            {
                var merge = (List<DBTable>)ListHelper.AND(xpars, ypars, null);
                if (merge.Count > 0)
                {
                    int r = xpars.Count.CompareTo(ypars.Count);
                    if (r != 0)
                        return r;
                }
                // foreach(DBTable xp in xpars)
                //     if(xp.GetChildTables())
            }

            if (xchil.Contains(y))
                return -1;
            else if (ychil.Contains(x))
                return 1;
            else
            {
                List<DBTable> merge = (List<DBTable>)ListHelper.AND(xchil, ychil, null);
                if (merge.Count > 0)
                {
                    int r = xchil.Count.CompareTo(ychil.Count);
                    if (r != 0)
                        return r;
                }
                // foreach(DBTable xp in xpars)
                //     if(xp.GetChildTables())
            }
            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }

        public static bool Equal<T>(T x, T y)
        {
            if (x is string xString && y is string yString)
            {
                return string.Equals(xString, yString, StringComparison.Ordinal);
            }
            if (x is byte[] xByte && y is byte[] yByte)
            {
                return ByteArrayComparer.Default.Equals(xByte, yByte);
            }
            return EqualityComparer<T>.Default.Equals(x, y);
        }

        public static bool Equal(object x, object y)
        {
            if (x == null)
            {
                return y == null;
            }
            if (y == null)
            {
                return false;
            }

            var equal = false;
            if (x.GetType() == typeof(string))
                equal = string.Equals(x.ToString(), y.ToString(), StringComparison.Ordinal);
            else if (x is Enum || y is Enum)
                equal = ((int)x).Equals((int)y);
            else if (x is byte[] byteX && y is byte[] byteY)
                equal = Helper.EqualsBytes(byteX, byteY);
            else
                equal = x.Equals(y);
            return equal;
        }

        public static object GetItem(List<KeyValuePair<string, object>> list, string key)
        {
            for (int i = 0; i < list.Count; i++)
                if (string.Compare(list[i].Key, key, true) == 0)
                    return list[i].Value;
            return DBNull.Value;
        }

        public static DBTable<T> GetTable<T>() where T : DBItem => (DBTable<T>)GetTable(typeof(T));

        public static DBTable GetTable(Type type)
        {
            foreach (var schema in Schems)
            {
                var table = schema.GetTable(type);
                if (table != null)
                    return table;
            }
            return null;
        }

        public static List<int> AccessGroups { get; } = new List<int>();

        public static IDBProvider DataProvider { get; set; }
    }

    public class DBSchemaChange : ICheck
    {
        private DBSchemaItem item;
        private DDLType change;
        private bool check = true;

        public string Type
        {
            get { return item == null ? null : Locale.Get(item.GetType()); }
        }

        public DBSchemaItem Item
        {
            get { return item; }
            set { item = value; }
        }

        public DDLType Change
        {
            get { return change; }
            set { change = value; }
        }

        public string Generate(bool dependency)
        {
            return item.FormatSql(change, dependency);
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", change, Type, item);
        }

        public bool Check
        {
            get { return check; }
            set { check = value; }
        }

        public int Order { get; set; }
    }
}
