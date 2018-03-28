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
        }

        public static SelectableList<DBSchemaChange> Changes = new SelectableList<DBSchemaChange>();

        public static event EventHandler<DBSchemaChangedArgs> DBSchemaChanged;

        public static void OnDBSchemaChanged(DBSchemaItem item, DDLType type)
        {
            if (item is DBTable && !DBService.Schems.Contains(((DBTable)item).Schema))
                return;
            if (item.Container == null)
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
            var args = new DBItemEventArgs(e) { State = DBUpdateState.Insert };
            OnStateEdited(args);
            RowAdded?.Invoke(args);
        }

        public static event DBItemEditEventHandler RowRemoved;

        internal static void OnRemoved(DBItem e)
        {
            var args = new DBItemEventArgs(e) { State = DBUpdateState.Delete };
            OnStateEdited(args);
            RowRemoved?.Invoke(args);
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

        public static event DBItemEditEventHandler RowStateEdited;

        internal static void OnStateEdited(DBItemEventArgs e)
        {
            RowStateEdited?.Invoke(e);
        }

        public static event DBItemEditEventHandler RowUpdating;

        internal static void OnUpdating(DBItemEventArgs e)
        {
            RowUpdating?.Invoke(e);
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
            Serialization.Deserialize(file, schems);
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
                if (DBService.Schems.Contains(schema.Name))
                    schema.Name = schema.Name + "1";
                DBService.Schems.Add((DBSchema)item);
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

        public static void CommitChanges(DBSchema schema)
        {
            schema.Connection.ExecuteQuery(BuildChangesQuery(schema));
        }

        public static string BuildChangesQuery(DBSchema schema)
        {
            var builder = new StringBuilder();
            foreach (var item in Changes)
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
            }
            Changes.Clear();
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
                schema = DBService.Schems[code.Substring(0, index++)];
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
                foreach (var sch in DBService.Schems)
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
                schema = DBService.Schems[code.Substring(0, index++)];
                int sindex = code.IndexOf('.', index);
                code = sindex < 0 ?
                    code.Substring(index) :
                    code.Substring(index, sindex - index);
            }
            return schema.TableGroups[code];
        }

        public static bool GetBool(DBItem row, string ColumnCode)
        {
            return GetBool(row, row.Table.Columns[ColumnCode]);
        }

        public static bool GetBool(DBItem row, DBColumn Column)
        {
            if (Column == null || (Column.Keys & DBColumnKeys.Boolean) != DBColumnKeys.Boolean)
                return false;

            return row[Column].ToString() == Column.BoolTrue;
        }

        public static void SetBool(DBItem row, string ColumnCode, bool Value)
        {
            SetBool(row, row.Table.Columns[ColumnCode], Value);
        }

        public static void SetBool(DBItem row, DBColumn Column, bool Value)
        {
            if (Column == null || (Column.Keys & DBColumnKeys.Boolean) != DBColumnKeys.Boolean)
                return;
            row[Column] = Value ? Column.BoolTrue : Column.BoolFalse;
        }

        public static DateTime GetDateVal(object val)
        {
            if (val == null)
                return DateTime.MinValue;
            if (val is DateTime)
                return (DateTime)val;
            return DateTime.Parse(val.ToString());
        }

        public static DateTime GetDate(DBItem row, DBColumn Column)
        {
            return GetDateVal(row[Column]);
        }

        public static DateTime GetDate(DBItem row, string Column)
        {
            return GetDateVal(row[Column]);
        }

        public static void SetDate(DBItem row, DBColumn Column, DateTime value)
        {
            row[Column] = value;
        }

        public static TimeSpan GetTimeSpan(DBItem row, DBColumn Column)
        {
            object val = row[Column];
            if (val == null)
                return new TimeSpan();
            if (val is TimeSpan)
                return (TimeSpan)val;
            return TimeSpan.Parse(val.ToString());
        }

        public static void SetTimeSpan(DBItem row, DBColumn Column, TimeSpan value)
        {
            row[Column] = value;
        }

        public static byte[] GetZip(DBItem row, DBColumn column)
        {
            var data = row.GetValue<byte[]>(column);
            if (data != null && Helper.IsGZip(data))
                data = Helper.ReadGZip(data);
            return data;
        }

        public static byte[] SetZip(DBItem row, DBColumn column, byte[] data)
        {
            byte[] temp = data != null && data.Length > 500 ? Helper.WriteGZip(data) : data;
            row.SetValue(temp, column);
            return temp;
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

        public static DBColumnGroup InitColumnGroup(DBTable table, string code)
        {
            DBColumnGroup cs = null;
            cs = table.ColumnGroups[code];
            if (cs == null)
            {
                cs = new DBColumnGroup(code);
                table.ColumnGroups.Add(cs);
            }
            return cs;
        }

        public static DBColumn InitColumn(DBTable table, string code)
        {
            DBColumn cs = null;
            cs = table.Columns[code];
            if (cs == null)
            {
                cs = new DBColumn(code);
                table.Columns.Add(cs);
            }
            return cs;
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

        public static string FormatToSqlText(object value)
        {
            if (value is DBItem)
                value = ((DBItem)value).PrimaryId;

            if (value == null)
                return "null";
            else if (value is string)
                return "'" + ((string)value).Replace("'", "''") + "'";
            else if (value is DateTime)
                if (((DateTime)value).TimeOfDay == TimeSpan.Zero)
                    return "'" + ((DateTime)value).ToString("yyyy-MM-dd") + "'";
                else
                    return "'" + ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff") + "'";
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
                List<DBTable> merge = (List<DBTable>)ListHelper.AND(xpars, ypars, null);
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

        //public static T Fabric<T>(DBItem row) where T : DBItem, new()
        //{
        //    if (row == null)
        //        return null;
        //    T view = row as T;
        //    if (view == null)//&& view.Table == table
        //    {
        //        view = new T();
        //        view.Table = row.Table;
        //        row.Table.Rows.Replace(row, view);
        //    }
        //    return view;
        //}

        public static DBItem FabricRow(DBItem row, Type t)
        {
            if (row == null)
                return null;
            DBItem view = row;
            if (view != null && TypeHelper.IsBaseType(view.GetType(), t))
                return view;

            var pa = EmitInvoker.Initialize(t, Type.EmptyTypes, true);
            if (pa == null)
                throw new InvalidOperationException(string.Format("Type {0} must have constructor with DBRow parameters", t));
            var rowview = (DBItem)pa.Create(new object[] { row });
            rowview.Table = row.Table;
            //rowview.Initialize(row);
            return rowview;
        }

        public static string GetRowText(DBTable table, object id)
        {
            return GetRowText(table, id, table.Columns.GetIsView());
        }

        public static string GetRowText(DBTable table, object id, IEnumerable<DBColumn> parameters)
        {
            return GetRowText(table, id, parameters, false, " - ");
        }

        public static string GetRowText(DBTable table, object id, IEnumerable<DBColumn> parametrs, bool showColumn, string separator)
        {
            return GetRowText(table.LoadItemById(id), parametrs, showColumn, separator);
        }

        public static string GetRowText(DBTable table, object id, bool allColumns, bool showColumn, string separator)
        {
            return GetRowText(table.LoadItemById(id), (allColumns ? (IEnumerable<DBColumn>)table.Columns : table.Columns.GetIsView()), showColumn, separator);
        }

        public static string GetRowText(DBItem row, bool allColumns, bool showColumn, string separator)
        {
            return GetRowText(row, (allColumns ? (IEnumerable<DBColumn>)row.Table.Columns : row.Table.Columns.GetIsView()), showColumn, separator);
        }

        public static string GetRowText(DBItem row)
        {
            if (row == null)
                return "<null>";
            //DBTable table = row.VirtualTable != null ? row.VirtualTable : row.Table;
            return GetRowText(row, row.Table.Columns.GetIsView(), false, " - ");
        }

        public static string GetRowText(DBItem row, ICollection<DBColumn> parameters)
        {
            if (row == null)
                return "<null>";
            return GetRowText(row, parameters, false, " - ");
        }

        public static string GetRowText(DBItem row, IEnumerable<DBColumn> parameters, bool showColumn, string separator)
        {
            if (row == null)
                return "<null>";
            else if (!row.Access.View)
                return "********";
            string bufRez = "";
            if (parameters == null)
                parameters = row.Table.Columns;
            //if (!parameters.Any())
            //{
            //    if (row.Table.CodeKey != null)
            //        parameters.Add(row.Table.CodeKey);
            //    else if (row.Table.PrimaryKey != null)
            //        parameters.Add(row.Table.PrimaryKey);
            //}
            string c = string.Empty;
            foreach (DBColumn column in parameters)
            {
                if (!column.Access.View)
                {
                    //bufRez += temprez;
                    continue;
                }
                string header = "";
                if (showColumn)
                    header = $"{column}: ";
                string value = FormatValue(column, row[column]);
                string temprez = header + value + separator;
                if (column.IsCulture)
                {
                    if (column.Culture.TwoLetterISOLanguageName == Locale.Instance.Culture.TwoLetterISOLanguageName)
                    {
                        bufRez += temprez;
                        if (value.Length != 0)
                            c = null;
                    }
                    else if (c != null && value.Length != 0)
                        c += value;
                }
                else
                    bufRez += temprez;
            }
            if (c != null)
                bufRez += " " + c;
            bufRez = bufRez.Trim((separator + " ").ToCharArray());

            return bufRez;
        }

        public static string FormatValue(DBColumn column, object val)
        {
            //if value passed to format is null
            if (val == null)
                return "";
            if (column == null)
                return val.ToString();
            if ((column.Keys & DBColumnKeys.Boolean) == DBColumnKeys.Boolean)
            {
                if (val.ToString().Equals(column.BoolTrue))
                    return "Check";
                else
                    return "Uncheck";
            }
            if (column.IsReference)
            {
                DBItem temp = column.ReferenceTable.LoadItemById(val);
                return temp == null ? "<new or empty>" : temp.ToString();
            }

            if (column.DataType == typeof(string))
                return val.ToString();

            if (column.DataType == typeof(byte[]))
            {
                if ((column.Keys & DBColumnKeys.Access) == DBColumnKeys.Access)
                {
                    AccessValue cash = new AccessValue();
                    cash.Read((byte[])val);
                    string rez = string.Empty;
                    foreach (var item in cash.Items)
                    {
                        rez += string.Format("{0}{1}", rez.Length > 0 ? "; " : string.Empty, item);
                    }
                    return rez;
                }
                else
                    return Helper.LengthFormat(((byte[])val).LongLength);
            }
            if (column.Format != null)
            {
                MethodInfo mi = val.GetType().GetMethod("ToString", new Type[] { typeof(string) });
                if (column.Format.ToLower() == "p")
                    if (val is decimal)
                        return ((decimal)val * 100).ToString("N") + "%";
                    else if (val is double)
                        return ((double)val * 100).ToString("N") + "%";
                    else if (val is float)
                        return ((float)val * 100).ToString("N") + "%";
                    else return (decimal.Parse(val.ToString()) * 100).ToString("N") + "%";
                if (column.Format.ToLower() == "b" && column.DataType == typeof(string) && column.Size == 1)
                    if (val.ToString() == "RowSetting")
                        return "V";
                    else
                        return "X";
                else if (mi != null)
                    return (string)mi.Invoke(val, new object[] { column.Format });
            }

            if (val is DateTime)
                return val.Equals(((DateTime)val).Date) ? ((DateTime)val).ToString("yyyy.MM.dd") : val.ToString();
            return val.ToString();
        }

        public static object ParseValue(DBColumn column, object value)
        {
            object buf = null;
            if (column == null)
                return value;
            if (value is bool && (column.Keys & DBColumnKeys.Boolean) == DBColumnKeys.Boolean && column.DataType != typeof(bool))
                value = (bool)value ? column.BoolTrue : column.BoolFalse;
            if (value == null || value == DBNull.Value)
                buf = null;
            else if (column.DataType == value.GetType())
                buf = value;
            else if (value is DBItem)
                buf = ((DBItem)value).PrimaryId;
            //else if (column.Pull.ItemType.IsG )
            //buf = ((DBItem)value).PrimaryId;
            else
                buf = ParseValue(column, value.ToString());

            if (buf is DateTime && buf.Equals(DateTime.MinValue))
                buf = null;
            return buf;
        }

        public static string FormatStatusFilter(DBTable table, DBStatus filter)
        {
            string rez = string.Empty;
            if (table.StatusKey != null && filter != 0 && filter != DBStatus.Empty)
            {
                var qlist = new QEnum();
                if ((filter & DBStatus.Actual) == DBStatus.Actual)
                    qlist.Items.Add(new QValue((int)DBStatus.Actual));
                if ((filter & DBStatus.New) == DBStatus.New)
                    qlist.Items.Add(new QValue((int)DBStatus.New));
                if ((filter & DBStatus.Edit) == DBStatus.Edit)
                    qlist.Items.Add(new QValue((int)DBStatus.Edit));
                if ((filter & DBStatus.Delete) == DBStatus.Delete)
                    qlist.Items.Add(new QValue((int)DBStatus.Delete));
                if ((filter & DBStatus.Archive) == DBStatus.Archive)
                    qlist.Items.Add(new QValue((int)DBStatus.Archive));
                if ((filter & DBStatus.Error) == DBStatus.Error)
                    qlist.Items.Add(new QValue((int)DBStatus.Error));
                var param = new QParam()
                {
                    ValueLeft = new QColumn(table.StatusKey),
                    Comparer = CompareType.In,
                    ValueRight = qlist
                };

                rez = param.Format();
            }
            return rez;
        }

        /// <summary>
        /// Parses the value.
        /// </summary>
        /// <returns>The value.</returns>
        /// <param name="column">Column.</param>
        /// <param name="value">Value.</param>
        public static object ParseValue(DBColumn column, string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;
            object val = null;
            var type = column.DataType;
            if (type == typeof(decimal))
            {
                if (decimal.TryParse(value.Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat, out var d))
                    val = d;
            }
            else if (type == typeof(DateTime))
            {
                var index = value.IndexOf('|');
                if (index >= 0)
                    value = value.Substring(0, index);
                DateTime date;
                if (value.Equals("getdate()", StringComparison.OrdinalIgnoreCase) || value.Equals("current_timestamp", StringComparison.OrdinalIgnoreCase))
                    val = DateTime.Now;
                if (DateTime.TryParse(value, out date))
                    val = date;
                else if (DateTime.TryParseExact(value, new string[] { "yyyyMMdd", "yyyyMM" }, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None, out date))
                    val = date;
            }
            else if (type == typeof(string))
                val = value;
            else if (type == typeof(int) || type.IsEnum)
            {
                if (int.TryParse(value, out int i))
                    val = i;
            }
            else if (type == typeof(byte))
            {
                if (byte.TryParse(value, out byte i))
                    val = i;
            }
            else if (type == typeof(TimeSpan))
            {
                val = TimeSpan.Parse(value);
            }
            else if (type == typeof(double))
            {
                if (double.TryParse(value, out double d))
                    val = d;
            }
            else if (type == typeof(float))
            {
                if (float.TryParse(value, out float f))
                    val = f;
            }
            else if (type == typeof(long))
            {
                if (long.TryParse(value, out long l))
                    val = l;
            }
            else if (type == typeof(bool))
            {
                if (bool.TryParse(value, out bool l))
                    val = l;
            }
            return val;
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
                else if (x is string && y is string)
                    equal = string.Equals((string)x, (string)y, StringComparison.Ordinal);
                else
                    equal = x.Equals(y);
            }
            return equal;
        }

        public static List<DBItem> GetChilds(DBItem row, int recurs = 2, DBLoadParam param = DBLoadParam.None)
        {
            var rows = new List<DBItem>();
            recurs--;
            var relations = row.Table.GetChildRelations();
            foreach (DBForeignKey relation in relations)
            {
                if (relation.Table.Name.IndexOf("drlog", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    relation.Table.Type != DBTableType.Table ||
                    relation.Column.ColumnType != DBColumnTypes.Default)
                    continue;
                if (recurs >= 0 || relation.Table == row.Table)
                {
                    var list = row.GetReferencing(relation, param);
                    foreach (DBItem item in list)
                    {
                        if (item != row)
                        {
                            var childs = GetChilds(item, recurs, param);
                            foreach (var child in childs)
                                if (!rows.Contains(child))
                                    rows.Add(child);
                            if (!rows.Contains(item))
                                rows.Add(item);
                        }
                    }
                }
            }
            return rows;
        }

        public static void Delete(DBItem row, int recurs = 2, DBLoadParam param = DBLoadParam.None)
        {
            try
            {
                recurs--;
                var relations = row.Table.GetChildRelations();
                foreach (DBForeignKey relation in relations)
                {
                    if (relation.Table.Name.IndexOf("drlog", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        relation.Table.Type != DBTableType.Table ||
                    relation.Column.ColumnType != DBColumnTypes.Default)
                        continue;
                    if (recurs >= 0 || relation.Table == row.Table)
                    {
                        var list = row.GetReferencing(relation, param);
                        foreach (DBItem item in list)
                        {
                            if (item.Attached)
                                Delete(item, recurs, param);
                        }
                    }
                }
                if ((row.DBState & DBUpdateState.Insert) == DBUpdateState.Insert)
                    row.Table.Remove(row);
                else
                {
                    row.Delete();
                    row.Save();
                }
            }
            catch (Exception ex)//TODO If Timeout Expired
            {
                Helper.OnException(ex);
            }
        }

        public static void Merge(IEnumerable list, DBItem main)
        {
            var relations = main.Table.GetChildRelations().ToList();
            var rows = new List<DBItem>();
            rows.Add(main);
            foreach (DBItem item in list)
                if (item != main)
                {
                    rows.Add(item);

                    item.DBState |= DBUpdateState.Delete;
                    foreach (DBColumn column in item.Table.Columns)
                        if (main[column] == DBNull.Value && item[column] != DBNull.Value)
                            main[column] = item[column];

                    foreach (DBForeignKey relation in relations)
                        if (relation.Table.Type == DBTableType.Table)
                        {
                            var refings = item.GetReferencing<DBItem>(relation, DBLoadParam.Load | DBLoadParam.Synchronize).ToList();
                            if (refings.Count > 0)
                            {
                                foreach (DBItem refing in refings)
                                    refing[relation.Column] = main.PrimaryId;

                                relation.Table.Save(refings);
                            }
                        }
                }
            main.Table.Save(rows);

        }

        public static IDbCommand CreateCommand(IDbConnection connection, string text = null, IDbTransaction transaction = null)
        {
            IDbCommand command = connection.CreateCommand();
            command.CommandTimeout = connection.ConnectionTimeout;
            command.CommandText = text;
            if (transaction != null)
                command.Transaction = transaction;

            return command;
        }

        public static object GetItem(List<KeyValuePair<string, object>> list, string key)
        {
            for (int i = 0; i < list.Count; i++)
                if (string.Compare(list[i].Key, key, true) == 0)
                    return list[i].Value;
            return DBNull.Value;
        }

        public static T FabricRow<T>()
        {
            throw new NotImplementedException();
        }

        public static ColumnAttribute GetColumnAttribute(PropertyInfo property)
        {
            var config = property.GetCustomAttribute<ColumnAttribute>();
            if (config == null)
            {
                config = property.GetCustomAttribute<VirtualColumnAttribute>();
            }
            return config;
        }

        private static Dictionary<Type, TableAttribute> cacheTables = new Dictionary<Type, TableAttribute>();

        private static Dictionary<Type, ItemTypeAttribute> cacheItemTypes = new Dictionary<Type, ItemTypeAttribute>();

        public static TableAttribute GetTableAttribute(Type type, bool inherite = false)
        {
            if (!cacheTables.TryGetValue(type, out TableAttribute table))
            {
                table = type.GetCustomAttribute<TableAttribute>();
                if (table == null)
                {
                    table = type.GetCustomAttribute<VirtualTableAttribute>();
                }
                if (table == null && inherite)
                {
                    var itemType = GetItemTypeAttribute(type);
                    return itemType?.Table;
                }
                if (table != null)
                {
                    table.Initialize(type);
                }

                cacheTables[type] = table;
            }
            return table;
        }

        public static ItemTypeAttribute GetItemTypeAttribute(Type type)
        {
            if (!cacheItemTypes.TryGetValue(type, out ItemTypeAttribute itemType))
            {
                itemType = type.GetCustomAttribute<ItemTypeAttribute>();
                if (itemType != null)
                {
                    itemType.Initialize(type);
                }
                cacheItemTypes[type] = itemType;
            }
            return itemType;
        }

        public static DBTable<T> GetTable<T>(DBSchema schema = null, bool generate = false) where T : DBItem, new()
        {
            return (DBTable<T>)GetTable(typeof(T), schema, generate);
        }

        public static DBTable GetTable(Type type, DBSchema schema = null, bool generate = false)
        {
            var config = GetTableAttribute(type);
            if (config != null)
            {
                if (config.Table == null && generate)
                    config.Generate(schema);
                return config.Table;
            }
            return null;
        }

        public static void Generate(IEnumerable<Assembly> assemblies, DBSchema schema)
        {
            var attributes = new List<TableAttribute>();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetExportedTypes().Where(item => item.IsClass))
                {
                    var tableAttribute = GetTableAttribute(type);
                    if (tableAttribute != null)
                    {
                        attributes.Add(tableAttribute);
                    }
                    else
                    {
                        GetItemTypeAttribute(type);
                    }
                }
            }

            foreach (var tableAttribute in attributes)
            {
                tableAttribute.Generate(schema);
            }
        }

        public static DBSchema Generate(Assembly assembly)
        {
            var schema = new DBSchema();
            schems.Add(schema);
            Generate(assembly, schema);
            return schema;
        }

        public static void Generate(Assembly assembly, DBSchema schema)
        {
            Generate(new[] { assembly }, schema);
        }

        public static DBProcedure ParseProcedure(string name)
        {
            foreach (var schema in Schems)
            {
                var procedure = schema.Procedures[name];
                if (procedure != null)
                    return procedure;
            }
            return null;
        }

        public static void ClearChache()
        {
            cacheTables.Clear();
        }



        private static List<int> accessGroups = new List<int>();

        public static List<int> AccessGroups { get { return accessGroups; } }

    }

    public class DBSchemaChange : ICheck
    {
        private DBSchemaItem item;
        private DDLType change;
        private bool check = true;

        public string Type
        {
            get { return item == null ? null : Locale.Get(item.GetType().FullName, item.GetType().Name); }
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
