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
using DataWF.Common;
using System.Data;

namespace DataWF.Data
{
    public class QTable : QItem
    {
        protected JoinType join;
        protected DBTable table;
        protected string tableName = null;

        public QTable()
        { }

        public QTable(DBTable table, string alias = null)
        {
            Table = table;
            this.alias = alias;
        }


        public string TableName
        {
            get { return tableName; }
            set
            {
                if (tableName != value)
                {
                    tableName = value;
                    table = null;
                    OnPropertyChanged(nameof(TableName));
                }
            }
        }

        public JoinType Join
        {
            get { return join; }
            set
            {
                if (join != value)
                {
                    join = value;
                    OnPropertyChanged(nameof(Join));
                }
            }
        }


        public override DBTable Table
        {
            get
            {
                if (table == null)
                    table = DBService.Schems.ParseTable(tableName);
                return table;
            }
            set
            {
                if (Table != value)
                {
                    TableName = value?.FullName;
                    Text = value?.Name;
                    table = value;
                    OnPropertyChanged(nameof(Table));
                }
            }
        }

        public override string Format(IDbCommand command = null)
        {
            var schema = Table.Schema.Connection.Schema;

            return $"{Join.Format()} {Table?.FormatQTable(alias) ?? ($"{text} {alias}")}";
        }
    }
}
