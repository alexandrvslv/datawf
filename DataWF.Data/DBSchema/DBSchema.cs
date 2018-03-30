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
using System.Xml;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBSchema : DBSchemaItem, IFileSerialize
    {
        private string connectionName = "";
        protected string dataBase = "";
        protected string fileName = "";
        protected DBConnection connection;

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

        public DBTable GenerateTable(string name)
        {
            DBTable table = null;
            table = Tables[name];
            if (table == null)
            {
                table = new DBTable<DBItem>(name);
                Tables.Add(table);
            }
            return table;
        }

        public void GenerateTables(IEnumerable<DBTableInfo> tables)
        {
            foreach (var tableInfo in tables)
            {
                var table = GenerateTable(tableInfo.Name);
                table.Type = tableInfo.View ? DBTableType.View : DBTableType.Table;
                table.GenerateColumns(tableInfo);
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
        public DBSystem System { get { return Connection?.System; } }

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

        public void Update()
        {
            foreach (var table in Tables)
            {
                table.Save();
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

        public List<DBTableInfo> GetTablesInfo(string schemaName = null, string tableName = null)
        {
            return System.GetTablesInfo(Connection, schemaName, tableName);
        }

        public void CreateDatabase()
        {
            Connection.System.DropDatabase(this);

            Connection.ExecuteGoQuery(FormatSql(DDLType.Create), true);

            if (Connection.Schema?.Length > 0)
            {
                if (Connection.System == DBSystem.Oracle)
                {
                    Connection.User = Name;
                }
            }
            if (string.IsNullOrEmpty(Connection.DataBase))// Connection.System != DBSystem.SQLite
            {
                Connection.DataBase = Name;
            }

            Connection.ExecuteGoQuery(FormatSql(), true);
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
    }
}
