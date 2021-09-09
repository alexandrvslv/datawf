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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public class DBProvider : IDBProvider
    {
        public static DBProvider Default;
        private readonly DBConnectionList connections = new DBConnectionList();
        private readonly DBSchemaList schems = new DBSchemaList();
        private DBSchema schema;
        protected string schemaName = "example";
        public DBProvider()
        {
            Default = this;
            schems.Provider = this;
            connections.Provider = this;
        }

        public static event EventHandler<DBSchemaChangedArgs> DBSchemaChanged;

        public SelectableList<DBSchemaChange> Changes = new SelectableList<DBSchemaChange>();

        public DBConnectionList Connections => connections;

        public DBSchemaList Schems => schems;

        public string SchemaName
        {
            get => schemaName;
            set => schemaName = value;
        }

        public DBSchema Schema
        {
            get => schema ??= schems[schemaName];
            set => schems[schemaName] = schema = value;
        }

        public virtual Task CreateNew()
        {
            Schema = new DBSchema()
            {
                Name = schemaName,
                Provider = this,
                Connection = new DBConnection
                {
                    Name = schemaName,
                    System = DBSystem.SQLite,
                    DataBase = $"{schemaName}.sqlite"
                }
            };

            Generate();

            //Schema.DropDatabase();

            Schema.ExecuteCreateDatabase();
            Save();
            return null;
        }

        public virtual void Generate()
        {
            Schema?.Generate(SchemaName);
        }

        public virtual void Load()
        {
            Helper.Log(this, "Start");
            Load("data.xml");

            if (Schema == null || Schema.Connection == null)
            {
                throw new Exception("Missing data.xml or connection.xml");
            }
            if (!Schema.Connection.CheckConnection())
            {
                throw new Exception("Check Connection FAIL!");
            }
            Generate();
            CommitChanges();

            Helper.Log(this, "Load & Generate Success");

            foreach (var initializer in Helper.ModuleInitializer)
            {
                initializer.Initialize(new[] { Schema });
            }
        }

        public virtual void Save()
        {
            Save("data.xml");
        }

        public void Save(string file)
        {
            foreach (var schema in schems)
            {
                schema.Tables.ApplyDefaultSort();
            }
            Serialization.Serialize(connections, "connections.xml");
            Serialization.Serialize(schems, file);
        }

        public void Load(string file)
        {
            Serialization.Deserialize("connections.xml", connections);
            Serialization.Deserialize(file, schems);
            foreach (var schema in schems)
            {
                schema.IsSynchronizing = false;
            }
            Helper.LogWorkingSet("Schema");
            Changes.Clear();
        }

        internal void OnChanged(DBSchemaItem item, DDLType type)
        {
            if (type == DDLType.None
                || !item.Containers.Any()
                || item.Schema == null
                || !item.Schema.Containers.Any()
                || item.Schema.IsSynchronizing)
                return;
            if (item is IDBTableContent tabled)
            {
                if (tabled.Table.IsVirtual || !tabled.Table.Containers.Any())
                    return;
                if (item is DBColumn column && column.ColumnType != DBColumnTypes.Default)
                    return;
            }
            DBSchemaChange change = null;

            var list = Changes.Select(DBSchemaChange.ItemInvoker.Instance, CompareType.Equal, item).ToList();

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
                    OnChanged(column, DDLType.Create);
                }

                foreach (var constraint in table.Constraints)
                {
                    if (constraint.Column?.ColumnType != DBColumnTypes.Default)
                        continue;
                    OnChanged(constraint, DDLType.Create);
                }

                foreach (var foreign in table.Foreigns)
                {
                    if (foreign.Column?.ColumnType != DBColumnTypes.Default)
                        continue;
                    OnChanged(foreign, DDLType.Create);
                }
            }

            DBSchemaChanged?.Invoke(item, new DBSchemaChangedArgs { Item = item, Type = type });
        }

        private IEnumerable<DBSchemaChange> GetChanges(IDBSchema schema)
        {
            var isSqlite = schema.Connection.System == DBSystem.SQLite;
            var chages = Changes.Where(p => p.Item.Schema == schema).ToList();
            chages.Sort(CompareChanges);
            foreach (var item in chages)
            {
                if (item.Item is DBConstraint && isSqlite)
                {
                    continue;
                }
                yield return item;
            }

            int CompareChanges(DBSchemaChange a, DBSchemaChange b)
            {
                if (a.Item is DBTable tableA && !tableA.IsVirtual)
                {
                    if (b.Item is DBTable tableB && !tableB.IsVirtual)
                    {
                        return DBTableComparer.Instance.Compare(tableA, tableB, true);
                    }
                    else
                    {
                        return -1;
                    }
                }
                else if (b.Item is DBTable table && !table.IsVirtual)
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
            }
        }

        public void CommitChanges()
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

        public void CommitChanges(DBSchema schema, DBSchemaChange item, string commands)
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
                    if (ex is Microsoft.Data.Sqlite.SqliteException sqliteException)
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

        public string BuildChangesQuery(IDBSchema schema)
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
            var xpars = new List<IDBTable>();
            x.GetAllParentTables(xpars);
            var ypars = new List<IDBTable>();
            y.GetAllParentTables(ypars);
            var xchil = new List<IDBTable>();
            x.GetAllChildTables(xchil);
            var ychil = new List<IDBTable>();
            y.GetAllChildTables(ychil);

            if (xpars.Contains(y))
                return 1;
            else if (ypars.Contains(x))
                return -1;
            else
            {
                var merge = (List<IDBTable>)ListHelper.AND(xpars, ypars, null);
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
                var merge = (List<IDBTable>)ListHelper.AND(xchil, ychil, null);
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

        public DBTable<T> GetTable<T>() where T : DBItem => (DBTable<T>)GetTable(typeof(T));

        public DBTable GetTable(Type type)
        {
            foreach (var schema in Schems)
            {
                var table = schema.GetTable(type);
                if (table != null)
                    return table;
            }
            return null;
        }

        public virtual DBUser FindUser(string email)
        {
            throw new NotImplementedException();
        }

        public void RefreshToString()
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
    }
}