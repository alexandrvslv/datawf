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
using System.Collections.Generic;

namespace DataWF.Data
{
    public class DBForeignList : DBConstraintList<DBForeignKey>
    {

        public DBForeignList(DBTable table) : base(table)
        {
            Indexes.Add(DBForeignKey.ReferenceNameInvoker.Instance);
            Indexes.Add(DBForeignKey.ReferenceTableNameInvoker.Instance);
            Indexes.Add(DBForeignKey.PropertyInvoker.Instance);
        }

        public DBForeignKey GetForeignByColumn(DBColumn column)
        {
            return SelectOne(nameof(DBForeignKey.ColumnName), CompareType.Equal, column.FullName);
        }

        public DBForeignKey GetByProperty(string property)
        {
            return SelectOne(nameof(DBForeignKey.Property), CompareType.Equal, property);
        }

        public IEnumerable<DBForeignKey> GetByReference(DBColumn reference)
        {
            return Select(DBForeignKey.ReferenceNameInvoker.Instance, CompareType.Equal, reference.FullName);
        }

        public IEnumerable<DBForeignKey> GetByReference(DBTable reference)
        {
            return Select(DBForeignKey.ReferenceTableNameInvoker.Instance, CompareType.Equal, reference.FullName);
        }

        public DBForeignKey GetByColumns(DBColumn column, DBColumn reference)
        {
            foreach (var item in items)
            {
                if (item.Column == column && item.Reference == reference)
                {
                    return item;
                }
            }
            return null;
        }

        public void CacheChildRelations()
        {
            foreach (var item in this)
            {
                var table = item.ReferenceTable;
                if (table != null)
                {
                    table.ChildRelations.Add(item);
                }
                if (table.IsVirtual)
                {
                    table.BaseTable.ChildRelations.Add(item);
                }
            }
        }
    }


}
