using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBForeignKey : DBConstraint
    {
        public DBForeignKey()
        {
            Type = DBConstaintType.Foreign;
        }

        public DBForeignKey(DBColumn column, DBTable value) : this()
        {
            Column = column;
            Reference = value.PrimaryKey;
        }

        public DBColumnReferenceList References { get; set; } = new DBColumnReferenceList();

        public override void GenerateName()
        {
            name = string.Format("{0}{1}{2}{3}", Table.Name, Columns.Names, Reference.Table.Name, References.Names);
        }

        [XmlIgnore]
        public DBColumn Reference
        {
            get { return References.Count == 0 ? null : References[0].Column; }
            set
            {
                if (References.Contains(value))
                    return;
                if (value == null)
                    References.Clear();
                else
                    References.Add(value);
            }
        }

        [XmlIgnore, Browsable(false)]
        public string ReferenceName
        {
            get { return References.Count == 0 ? null : References[0].ColumnName; }
            set
            {
                if (References.Contains(value))
                    return;
                if (value == null)
                    References.Clear();
                else
                    References.Add(value);
            }
        }

        [Browsable(false)]
        public DBTable ReferenceTable
        {
            get { return Reference?.Table; }

        }

        [Browsable(false)]
        public string ReferenceTableName
        {
            get { return ReferenceName?.Substring(0, ReferenceName.LastIndexOf(".", StringComparison.Ordinal)); }
        }

        public override string ToString()
        {
            return string.Format("{0}-{1}({2})", Column, ReferenceTable, Reference);
        }
    }
}
