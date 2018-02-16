/*
 DBColumnList.cs
 
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

namespace DataWF.Data
{
    public class DBTableItemList<T> : DBSchemaItemList<T> where T : DBSchemaItem, new()
    {
        [NonSerialized()]
        protected DBTable table;

        public DBTableItemList(DBTable table)
            : base()
        {
            this.table = table;
        }

        public DBTable Table
        {
            get { return table; }
            set { table = value; }
        }

        public override void Add(T item)
        {
            if (table == null)
                throw new InvalidOperationException("Table property nead to be specified before add any item!");

            if (item is IDBTableContent && ((IDBTableContent)item).Table == null)
                ((IDBTableContent)item).Table = table;

            base.Add(item);
        }
    }
}
