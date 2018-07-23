using DataWF.Common;
/*
 DBConstraintList.cs
 
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
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Data
{
    public class DBConstraintList<T> : DBTableItemList<T> where T : DBConstraint, new()
    {
        static readonly Invoker<T, string> columnNameInvoker = new Invoker<T, string>(nameof(DBConstraint.ColumnName), (item) => item.ColumnName);

        public DBConstraintList(DBTable table) : base(table)
        {
            Indexes.Add(columnNameInvoker);
        }

        public IEnumerable<T> GetByColumn(DBColumn column)
        {
            return Select(nameof(DBConstraint.ColumnName), CompareType.Equal, column.FullName);
        }

        public IEnumerable<T> GetByColumnAndTYpe(DBColumn column, DBConstraintType type)
        {
            return Select(nameof(DBConstraint.ColumnName), CompareType.Equal, column.FullName).Where(p => p.Type == type);
        }

        public IEnumerable<T> GetByValue(string value)
        {
            return Select(nameof(DBConstraint.Value), CompareType.Equal, value);
        }
    }
}
