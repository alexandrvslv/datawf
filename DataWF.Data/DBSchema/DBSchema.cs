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
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [InvokerGenerator(Instance = true)]
    public partial class DBSchema : DBSchemaItem, IFileSerialize, IDBSchema
    {
        public static DBSchema Generate(string schemaName, params Assembly[] assemblies)
        {
            var schema = new DBSchema(schemaName);
            schema.Generate(assemblies);
            DBService.Schems.Add(schema);
            return schema;
        }

        public static DBSchema Generate(string schemaName, params Type[] types)
        {
            var schema = new DBSchema(schemaName);
            schema.Generate(types);
            DBService.Schems.Add(schema);
            return schema;
        }

        private readonly Dictionary<Type, DBTable> cacheTables = new Dictionary<Type, DBTable>();
        protected DBConnection connection;
        private string connectionName = string.Empty;
        protected string dataBase = "";
        protected string fileName = "";
        protected string logSchemaName;
        private bool cacheRelation;
        private FileDataTable fileTable;
        private IDBSchemaLog logSchema;

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


        public Version Version { get; set; } = new Version(1, 0, 0, 0);

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBSchemaList Schems => Containers.FirstOrDefault(p=>p is DBSchemaList) as DBSchemaList;

        [Browsable(false)]
        public string ConnectionName { get => connectionName; set => connectionName = value; }

        [XmlIgnore, JsonIgnore]
        public DBConnection Connection
        {
            get => connection ?? (connection = DBService.Connections[ConnectionName]);
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
                    OnPropertyChanged();
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public IDBSchemaLog LogSchema
        {
            get => logSchema ?? (logSchema = (IDBSchemaLog)DBService.Schems[logSchemaName]);
            set
            {
                logSchema = value;
                LogSchemaName = value?.Name;
                if (value != null
                    && DBService.Schems.Contains(this)
                    && !DBService.Schems.Contains(value.Name))
                {
                    DBService.Schems.Add((DBSchema)value);
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBSystem System => Connection?.System ?? DBSystem.Default;

        public DBTableList Tables { get; set; }

        public DBTableGroupList TableGroups { get; set; }

        public DBProcedureList Procedures { get; set; }

        public DBSequenceList Sequences { get; set; }

        public override string Name
        {
            get => base.Name;
            set
            {
                base.Name = value;
                if (DataBase == null)
                    DataBase = value;
                if (LogSchema != null)
                {
                    LogSchema.Name = Name + "_log";
                }
            }
        }

        public string DataBase
        {
            get => dataBase;
            set
            {
                if (dataBase != value)
                {
                    dataBase = value;
                    OnPropertyChanged(nameof(DataBase));
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public FileDataTable FileTable => fileTable ??= (FileDataTable)GetTable<FileData>();

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
            set => fileName = value;
        }

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

        public override string FormatSql(DDLType ddlType, bool dependency = false)
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

        public virtual IDBSchemaLog GenerateLogSchema()
        {
            if (LogSchema == null)
            {
                //var logConnection = connection.Clone();
                //logConnection.Name += "_log";
                LogSchema = new DBSchemaLog()
                {
                    Name = Name + "_log",
                    Connection = connection,
                    TargetSchema = this
                };
            }
            return LogSchema;
        }

        internal IEnumerable<DBConstraint> GetConstraints()
        {
            foreach (var table in Tables)
            {
                if (table.IsVirtual)
                    continue;
                foreach (var constraint in table.Constraints)
                    yield return constraint;
            }
        }

        internal IEnumerable<DBForeignKey> GetForeigns()
        {
            foreach (var table in Tables)
            {
                if (table.IsVirtual)
                    continue;
                foreach (var constraint in table.Foreigns)
                    yield return constraint;
            }
        }

        internal IEnumerable<DBIndex> GetIndexes()
        {
            foreach (var table in Tables)
            {
                if (table.IsVirtual)
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
                tables.Sort(new InvokerComparer<DBTable, string>("Name"));
                foreach (var table in tables)
                {
                    if (table.Type == DBTableType.Table && !(table is IDBTableLog))
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
                            writer.WriteElementString("p", column.PropertyName);
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
            var types = new List<Type>();
            foreach (var assembly in assemblies)
            {
                Procedures.Generate(assembly);
                types.AddRange(assembly.GetExportedTypes()
                    .Where(item => item.IsClass));
            }
            Generate(types);
        }

        public IDBTable GetVirtualTable<T>(int itemType) where T : DBItem
        {
            var table = GetTable<T>();
            return table.GetVirtualTable(itemType);
        }

        public DBTable<T> GetTable<T>(bool generate = false) where T : DBItem
        {
            return (DBTable<T>)GetTable(typeof(T), generate);
        }

        public DBTable GetTable(Type type, bool generate = false)
        {
            if (type == null)
                return null;
            if (!cacheTables.TryGetValue(type, out var table))
            {
                var itemGenerator = TableGenerator.GetItemType(type);
                if (itemGenerator != null)
                {
                    if (!itemGenerator.IsGenerated(this, out table) && generate)
                        table = itemGenerator.Generate(this);
                    else if (!generate)
                        table = Tables[itemGenerator.Type.Name];

                    if (table != null)
                        return cacheTables[type] = table;
                }
                else
                {
                    var tableGenerator = TableGenerator.Get(type);
                    if (tableGenerator != null && tableGenerator.ItemType == type)
                    {
                        if (!tableGenerator.IsGenerated(this, out table) && generate)
                            table = tableGenerator.Generate(this);
                        else if (!generate)
                            table = Tables[tableGenerator.Attribute.TableName];

                        if (table != null)
                            return cacheTables[type] = table;
                    }
                    else
                    {
                        cacheTables[type] = null;
                    }
                }
            }
            return table;
        }

        public virtual void Generate(string name)
        {
            Name = name;
        }

        public void Generate(IEnumerable<Type> types)
        {
            var logSchema = GenerateLogSchema();
            Helper.Logs.Add(new StateInfo("Load", "Database", "Generate Schema"));
            var tableGenerators = new HashSet<TableGenerator>();
            var logTableGenerators = new HashSet<LogTableGenerator>();
            foreach (var type in types)
            {
                var tableGenerator = TableGenerator.Get(type);
                if (tableGenerator != null)
                {
                    if (tableGenerator is LogTableGenerator logTableGenerator)
                    {
                        logTableGenerators.Add(logTableGenerator);
                    }
                    else
                    {
                        tableGenerators.Add(tableGenerator);
                    }
                }
                else if (TypeHelper.IsInterface(type, typeof(IExecutable)))
                {
                    Procedures.Generate(type);
                }
            }

            foreach (var tableGenerator in tableGenerators)
            {
                var table = tableGenerator.Generate(this);
                table.RemoveDeletedColumns();
            }

            foreach (var logTableGenerator in logTableGenerators)
            {
                var table = logTableGenerator.Generate(logSchema);
                table.RemoveDeletedColumns();
            }

            foreach (DBTable table in Tables)
            {
                if (!(table is IDBTableLog)
                    && !table.IsVirtual
                    && table.IsLoging)
                {
                    table.GenerateLogTable();
                }
            }

            Procedures.CheckDeleted();
        }

        public string GeneretePatch(IEnumerable<DBItem> items)
        {
            var rez = new StringBuilder();

            foreach (var item in items)
            {
                rez.Append(item.FormatPatch());
            }
            return rez.ToString();
        }

        public IEnumerable<DBForeignKey> GetChildRelations(DBTable target)
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
            return target.ChildRelations;
        }
    }
}
