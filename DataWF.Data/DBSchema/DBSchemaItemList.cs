/*
 DBSchemaItemList.cs
 
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
    public class DBSchemaItemList<T> : SelectableList<T> where T : DBSchemaItem
    {
        [NonSerialized()]
        protected DBSchema schema;

        public DBSchemaItemList()
            : this(null)
        { }

        public DBSchemaItemList(DBSchema schema)
            : base()
        {
            Indexes.Add(new Invoker<DBSchemaItem, string>(nameof(DBSchemaItem.Name), (item) => item.Name));
            this.schema = schema;
        }

        [Browsable(false)]
        public DBSchema Schema
        {
            get { return schema; }
            internal set { schema = value; }
        }

        public virtual T this[string name]
        {
            get { return SelectOne(nameof(DBSchemaItem.Name), CompareType.Equal, name); }
            set
            {
                int i = GetIndexByName(name);
                this[i] = value;
            }
        }

        protected int GetIndexByName(string name)
        {
            for (int i = 0; i < this.Count; i++)
                if (name == null && this[i].Name == null)
                    return i;
                else if (name == null || this[i].Name == null)
                    continue;
                else if (this[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        public bool Contains(string name)
        {
            return this[name] != null;
        }

        public void Remove(string name)
        {
            T item = this[name];
            if (item != null)
                Remove(item);
        }

        public override bool Remove(T item)
        {
            bool flag = base.Remove(item);
            DBService.OnDBSchemaChanged(item, DDLType.Drop);
            return flag;
        }

        //public DateTime GetMaxStamp ()
        //{
        //  DateTime value = this [0].Stamp;
        //  for (int i = 1; i < Count; i++)
        //      if (this [i].Stamp > value)
        //          value = this [i].Stamp;
        //  return value;
        //}

        public override void Add(T item)
        {
            if (Contains(item.Name))
                throw new Exception($"{typeof(T).Name} with name {item.Name} already exist");

            if (item.Schema == null && schema != null)
                item.Schema = schema;

            base.Add(item);
            DBService.OnDBSchemaChanged(item, DDLType.Create);
        }

        public override object NewItem()
        {
            T item = (T)EmitInvoker.CreateObject(typeof(T));
            item.Schema = schema;
            return item;
        }
    }
}
