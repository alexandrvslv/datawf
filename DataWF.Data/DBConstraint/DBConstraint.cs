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
using System.ComponentModel;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [InvokerGenerator(Instance = true)]
    public partial class DBConstraint : DBTableItem, IDBTableContent
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
                OnPropertyChanged(DDLType.Alter);
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
                OnPropertyChanged(DDLType.Alter);
            }
        }

        public DBColumnReferenceList Columns { get; set; }

        [XmlIgnore, JsonIgnore]
        public DBColumn Column
        {
            get { return Columns.GetFirst()?.Column; }
            set
            {
                var exist = Columns.Get(value?.FullName);
                if (exist?.Column == value)
                    return;
                if (value == null)
                    Columns.Clear();
                else
                {
                    if (Table == null)
                        Table = value.Table;
                    if (exist != null)
                        exist.Column = value;
                    else
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

        public override string FormatSql(DDLType ddlType, bool dependency = false)
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
    }
}
