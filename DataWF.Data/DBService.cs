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
using System.ComponentModel;
using System.Linq;
using System.Text;

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

        //public static event DBItemEditEventHandler RowAdded;

        //internal static void OnAdded(DBItem e)
        //{
        //    RowAdded?.Invoke(new DBItemEventArgs(e) { State = DBUpdateState.Insert });
        //}

        //public static event DBItemEditEventHandler RowRemoved;

        //internal static void OnRemoved(DBItem e)
        //{
        //    RowRemoved?.Invoke(new DBItemEventArgs(e) { State = DBUpdateState.Delete });
        //}

        //public static event DBItemEditEventHandler RowEditing;

        //internal static void OnEditing(DBItemEventArgs e)
        //{
        //    RowEditing?.Invoke(e);
        //}

        //public static event DBItemEditEventHandler RowEdited;

        //internal static void OnEdited(DBItemEventArgs e)
        //{
        //    RowEdited?.Invoke(e);
        //}

        //public static event DBItemEditEventHandler RowStateEdited;
        //internal static void OnStateEdited(DBItemEventArgs e)
        //{
        //    RowStateEdited?.Invoke(e);
        //}

        public static Action<DBItemEventArgs> RowUpdating;

        internal static void OnUpdating(DBItemEventArgs e)
        {
            RowUpdating?.Invoke(e);
        }

        public static Action<DBItemEventArgs> RowLoging;

        internal static void OnLogItem(DBItemEventArgs e)
        {
            RowLoging?.Invoke(e);
        }

        public static Action<DBItemEventArgs> RowUpdated;

        internal static void OnUpdated(DBItemEventArgs e)
        {
            RowUpdated?.Invoke(e);
        }

        public static Action<DBItemEventArgs> RowAccept;

        internal static void OnAccept(DBItemEventArgs e)
        {
            RowAccept?.Invoke(e);
        }

        public static Action<DBItemEventArgs> RowReject;

        internal static void OnReject(DBItemEventArgs e)
        {
            RowReject?.Invoke(e);
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
            if (item is DBTable table)
            {
                foreach (var column in table.Columns)
                {
                    OnDBSchemaChanged(column, DDLType.Create);
                }

                foreach (var constraint in table.Constraints)
                {
                    OnDBSchemaChanged(constraint, DDLType.Create);
                }

                foreach (var foreign in table.Foreigns)
                {
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
                var chages = Changes.Where(p => p.Item.Schema == schema).ToList();
                chages.Sort((a, b) =>
                {
                    if (a.Item is DBTable tableA)
                    {
                        if (b.Item is DBTable tableB)
                        {
                            return DBTableComparer.Instance.Compare(tableA, tableB, true);
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    else if (b.Item is DBTable)
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
                    string val = item.Generate();
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

        public static void CommitChanges(DBSchema schema, DBSchemaChange item, string commands)
        {
            foreach (var command in schema.Connection.SplitGoQuery(commands))
            {
                try
                {
                    Console.WriteLine($"sqlinfo: {item}");
                    Console.Write(command);
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
                    //TODO MSSql, MySql, Oracle, Sqlite
                    if (ex.Message.IndexOf("already exist", StringComparison.OrdinalIgnoreCase) >= 0
                        || ex.Message.IndexOf("duplicate column name", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine($"sqlinfo: skip already exist");
                        continue;
                    }
                    throw ex;
                }
            }
            Console.WriteLine();
        }

        public static string BuildChangesQuery(DBSchema schema)
        {
            var builder = new StringBuilder();
            foreach (var item in Changes.ToList())
            {
                if (schema != null && item.Item.Schema != schema)
                    continue;
                string val = item.Generate();
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

        public static bool EqualClass<T>(T x, T y) where T : class
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

        public static bool Equal<T>(T x, T y)
        {
            if (x is string xString && y is string yString)
            {
                return StringComparer.Ordinal.Equals(xString, yString);
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
                equal = Helper.CompareByteAsSpan(byteX, byteY);
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

        public string Generate()
        {
            return item.FormatSql(change);
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
