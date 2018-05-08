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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBLogSchema : DBSchema
    {
        protected DBSchema baseSchema;
        protected string baseSchemaName;

        public string BaseSchemaName
        {
            get { return baseSchemaName; }
            set
            {
                if (baseSchemaName != value)
                {
                    baseSchemaName = value;
                    OnPropertyChanged(nameof(BaseSchemaName), false);
                }
            }
        }

        [XmlIgnore]
        public DBSchema BaseSchema
        {
            get { return baseSchema ?? (baseSchema = DBService.Schems[BaseSchemaName]); }
            set
            {
                baseSchema = value;
                BaseSchemaName = value?.Name;
            }
        }
    }

    public class DBSchema : DBSchemaItem, IFileSerialize
    {
        protected DBConnection connection;
        private string connectionName = string.Empty;
        protected string dataBase = "";
        protected string fileName = "";
        protected DBLogSchema logSchema;
        protected string logSchemaName;

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
        public string ConnectionName { get => connectionName; set => connectionName = value; }

        [XmlIgnore]
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
                    OnPropertyChanged(nameof(LogSchemaName), false);
                }
            }
        }

        [XmlIgnore]
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
                    OnPropertyChanged(nameof(DataBase), false);
                }
            }
        }

        [XmlIgnore]
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
                if (table.Container == null)
                    Tables.Add(table);
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
        #endregion

        public void Update()
        {
            foreach (var table in Tables)
            {
                table.Save();
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
                    if (table.Container == null)
                        Tables.Add(table);
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

        public void CreateDatabase()
        {
            if (Connection == null)
                throw new InvalidOperationException("Connection is not defined!");
            Helper.Logs.Add(new StateInfo("Load", "Database", "Create Database"));
            System.CreateDatabase(this, Connection);
            if (LogSchema != null)
            {
                if (LogSchema.Connection != Connection)
                    LogSchema.CreateDatabase();
                else
                    LogSchema.CreateSchema();
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
                foreach (var constraint in table.Constraints)
                    yield return constraint;
            }
        }

        internal IEnumerable<DBForeignKey> GetForeigns()
        {
            foreach (var table in Tables)
            {
                foreach (var constraint in table.Foreigns)
                    yield return constraint;
            }
        }

        internal IEnumerable<DBIndex> GetIndexes()
        {
            foreach (var table in Tables)
            {
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
                    if (table.Type == DBTableType.Table && !(table is DBLogTable))
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
    }
}
