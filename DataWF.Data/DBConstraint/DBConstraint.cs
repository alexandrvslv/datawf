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
using System;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBConstraint : DBTableItem, IDBTableContent
    {
        protected DBConstaintType type;
        protected string value;
        protected DBColumnReferenceList columns = new DBColumnReferenceList();

        public DBConstraint()
        {
        }

        public virtual void GenerateName()
        {
            name = string.Format("{0}{1}{2}", type.ToString().Substring(0, 2).ToLower(), Table.Name, columns.Names);
        }

        public DBConstaintType Type
        {
            get { return type; }
            set
            {
                if (type == value)
                    return;
                type = value;
                OnPropertyChanged(nameof(Type), true);
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
            }
        }

        public DBColumnReferenceList Columns
        {
            get { return columns; }
        }

        [XmlIgnore]
        public DBColumn Column
        {
            get { return columns.Count == 0 ? null : columns[0].Column; }
            set
            {
                if (columns.Contains(value))
                    return;
                if (value == null)
                    columns.Clear();
                else
                {
                    if (Table == null)
                        Table = value.Table;
                    columns.Add(value);
                }
            }
        }

        [Browsable(false)]
        public string ColumnName
        {
            get { return columns.Count == 0 ? null : columns[0].ColumnName; }
            set
            {
                if (columns.Contains(value))
                    return;
                if (value == null)
                    columns.Clear();
                else
                    columns.Add(value);
            }
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", type, Column, value);
        }

        public override string FormatSql(DDLType ddlType)
        {
            var ddl = new StringBuilder();
            Schema?.Connection?.System.Format(ddl, this, ddlType);
            return ddl.ToString();
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
