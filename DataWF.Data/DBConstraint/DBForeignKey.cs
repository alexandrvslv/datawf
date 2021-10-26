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
using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

[assembly: Invoker(typeof(DBForeignKey), nameof(DBForeignKey.References), typeof(DBForeignKey.ReferencesInvoker))]
[assembly: Invoker(typeof(DBForeignKey), nameof(DBForeignKey.ReferenceName), typeof(DBForeignKey.ReferenceNameInvoker))]
[assembly: Invoker(typeof(DBForeignKey), nameof(DBForeignKey.Property), typeof(DBForeignKey.PropertyInvoker))]
[assembly: Invoker(typeof(DBForeignKey), nameof(DBForeignKey.ReferenceTableName), typeof(DBForeignKey.ReferenceTableNameInvoker))]
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
            get => property;
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

        public class ReferencesInvoker : Invoker<DBForeignKey, DBColumnReferenceList>
        {
            public static readonly ReferencesInvoker Instance = new ReferencesInvoker();
            public override string Name => nameof(DBForeignKey.References);

            public override bool CanWrite => true;

            public override DBColumnReferenceList GetValue(DBForeignKey target) => target.References;

            public override void SetValue(DBForeignKey target, DBColumnReferenceList value) => target.References = value;
        }

        public class ReferenceNameInvoker : Invoker<DBForeignKey, string>
        {
            public static readonly ReferenceNameInvoker Instance = new ReferenceNameInvoker();
            public override string Name => nameof(DBForeignKey.ReferenceName);

            public override bool CanWrite => true;

            public override string GetValue(DBForeignKey target) => target.ReferenceName;

            public override void SetValue(DBForeignKey target, string value) => target.ReferenceName = value;
        }

        public class PropertyInvoker : Invoker<DBForeignKey, string>
        {
            public static readonly PropertyInvoker Instance = new PropertyInvoker();

            public override bool CanWrite => throw new System.NotImplementedException();

            public override string Name => nameof(DBForeignKey.Property);

            public override string GetValue(DBForeignKey target) => target.Property;

            public override void SetValue(DBForeignKey target, string value) => target.Property = value;
        }

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
