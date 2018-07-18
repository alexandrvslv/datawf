/*
 DBService.cs
 
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using DataWF.Common;

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
        private static DBSchema defaultSchema;
        private static DBConnectionList connections = new DBConnectionList();
        private static DBSchemaList schems = new DBSchemaList();

        public static void SaveCache()
        {
            foreach (DBSchema schema in schems)
            {
                foreach (DBTable table in schema.Tables)
                {
                    if (table.Count > 0 && table.IsCaching && !(table is IDBVirtualTable))
                    {
                        table.SaveFile();
                    }
                }
            }
        }

        public static void LoadCache()
        {
            foreach (DBSchema schema in schems)
            {
                foreach (DBTable table in schema.Tables)
                {
                    if (table.IsCaching && !(table is IDBVirtualTable))
                    {
                        table.LoadFile();
                    }
                }
            }
            Helper.LogWorkingSet("Data Cache");
        }

        public static SelectableList<DBSchemaChange> Changes = new SelectableList<DBSchemaChange>();

        public static event EventHandler<DBSchemaChangedArgs> DBSchemaChanged;

        public static void OnDBSchemaChanged(DBSchemaItem item, DDLType type)
        {
            if (type == DDLType.Default
                || item.Container == null
                || item.Schema == null
                || item.Schema.Container == null
                || item.Schema.IsSynchronizing)
                return;
            if (item is IDBTableContent)
            {
                var table = ((IDBTableContent)item).Table;
                if (table is IDBVirtualTable || table.Container == null)
                    return;
            }
            DBSchemaChange change = null;

            var list = Changes.Select("Item", CompareType.Equal, item).ToList();

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
                change = new DBSchemaChange() { Item = item, Change = type };
                Changes.Add(change);
            }
            DBSchemaChanged?.Invoke(item, new DBSchemaChangedArgs { Item = item, Type = type });
        }

        public static event DBItemEditEventHandler RowAdded;

        internal static void OnAdded(DBItem e)
        {
            RowAdded?.Invoke(new DBItemEventArgs(e) { State = DBUpdateState.Insert });
        }

        public static event DBItemEditEventHandler RowRemoved;

        internal static void OnRemoved(DBItem e)
        {
            RowRemoved?.Invoke(new DBItemEventArgs(e) { State = DBUpdateState.Delete });
        }

        public static event DBItemEditEventHandler RowEditing;

        internal static void OnEditing(DBItemEventArgs e)
        {
            RowEditing?.Invoke(e);
        }

        public static event DBItemEditEventHandler RowEdited;

        internal static void OnEdited(DBItemEventArgs e)
        {
            RowEdited?.Invoke(e);
        }

        //public static event DBItemEditEventHandler RowStateEdited;
        //internal static void OnStateEdited(DBItemEventArgs e)
        //{
        //    RowStateEdited?.Invoke(e);
        //}

        public static event DBItemEditEventHandler RowUpdating;

        internal static void OnUpdating(DBItemEventArgs e)
        {
            RowUpdating?.Invoke(e);
        }

        public static event DBItemEditEventHandler RowLoging;

        internal static void OnLogItem(DBItemEventArgs e)
        {
            RowLoging?.Invoke(e);
        }

        public static event DBItemEditEventHandler RowUpdated;

        internal static void OnUpdated(DBItemEventArgs e)
        {
            RowUpdated?.Invoke(e);
        }

        public static event DBItemEditEventHandler RowAccept;

        internal static void OnAccept(DBItem item)
        {
            RowAccept?.Invoke(new DBItemEventArgs(item));
        }

        public static event DBItemEditEventHandler RowReject;

        internal static void OnReject(DBItem item)
        {
            RowReject?.Invoke(new DBItemEventArgs(item));
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
            if (DataProvider != null)
                DataProvider.Save();
        }

        public static void Load()
        {
            Load("data.xml");
        }

        public static void Load(string file)
        {
            Serialization.Deserialize("connections.xml", connections);
            Serialization.Deserialize(file, schems);
            Helper.LogWorkingSet("Schema");
            Changes.Clear();
            if (DataProvider != null)
            {
                DataProvider.Load();
                Helper.LogWorkingSet("DataProvider");
            }
        }

        public static DBConnectionList Connections
        {
            get { return connections; }
            set { connections = value; }
        }

        public static DBSchemaList Schems
        {
            get { return schems; }
        }

        public static DBSchema DefaultSchema
        {
            get
            {
                if (defaultSchema == null && schems.Count > 0)
                    defaultSchema = schems[0];
                return defaultSchema;
            }
            set
            {
                defaultSchema = value;
                if (defaultSchema != null && !schems.Contains(defaultSchema))
                    schems.Add(defaultSchema);
            }
        }

        public static void Deserialize(string file, DBSchemaItem selectedDBItem)
        {
            var item = Serialization.Deserialize(file);
            if (item is DBTable)
            {
                DBSchema schema = selectedDBItem.Schema;

                if (schema.Tables.Contains(((DBTable)item).Name))
                    schema.Tables.Remove(((DBTable)item).Name);
                schema.Tables.Add((DBTable)item);
            }
            else if (item is DBSchema)
            {
                DBSchema schema = (DBSchema)item;
                if (Schems.Contains(schema.Name))
                    schema.Name = schema.Name + "1";
                Schems.Add((DBSchema)item);
            }
            else if (item is DBColumn)
            {
                var table = selectedDBItem as DBTable;
                if (table != null)
                    table.Columns.Add((DBColumn)item);
            }
            else if (item is SelectableList<DBSchemaItem>)
            {
                var list = (SelectableList<DBSchemaItem>)item;
                foreach (var i in list)
                {
                    if (i is DBColumn && selectedDBItem is DBTable)
                        ((DBTable)selectedDBItem).Columns.Add((DBColumn)i);
                    else if (i is DBTable && selectedDBItem is DBSchema)
                        ((DBSchema)selectedDBItem).Tables.Add((DBTable)i);
                }

            }
        }

        public static void CommitChanges()
        {
            if (Changes.Count == 0)
                return;
            var schema = (DBSchema)null;
            var builder = new StringBuilder();
            foreach (var item in Changes.OrderBy(p => p.Item.Schema))
            {
                if (item.Item.Schema != schema)
                {
                    if (schema != null)
                    {
                        schema.Connection.ExecuteGoQuery(builder.ToString());
                        schema = null;
                        builder.Clear();
                    }
                    schema = item.Item.Schema;
                }
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
            }
            if (schema != null)
            {
                foreach (var command in schema.Connection.SplitGoQuery(builder.ToString()))
                {
                    try
                    {
                        schema.Connection.ExecuteQuery(command);
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.IndexOf("already exist", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            throw ex;
                        }
                    }
                }
            }
            Serialization.Serialize(Changes, $"SchemaDiff_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.xml");
            Changes.Clear();
            Save();
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

        public static DBColumn ParseColumn(string name, DBSchema schema = null)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            DBColumn column = null;
            DBTable table = ParseTable(name, schema);

            int index = name.LastIndexOf('.');
            name = index < 0 ? name : name.Substring(index + 1);
            if (schema == null)
                schema = DefaultSchema;


            if (table != null)
            {
                column = table.ParseColumn(name);
            }
            else if (schema != null)
            {
                foreach (var t in schema.Tables)
                {
                    column = t.Columns[name];
                    if (column != null)
                        break;
                }
            }
            return column;
        }

        public static DBTable ParseTable(string code, DBSchema s = null)
        {
            if (string.IsNullOrEmpty(code))
                return null;
            DBTable table = null;
            DBSchema schema = null;
            int index = code.IndexOf('.');
            if (index >= 0)
            {
                schema = Schems[code.Substring(0, index++)];
                int sindex = code.IndexOf('.', index);
                code = sindex < 0 ? code.Substring(index) : code.Substring(index, sindex - index);
            }
            if (schema == null)
                schema = s;
            if (schema != null)
            {
                table = schema.Tables[code];
            }
            else
            {
                foreach (var sch in Schems)
                {
                    table = sch.Tables[code];
                    if (table != null)
                        break;
                }
            }
            return table;
        }

        public static DBTableGroup ParseTableGroup(string code, DBSchema s = null)
        {
            if (code == null)
                return null;
            DBSchema schema = null;
            int index = code.IndexOf('.');
            if (index < 0)
                schema = s;
            else
            {
                schema = Schems[code.Substring(0, index++)];
                int sindex = code.IndexOf('.', index);
                code = sindex < 0 ?
                    code.Substring(index) :
                    code.Substring(index, sindex - index);
            }
            return schema.TableGroups[code];
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



        public static event DBExecuteDelegate Execute;

        internal static void OnExecute(DBExecuteType type, string text, TimeSpan ms, object rez)
        {
            Execute?.Invoke(new DBExecuteEventArg { Time = ms, Query = text, Type = type, Rezult = rez });
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

        public static bool Equal(object x, object y)
        {
            if (x == null)
                return y == null;

            var equal = object.ReferenceEquals(x, y);
            if (!equal)
            {
                if (x is byte[] && y is byte[])
                    equal = Helper.CompareByte((byte[])x, (byte[])y);
                else if (x is Enum && y is int)
                    equal = ((int)x).Equals(y);
                else if (y is Enum && x is int)
                    equal = ((int)y).Equals(x);
                else if (x is string && y is string)
                    equal = string.Equals((string)x, (string)y, StringComparison.Ordinal);
                else
                    equal = x.Equals(y);
            }
            return equal;
        }

        public static object GetItem(List<KeyValuePair<string, object>> list, string key)
        {
            for (int i = 0; i < list.Count; i++)
                if (string.Compare(list[i].Key, key, true) == 0)
                    return list[i].Value;
            return DBNull.Value;
        }



        public static DBProcedure ParseProcedure(string code, string category = "General")
        {
            var procedure = (DBProcedure)null;
            foreach (var schema in Schems)
            {
                procedure = schema.Procedures[code];
                if (procedure == null)
                    procedure = schema.Procedures.SelectByCode(code, category);
                if (procedure == null && category != "General")
                    procedure = schema.Procedures.SelectByCode(code);
                if (procedure != null)
                    break;
            }
            return procedure;
        }

        private static List<int> accessGroups = new List<int>();

        public static List<int> AccessGroups { get { return accessGroups; } }

        public static IDataProvider DataProvider { get; set; }
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
    }
}
