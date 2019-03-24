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
using Newtonsoft.Json;
using System.ComponentModel;
using System.Text;
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
    }
}
