/*
 DBRowEventArgs.cs
 
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
using System.Data;
using System.Linq;

namespace DataWF.Data
{
    public enum DBItemMethod
    {
        Accept,
        Attach,
        Change,
        Update
    }

    public class DBItemEventArgs : CancelEventArgs
    {
        private DBItem row;
        private DBUpdateState state;
        private DBColumn column;
        private object value;
        private string property = string.Empty;
        private List<DBColumn> columns;

        public DBItemEventArgs(DBItem item, DBColumn Column = null, string property = null, object Value = null)
        {
            this.row = item;
            this.state = item.DBState;
            this.column = Column;
            this.value = Value;
            this.Property = property;
            this.columns = row.GetChangeKeys().ToList();
        }

        public DBUpdateState State
        {
            get { return state; }
            set { state = value; }
        }

        public DBColumn Column
        {
            get { return column; }
        }

        public string Property
        {
            get { return property; }
            private set { property = value ?? string.Empty; }
        }

        public object Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public DBItem Row
        {
            get { return row; }
        }

        public List<DBColumn> Columns
        {
            get { return columns; }
            set { columns = value; }
        }

        public DBTransaction Transaction { get; set; }

        public bool StateAdded(DBUpdateState filter)
        {
            return (state & filter) != filter && (row.DBState & filter) == filter;
        }

        public bool StateRemoved(DBUpdateState filter)
        {
            return (state & filter) == filter && (row.DBState & filter) != filter; ;
        }
    }
}
