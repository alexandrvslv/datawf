/*
 DBColumnGroup.cs
 
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBColumnGroup : DBSchemaItem, IComparable, IComparable<DBColumnGroup>, IDBTableContent
    {
        [NonSerialized()]
        protected DBColumnGroupList list;
        [NonSerialized()]
        protected DBTable table;

        protected int order = -1;

        public DBColumnGroup()
            : base()
        {
        }

        public DBColumnGroup(string name)
            : base(name)
        {
        }

        public override string FullName
        {
            get
            {
                return string.Format("{0}.{1}",
                Table == null ? string.Empty : Table.FullName,
                name);
            }
        }

        [Description("Порядковы номер в таблице"), DisplayName("Порядок"), Category("Отображение")]
        public int Order
        {
            get { return order; }
            set
            {
                if (order == value)
                    return;
                order = value;
                OnPropertyChanged(nameof(Order), false);
            }
        }

        [XmlIgnore, Browsable(false)]
        public DBTable Table
        {
            get { return table; }
            set { table = value; }
        }

        [Browsable(false)]
        public override DBSchema Schema
        {
            get { return table == null ? null : table.Schema; }
        }

        public IEnumerable<DBColumn> GetColumns()
        {
            return Table.Columns.GetByGroup(this);
        }

        #region IComparable Members

        public override int CompareTo(object obj)
        {
            return (CompareTo((DBColumnGroup)obj));
        }

        #endregion
        #region IComparable<DBColumn> Members

        public int CompareTo(DBColumnGroup other)
        {
            return order.CompareTo(other.order);
        }

        #endregion
        public override object Clone()
        {
            return new DBColumnGroup()
            {
                name = name,
                order = order
            };
        }

        public override string FormatSql(DDLType ddlType)
        {
            return null;
        }
    }
}
