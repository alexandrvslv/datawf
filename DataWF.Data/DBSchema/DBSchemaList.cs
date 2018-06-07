/*
 DBSchemaList.cs
 
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
using System;
using System.ComponentModel;

namespace DataWF.Data
{
    public class DBSchemaList : DBSchemaItemList<DBSchema>
    {
        public DBSchemaList() : base()
        { }

        public event EventHandler<ListPropertyChangedEventArgs> ItemsListChanged;

        public override int AddInternal(DBSchema item)
        {
            int index = base.AddInternal(item);
            if (item.LogSchema != null && item.LogSchema.Container == null)
            {
                Add(item.LogSchema);
            }
            return index;
        }

        protected internal void OnItemsListChanged(object sender, ListPropertyChangedEventArgs arg)
        {
            ItemsListChanged?.Invoke(sender, arg);
        }

        public override void OnListChanged(ListChangedType type, int newIndex = -1, int oldIndex = -1, object item = null, string property = null)
        {
            base.OnListChanged(type, newIndex, oldIndex, item, property);
            if (ItemsListChanged != null)
            {
                OnItemsListChanged(this, new ListPropertyChangedEventArgs(type, newIndex, oldIndex) { Sender = item, Property = property });
            }
        }
    }
}
