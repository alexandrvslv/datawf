using DataWF.Common;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBForeignKey : DBConstraint
    {
        private string property;

        public DBForeignKey() : base()
        {
            Type = DBConstraintType.Foreign;
            References = new DBColumnReferenceList { Container = this };
        }

        public DBForeignKey(DBColumn column, DBTable value) : this()
        {
            Column = column;
            Reference = value.PrimaryKey;
        }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public PropertyInfo PropertyInfo { get; set; }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public IInvoker PropertyInvoker { get; set; }

        public string Property
        {
            get { return property; }
            set
            {
                if (property != value)
                {
                    property = value;
                    OnPropertyChanged(nameof(Property));
                }
            }
        }

        public DBColumnReferenceList References { get; set; }

        public override void GenerateName()
        {
            name = string.Format("{0}{1}{2}{3}", Table.Name, Columns.Names, Reference.Table.Name, References.Names);
        }

        [XmlIgnore, JsonIgnore]
        public DBColumn Reference
        {
            get { return References.GetFirst()?.Column; }
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

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public string ReferenceName
        {
            get { return References.GetFirst()?.ColumnName; }
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
