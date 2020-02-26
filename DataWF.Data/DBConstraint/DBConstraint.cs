/*
 DBConstrain.cs
 
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
using System.ComponentModel;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBConstraint : DBTableItem, IDBTableContent
    {
        protected DBConstraintType type;
        protected string value;

        public DBConstraint()
        {
            Columns = new DBColumnReferenceList { Container = this };
        }

        public virtual void GenerateName()
        {
            name = string.Format("{0}{1}{2}", type.ToString().Substring(0, 2).ToLower(), Table.Name, Columns.Names);
        }

        public DBConstraintType Type
        {
            get { return type; }
            set
            {
                if (type == value)
                    return;
                type = value;
                OnPropertyChanged(nameof(Type), DDLType.Alter);
            }
        }

        public string Value
        {
            get { return value; }
            set
            {
                if (value == this.value)
                    return;
                this.value = value;
                OnPropertyChanged(nameof(Value), DDLType.Alter);
            }
        }

        public DBColumnReferenceList Columns { get; set; }

        [XmlIgnore, JsonIgnore]
        public DBColumn Column
        {
            get { return Columns.GetFirst()?.Column; }
            set
            {
                if (Columns.Contains(value))
                    return;
                if (value == null)
                    Columns.Clear();
                else
                {
                    if (Table == null)
                        Table = value.Table;
                    Columns.Add(value);
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public string ColumnName
        {
            get { return Columns.GetFirst().ColumnName; }
            set
            {
                if (Columns.Contains(value))
                    return;
                if (value == null)
                    Columns.Clear();
                else
                    Columns.Add(value);
            }
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", type, Column, value);
        }

        public override string FormatSql(DDLType ddlType)
        {
            var builder = new StringBuilder();
            Table?.System.Format(builder, this, ddlType);
            return builder.ToString();
        }

        public override object Clone()
        {
            var constraint = new DBConstraint()
            {
                Name = Name,
                Type = Type,
                Column = Column,
                Value = Value,
            };
            foreach (var item in Columns)
            {
                constraint.Columns.Add(item.Clone());
            }
            return constraint;
        }

        [Invoker(typeof(DBConstraint), nameof(DBConstraint.Type))]
        public class TypeInvoker<T> : Invoker<T, DBConstraintType> where T : DBConstraint
        {
            public static readonly TypeInvoker<T> Instance = new TypeInvoker<T>();
            public override string Name => nameof(DBConstraint.Type);

            public override bool CanWrite => true;

            public override DBConstraintType GetValue(T target) => target.Type;

            public override void SetValue(T target, DBConstraintType value) => target.Type = value;
        }

        [Invoker(typeof(DBConstraint), nameof(DBConstraint.Columns))]
        public class ColumnsInvoker<T> : Invoker<T, DBColumnReferenceList> where T : DBConstraint
        {
            public static readonly ColumnsInvoker<T> Instance = new ColumnsInvoker<T>();
            public override string Name => nameof(DBConstraint.Columns);

            public override bool CanWrite => true;

            public override DBColumnReferenceList GetValue(T target) => target.Columns;

            public override void SetValue(T target, DBColumnReferenceList value) => target.Columns = value;
        }

        [Invoker(typeof(DBConstraint), nameof(DBConstraint.ColumnName))]
        public class ColumnNameInvoker<T> : Invoker<T, string> where T : DBConstraint
        {
            public static readonly ColumnNameInvoker<T> Instance = new ColumnNameInvoker<T>();
            public override string Name => nameof(DBConstraint.ColumnName);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.ColumnName;

            public override void SetValue(T target, string value) => target.ColumnName = value;
        }

        [Invoker(typeof(DBConstraint), nameof(DBConstraint.Value))]
        public class ValueInvoker<T> : Invoker<T, string> where T : DBConstraint
        {
            public static readonly ValueInvoker<T> Instance = new ValueInvoker<T>();
            public override string Name => nameof(DBConstraint.Value);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.Value;

            public override void SetValue(T target, string value) => target.Value = value;
        }
    }
}
