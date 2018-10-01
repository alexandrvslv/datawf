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
using DataWF.Common;
using System.Data;

namespace DataWF.Data
{
    public class QBetween : QItem, IBetween
    {
        private QItem value1;
        private QItem value2;

        public QBetween(object val1 = null, object val2 = null, DBColumn column = null)
        {
            if (val1 is QItem)
                value1 = (QItem)val1;
            else if (val1 is DBColumn)
                value1 = new QColumn((DBColumn)val1);
            else if (val1 != null)
                value1 = new QValue(val1, column);

            if (val2 is QItem)
                value2 = (QItem)val1;
            else if (val2 is DBColumn)
                value2 = new QColumn((DBColumn)val2);
            else if (val2 != null)
                value2 = new QValue(val2, column);
        }

        public override object GetValue(DBItem row = null)
        {
            return this;
        }

        public override string ToString()
        {
            return base.ToString();// string.Format("{0} {1}", value1, value2);
        }

        public override string Format(IDbCommand command = null)
        {
            string f1 = value1?.Format(command) ?? string.Empty;
            string f2 = value2?.Format(command) ?? string.Empty;
            if (f1.Length == 0 || f2.Length == 0)
                return string.Empty;

            if (Query?.Table?.Schema?.System == DBSystem.MSSql
                || Query?.Table?.Schema?.System == DBSystem.Postgres
                || Query?.Table?.Schema?.System == DBSystem.SQLite)
                return f1 + " and " + f2;
            else
                return string.Format("({0}, {1})", f1, f2);
        }

        public object MaxValue()
        {
            return Max.GetValue();
        }

        public object MinValue()
        {
            return Min.GetValue();
        }

        public QItem Min
        {
            get { return value1; }
            set { value1 = value; }
        }

        public QItem Max
        {
            get { return value2; }
            set { value2 = value; }
        }

    }
}