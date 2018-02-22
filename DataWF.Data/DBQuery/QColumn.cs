/*
 QColumn.cs
 
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

namespace DataWF.Data
{
    public class QColumn : QItem
    {
        [NonSerialized]
        protected DBColumn _columnn;
        [NonSerialized]
        private object temp;
        [DefaultValue(null)]
        protected string column;
        [DefaultValue(null)]
        private string prefix;

        public QColumn()
        {
        }

        public QColumn(string code)
            : base(code)
        {
            column = code;
        }

        public QColumn(DBColumn column)
            : base(column.Name)
        {
            Column = column;
        }

        public string ColumnCode
        {
            get { return column; }
            set
            {
                if (column != value)
                {
                    column = value;
                    _columnn = null;
                }
                OnPropertyChanged(nameof(ColumnCode));
            }
        }

        public DBColumn Column
        {
            get
            {
                if (_columnn == null && column != null)
                    _columnn = base.Table != null ? base.Table.ParseColumn(column) : DBService.ParseColumn(column);
                return _columnn;
            }
            set
            {
                if (Column != value)
                {
                    ColumnCode = value == null ? null : value.FullName;
                    Text = value == null ? null : value.Name;
                    //prefix = value.Table.Code;
                    _columnn = value;
                }
            }
        }

        public override DBTable Table
        {
            get { return Column == null ? base.Table : Column.Table; }
            set { }
        }

        public override void Dispose()
        {
            _columnn = null;
            base.Dispose();
        }

        public string FullName
        {
            get { return $"{Table}.{Column}"; }
        }

        public override string Format(IDbCommand command = null)
        {
            if (Column == null)
                return text;
            else if (command != null && (Column.ColumnType == DBColumnTypes.Internal || Column.ColumnType == DBColumnTypes.Expression))
                return string.Empty;
            else if (Column.ColumnType == DBColumnTypes.Query && Column.Table.Type != DBTableType.View)
                return string.Format("({0}) as {1}", Column.Query, text);
            else
                return string.Format("{0}{1}{2}", prefix != null ? (prefix + ".") : "", text, alias != null ? (" as " + alias) : "");
        }

        public override object GetValue(DBItem row)
        {
            return temp ?? row.GetValue(Column);
        }

        public string Prefix
        {
            get { return prefix; }
            set
            {
                if (prefix != value)
                {
                    prefix = value;
                    OnPropertyChanged(nameof(Prefix));
                }
            }
        }
        public object Temp { get { return temp; } set { temp = value; } }

        public override string ToString()
        {
            return Column == null ? base.ToString() : Column.ToString();
        }
    }
}