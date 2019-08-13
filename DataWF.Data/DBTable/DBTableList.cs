/*
 DBTableList.cs
 
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
using DataWF.Common;

namespace DataWF.Data
{
    public class DBTableList : DBSchemaItemList<DBTable>
    {

        public DBTableList() : this(null)
        { }

        public DBTableList(DBSchema schema) : base(schema)
        {
            Indexes.Add(DBTable.GroupNameInvoker.Instance);
            ApplyDefaultSort();
        }

        public IEnumerable<DBTable> GetByGroup(string name)
        {
            return Select(nameof(DBTable.GroupName), CompareType.Equal, name);
        }

        public override int AddInternal(DBTable item)
        {
            var index = base.AddInternal(item);
            if (item is IDBVirtualTable)
            {
                ((IDBVirtualTable)item).BaseTable.AddVirtual((IDBVirtualTable)item);
            }
            return index;
        }

        public void ApplyDefaultSort()
        {
            ApplySort(new DBTableComparer());
        }
    }
}
