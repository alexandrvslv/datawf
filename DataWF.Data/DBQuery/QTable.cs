/*
 QTable.cs
 
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
using System.Data;

namespace DataWF.Data
{
    public class QTable : QItem
    {
        protected DBTable _table;
        protected string table = null;


        public QTable()
        { }

        public QTable(DBTable table, string alias = null)
        {
            Table = table;
            this.alias = alias;
        }


        public string TableName
        {
            get { return table; }
            set
            {
                if (table != value)
                {
                    table = value;
                    _table = null;
                    OnPropertyChanged(TableName);
                }
            }
        }


        public override DBTable Table
        {
            get
            {
                if (_table == null)
                    _table = DBService.ParseTable(table);
                return _table;
            }
            set
            {
                if (Table != value)
                {
                    TableName = value?.FullName;
                    Text = value?.Name;
                    _table = value;
                    OnPropertyChanged(nameof(Table));
                }
            }
        }

        public override string Format(IDbCommand command = null)
        {
            var schema = Table.Schema.Connection.Schema;

            return string.Format("{0}{1}{2}", Table != null && schema != null && schema.Length > 0 ? (schema + ".") : string.Empty,
                                 command != null && Table is IDBVirtualTable ? Table.SqlName : text,
                alias == null ? "" : " " + alias);
        }
    }
}
