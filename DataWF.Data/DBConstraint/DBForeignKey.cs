using DataWF.Common;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;
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
        public IInvoker Invoker { get; set; }

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

        [XmlIgnore, JsonIgnore]
        public DBColumn Reference
        {
            get => References.GetFirst()?.Column;
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
            get => References.GetFirst()?.ColumnName;
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
        public DBTable ReferenceTable => Reference?.Table;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public string ReferenceTableName => ReferenceName?.Substring(0, ReferenceName.LastIndexOf(".", StringComparison.Ordinal));

        public override void GenerateName()
        {
            name = string.Format("{0}{1}{2}{3}", Table.Name, Columns.Names, Reference.Table.Name, References.Names);
        }

        public override string ToString()
        {
            return string.Format("{0}-{1}({2})", Column, ReferenceTable, Reference);
        }

        [Invoker(typeof(DBForeignKey), nameof(DBForeignKey.References))]
        public class ReferencesInvoker : Invoker<DBForeignKey, DBColumnReferenceList>
        {
            public static readonly ReferencesInvoker Instance = new ReferencesInvoker();
            public override string Name => nameof(DBForeignKey.References);

            public override bool CanWrite => true;

            public override DBColumnReferenceList GetValue(DBForeignKey target) => target.References;

            public override void SetValue(DBForeignKey target, DBColumnReferenceList value) => target.References = value;
        }

        [Invoker(typeof(DBForeignKey), nameof(DBForeignKey.ReferenceName))]
        public class ReferenceNameInvoker : Invoker<DBForeignKey, string>
        {
            public static readonly ReferenceNameInvoker Instance = new ReferenceNameInvoker();
            public override string Name => nameof(DBForeignKey.ReferenceName);

            public override bool CanWrite => true;

            public override string GetValue(DBForeignKey target) => target.ReferenceName;

            public override void SetValue(DBForeignKey target, string value) => target.ReferenceName = value;
        }

        [Invoker(typeof(DBForeignKey), nameof(DBForeignKey.Property))]
        public class PropertyInvoker : Invoker<DBForeignKey, string>
        {
            public static readonly PropertyInvoker Instance = new PropertyInvoker();

            public override bool CanWrite => throw new System.NotImplementedException();

            public override string Name => nameof(DBForeignKey.Property);

            public override string GetValue(DBForeignKey target) => target.Property;

            public override void SetValue(DBForeignKey target, string value) => target.Property = value;
        }

        [Invoker(typeof(DBForeignKey), nameof(DBForeignKey.ReferenceTableName))]
        public class ReferenceTableNameInvoker : Invoker<DBForeignKey, string>
        {
            public static readonly ReferenceTableNameInvoker Instance = new ReferenceTableNameInvoker();

            public override string Name => nameof(DBForeignKey.ReferenceTableName);

            public override bool CanWrite => false;

            public override string GetValue(DBForeignKey target) => target.ReferenceTableName;

            public override void SetValue(DBForeignKey target, string value) { }
        }
    }
}
