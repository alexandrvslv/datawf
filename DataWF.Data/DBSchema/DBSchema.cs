/*
 DBSchema.cs
 
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
using DataWF.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBSchema : DBSchemaItem, IFileSerialize
    {
        public static DBSchema Generate(Assembly assembly, string schemaName)
        {
            var schema = new DBSchema(schemaName);
            schema.Generate(new[] { assembly });
            DBService.Schems.Add(schema);
            return schema;
        }

        protected DBConnection connection;
        private string connectionName = string.Empty;
        protected string dataBase = "";
        protected string fileName = "";
        protected DBLogSchema logSchema;
        protected string logSchemaName;
        private bool cacheRelation;

        public DBSchema()
            : this(null)
        { }

        public DBSchema(string name) : base(name)
        {
            DataBase = name;
            Sequences = new DBSequenceList(this);
            Tables = new DBTableList(this);
            TableGroups = new DBTableGroupList(this);
            Procedures = new DBProcedureList(this);
        }

        public DBSchema(string name, string fileName) : this(name)
        {
            //Init();
            FileName = fileName;
            Serialization.Deserialize(fileName, this);
        }

        [Browsable(false)]
        public DBSchemaList Schems => Containers.FirstOrDefault() as DBSchemaList;

        [Browsable(false)]
        public string ConnectionName { get => connectionName; set => connectionName = value; }

        [XmlIgnore, JsonIgnore]
        public DBConnection Connection
        {
            get { return connection ?? (connection = DBService.Connections[ConnectionName]); }
            set
            {
                connection = value;
                ConnectionName = connection?.Name;
                if (value != null && !DBService.Connections.Contains(value))
                {
                    DBService.Connections.Add(value);
                }
            }
        }

        [Browsable(false)]
        public string LogSchemaName
        {
            get => logSchemaName;
            set
            {
                if (logSchemaName != value)
                {
                    logSchemaName = value;
                    OnPropertyChanged(nameof(LogSchemaName));
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public DBLogSchema LogSchema
        {
            get { return logSchema ?? (logSchema = (DBLogSchema)DBService.Schems[logSchemaName]); }
            set
            {
                logSchema = value;
                LogSchemaName = value?.Name;
            }
        }

        [Browsable(false)]
        public DBSystem System { get { return Connection?.System ?? DBSystem.Default; } }

        public DBTableList Tables { get; set; }

        public DBTableGroupList TableGroups { get; set; }

        public DBProcedureList Procedures { get; set; }

        public DBSequenceList Sequences { get; set; }

        public override string Name
        {
            get { return base.Name; }
            set
            {
                base.Name = value;
                if (DataBase == null)
                    DataBase = value;
            }
        }

        public string DataBase
        {
            get { return dataBase; }
            set
            {
                if (dataBase != value)
                {
                    dataBase = value;
                    OnPropertyChanged(nameof(DataBase));
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public bool IsSynchronizing { get; internal set; }

        public DBTable GenerateTable(string name)
        {
            return Tables[name] ?? new DBTable<DBItem>(name) { Schema = this };
        }

        public void GenerateTablesInfo(IEnumerable<DBTableInfo> tables)
        {
            foreach (var tableInfo in tables)
            {
                var table = GenerateTable(tableInfo.Name);
                table.Type = tableInfo.View ? DBTableType.View : DBTableType.Table;
                table.Generate(tableInfo);
                if (!Tables.Contains(table))
                {
                    Tables.Add(table);
                }
            }
        }

        #region IFileSerialize Members

        public void Save(string file)
        {
            Serialization.Serialize(this, file);
        }

        public void Save()
        {
            Save(FileName);
        }

        public void Load(string file)
        {
            Serialization.Deserialize(file, this);
        }

        public void Load()
        {
            Load(FileName);
        }

        [Browsable(false)]
        public string FileName
        {
            get
            {
                if (name == null || name.Length == 0)
                    name = "schema";
                if (fileName == "")
                    fileName = "schems" + Path.DirectorySeparatorChar + name + ".xml";
                return fileName;
            }
            set { fileName = value; }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public List<Assembly> Assemblies { get; private set; }
        #endregion

        public async Task Update()
        {
            foreach (var table in Tables)
            {
                await table.Save();
            }
        }

        public string FormatSql()
        {
            var ddl = new StringBuilder();
            System?.Format(ddl, this);
            return ddl.ToString();
        }

        public override string FormatSql(DDLType ddlType)
        {
            var ddl = new StringBuilder();
            System?.Format(ddl, this, ddlType);
            return ddl.ToString();
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }

        public void LoadTablesInfo()
        {
            try
            {
                IsSynchronizing = true;

                foreach (var tableInfo in GetTablesInfo())
                {
                    var table = GenerateTable(tableInfo.Name);
                    table.Type = tableInfo.View ? DBTableType.View : DBTableType.Table;
                    table.Generate(tableInfo);
                    if (!Tables.Contains(table))
                    {
                        Tables.Add(table);
                    }
                }
            }
            finally
            {
                IsSynchronizing = false;
            }
        }

        public IEnumerable<DBTableInfo> GetTablesInfo(string schemaName = null, string tableName = null)
        {
            return System.GetTablesInfo(Connection, schemaName, tableName);
        }

        public void DropDatabase()
        {
            System.DropDatabase(this);
        }

        public void CreateDatabase()
        {
            if (Connection == null)
                throw new InvalidOperationException("Connection is not defined!");
            Helper.Logs.Add(new StateInfo("Load", "Database", "Create Database"));
            System.CreateDatabase(this, Connection);
            if (LogSchema != null)
            {
                if (LogSchema.Connection == null)
                {
                    LogSchema.Connection = Connection;
                }

                if (LogSchema.Connection != Connection)
                {
                    LogSchema.CreateDatabase();
                }
                else
                {
                    LogSchema.CreateSchema();
                }
            }
        }

        public void CreateSchema()
        {
            if (Connection == null)
                throw new InvalidOperationException("Connection is not defined!");
            Helper.Logs.Add(new StateInfo("Load", "Database", "Create Schema"));
            System.CreateSchema(this, Connection);
        }

        public DBLogSchema GenerateLogSchema()
        {
            if (LogSchema == null)
            {
                //var logConnection = connection.Clone();
                //logConnection.Name += "_log";
                LogSchema = new DBLogSchema()
                {
                    Name = Name + "_log",
                    Connection = connection,
                    BaseSchema = this
                };
                foreach (DBTable table in Tables)
                {
                    if (table.IsLoging)
                    {
                        table.GenerateLogTable();
                    }
                }
            }
            return LogSchema;
        }

        internal IEnumerable<DBConstraint> GetConstraints()
        {
            foreach (var table in Tables)
            {
                if (table is IDBVirtualTable)
                    continue;
                foreach (var constraint in table.Constraints)
                    yield return constraint;
            }
        }

        internal IEnumerable<DBForeignKey> GetForeigns()
        {
            foreach (var table in Tables)
            {
                if (table is IDBVirtualTable)
                    continue;
                foreach (var constraint in table.Foreigns)
                    yield return constraint;
            }
        }

        internal IEnumerable<DBIndex> GetIndexes()
        {
            foreach (var table in Tables)
            {
                if (table is IDBVirtualTable)
                    continue;
                foreach (var index in table.Indexes)
                    yield return index;
            }
        }

        public void ExportXHTML(string filename)
        {
            //var doc = new System.Xml.XmlDocument();
            using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
            using (var writer = XmlWriter.Create(stream))
            {
                writer.WriteStartDocument(true);
                writer.WriteStartElement("html");
                writer.WriteElementString("title", DisplayName);
                writer.WriteStartElement("body");
                writer.WriteElementString("H1", $"{DisplayName} ({ DisplayName})");
                var tables = Tables.ToList();
                tables.Sort(new InvokerComparer<DBTable>("Name"));
                foreach (var table in tables)
                {
                    if (table.Type == DBTableType.Table && !(table is IDBLogTable))
                    {
                        writer.WriteElementString("H2", table.DisplayName + " (" + table.Name + ")");
                        writer.WriteStartElement("table");
                        writer.WriteAttributeString("border", "1");
                        writer.WriteAttributeString("cellspacing", "0");
                        writer.WriteAttributeString("cellpadding", "5");
                        writer.WriteStartElement("tr");

                        writer.WriteStartElement("th");
                        writer.WriteElementString("p", "Code");
                        writer.WriteEndElement();//th

                        writer.WriteStartElement("th");
                        writer.WriteElementString("p", "Name");
                        writer.WriteEndElement();//th

                        writer.WriteStartElement("th");
                        writer.WriteElementString("p", "Type");
                        writer.WriteEndElement();//th

                        writer.WriteStartElement("th");
                        writer.WriteElementString("p", "Size");
                        writer.WriteEndElement();//th
                        writer.WriteStartElement("th");
                        writer.WriteElementString("p", "Prec");
                        writer.WriteEndElement();//th

                        writer.WriteStartElement("th");
                        writer.WriteElementString("p", "Spec");
                        writer.WriteEndElement();//th

                        writer.WriteStartElement("th");
                        writer.WriteElementString("p", "Reference");
                        writer.WriteEndElement();//th


                        writer.WriteEndElement();//tr

                        foreach (var column in table.Columns)
                        {
                            writer.WriteStartElement("tr");

                            writer.WriteStartElement("td");
                            writer.WriteElementString("p", column.Name);
                            writer.WriteEndElement();//td

                            writer.WriteStartElement("td");
                            writer.WriteElementString("p", column.Name);
                            writer.WriteEndElement();//td

                            writer.WriteStartElement("td");
                            writer.WriteElementString("p", column.DBDataType.ToString());
                            writer.WriteEndElement();//td

                            writer.WriteStartElement("td");
                            writer.WriteElementString("p", column.Size.ToString());
                            writer.WriteEndElement();//td

                            writer.WriteStartElement("td");
                            writer.WriteElementString("p", column.Scale.ToString());
                            writer.WriteEndElement();//td

                            writer.WriteStartElement("td");
                            writer.WriteElementString("p", column.Keys.ToString());
                            writer.WriteEndElement();//td

                            writer.WriteStartElement("td");
                            writer.WriteElementString("p", column.ReferenceTable != null ? (column.ReferenceTable + " (" + column.ReferenceTable.Name + ")") : null);
                            writer.WriteEndElement();//td

                            writer.WriteEndElement();//tr
                        }
                        writer.WriteEndElement();//table
                    }
                }

                writer.WriteEndElement();//body
                writer.WriteEndElement();//html
                writer.WriteEndDocument();
            }
        }

        public DBTable ParseTable(string code)
        {
            if (string.IsNullOrEmpty(code))
                return null;
            DBTable table = null;
            DBSchema schema = null;
            int index = code.IndexOf('.');
            if (index >= 0)
            {
                schema = Schems?[code.Substring(0, index++)];
                int sindex = code.IndexOf('.', index);
                code = sindex < 0 ? code.Substring(index) : code.Substring(index, sindex - index);
            }
            if (schema == null)
                schema = this;

            table = schema.Tables[code];

            if (table == null)
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

        public Task LoadTablesInfoAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    LoadTablesInfo();
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                }
            });
        }

        public void Generate(IEnumerable<Assembly> assemblies)
        {
            Assemblies = new List<Assembly>(assemblies);
            var logSchema = GenerateLogSchema();
            Helper.Logs.Add(new StateInfo("Load", "Database", "Generate Schema"));
            var attributes = new List<TableGenerator>();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetExportedTypes().Where(item => item.IsClass))
                {
                    var tableAttribute = DBTable.GetTableAttribute(type);
                    if (tableAttribute != null)
                    {
                        attributes.Add(tableAttribute);
                    }
                    else
                    {
                        DBTable.GetItemTypeAttribute(type);
                    }
                }
                Procedures.Generate(assembly);
            }

            foreach (var tableAttribute in attributes)
            {
                tableAttribute.Generate(this);
            }
        }

        public string GeneretePatch(IEnumerable<DBItem> items)
        {
            var rez = new StringBuilder();

            foreach (var item in items)
            {
                rez.Append(((DBItem)item).FormatPatch());
            }
            return rez.ToString();
        }

        public IEnumerable<DBForeignKey> GetChildRelations(DBTable dBTable)
        {
            if (!cacheRelation)
            {
                foreach (var table in Tables)
                {
                    table.ChildRelations.Clear();
                }
                foreach (var table in Tables)
                {
                    table.Foreigns.CacheChildRelations();
                }
                cacheRelation = true;

            }
            return dBTable.ChildRelations;
        }

        [Invoker(typeof(DBSchema), nameof(DBSchema.DataBase))]
        public class DataBaseInvoker<T> : Invoker<T, string> where T : DBSchema
        {
            public static readonly DataBaseInvoker<T> Instance = new DataBaseInvoker<T>();
            public override string Name => nameof(DBSchema.DataBase);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.DataBase;

            public override void SetValue(T target, string value) => target.DataBase = value;
        }

        [Invoker(typeof(DBSchema), nameof(DBSchema.ConnectionName))]
        public class ConnectionNameInvoker<T> : Invoker<T, string> where T: DBSchema
        {
            public static readonly ConnectionNameInvoker<T> Instance = new ConnectionNameInvoker<T>();
            public override string Name => nameof(DBSchema.ConnectionName);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.ConnectionName;

            public override void SetValue(T target, string value) => target.ConnectionName = value;
        }

        [Invoker(typeof(DBSchema), nameof(DBSchema.Connection))]
        public class ConnectionInvoker<T> : Invoker<T, DBConnection> where T : DBSchema
        {
            public static readonly ConnectionInvoker<T> Instance = new ConnectionInvoker<T>();
            public override string Name => nameof(DBSchema.Connection);

            public override bool CanWrite => true;

            public override DBConnection GetValue(T target) => target.Connection;

            public override void SetValue(T target, DBConnection value) => target.Connection = value;
        }

        [Invoker(typeof(DBSchema), nameof(DBSchema.LogSchemaName))]
        public class LogSchemaNameInvoker<T> : Invoker<T, string> where T : DBSchema
        {
            public static readonly LogSchemaNameInvoker<T> Instance = new LogSchemaNameInvoker<T>();
            public override string Name => nameof(DBSchema.LogSchemaName);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.LogSchemaName;

            public override void SetValue(T target, string value) => target.LogSchemaName = value;
        }

        [Invoker(typeof(DBSchema), nameof(DBSchema.LogSchema))]
        public class LogSchemaInvoker<T> : Invoker<T, DBLogSchema> where T : DBSchema
        {
            public static readonly LogSchemaInvoker<T> Instance = new LogSchemaInvoker<T>();
            public override string Name => nameof(DBSchema.LogSchema);

            public override bool CanWrite => true;

            public override DBLogSchema GetValue(T target) => target.LogSchema;

            public override void SetValue(T target, DBLogSchema value) => target.LogSchema = value;
        }

        [Invoker(typeof(DBSchema), nameof(DBSchema.Tables))]
        public class TablesInvoker<T> : Invoker<T, DBTableList> where T : DBSchema
        {
            public static readonly TablesInvoker<T> Instance = new TablesInvoker<T>();
            public override string Name => nameof(DBSchema.Tables);

            public override bool CanWrite => true;

            public override DBTableList GetValue(T target) => target.Tables;

            public override void SetValue(T target, DBTableList value) => target.Tables = value;
        }

        [Invoker(typeof(DBSchema), nameof(DBSchema.TableGroups))]
        public class TableGroupsInvoker<T> : Invoker<T, DBTableGroupList> where T : DBSchema
        {
            public static readonly TableGroupsInvoker<T> Instance = new TableGroupsInvoker<T>();
            public override string Name => nameof(DBSchema.TableGroups);

            public override bool CanWrite => true;

            public override DBTableGroupList GetValue(T target) => target.TableGroups;

            public override void SetValue(T target, DBTableGroupList value) => target.TableGroups = value;
        }

        [Invoker(typeof(DBSchema), nameof(DBSchema.Procedures))]
        public class ProceduresInvoker<T> : Invoker<T, DBProcedureList> where T : DBSchema
        {
            public static readonly ProceduresInvoker<T> Instance = new ProceduresInvoker<T>();
            public override string Name => nameof(DBSchema.Procedures);

            public override bool CanWrite => true;

            public override DBProcedureList GetValue(T target) => target.Procedures;

            public override void SetValue(T target, DBProcedureList value) => target.Procedures = value;
        }

        [Invoker(typeof(DBSchema), nameof(DBSchema.Sequences))]
        public class SequencesInvoker<T> : Invoker<T, DBSequenceList> where T : DBSchema
        {
            public static readonly SequencesInvoker<T> Instance = new SequencesInvoker<T>();
            public override string Name => nameof(DBSchema.Sequences);

            public override bool CanWrite => true;

            public override DBSequenceList GetValue(T target) => target.Sequences;

            public override void SetValue(T target, DBSequenceList value) => target.Sequences = value;
        }
    }
}
