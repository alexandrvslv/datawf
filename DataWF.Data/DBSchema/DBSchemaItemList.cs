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
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBSchemaItemList<T> : SelectableList<T> where T : DBSchemaItem
    {
        private IListIndex<T, string> nameIndex;

        public DBSchemaItemList()
            : this(null)
        { }

        public DBSchemaItemList(IDBSchema schema)
            : base()
        {
            nameIndex = Indexes.Add(DBSchemaItem.NameInvoker.Instance);
            Schema = schema;
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public virtual IDBSchema Schema { get; internal set; }

        public virtual T this[string name]
        {
            get => nameIndex.SelectOne(name);
            set
            {
                int i = GetIndexByName(name);
                value.Name = name;
                if (i < 0)
                {
                    Add(value);
                }
                else
                {
                    this[i] = value;
                }
            }
        }

        protected int GetIndexByName(string name)
        {
            var item = this[name];
            return item == null ? -1 : IndexOf(item);
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
            ((DBSchema)Schema)?.OnChanged(item, GetRemoveType(item));
            return flag;
        }

        public override int Add(T item)
        {
            if (item.Schema != null && item.Schema != Schema)
            {
                throw new Exception($"item {item} already attached to {item.Schema}");
            }
            var index = base.Add(item);
            if (index > -1)
            {
                ((DBSchema)Schema)?.OnChanged(item, GetInsertType(item));
            }
            return index;
        }

        public override void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnItemPropertyChanged(sender, e);
            if (Schema != null
                && !Schema.IsSynchronizing
                && Schema.Provider != null)
            {
                Schema.Provider.OnItemsListChanged(this, e);
            }
        }

        public override NotifyCollectionChangedEventArgs OnCollectionChanged(NotifyCollectionChangedAction type, object item = null, int index = -1, int oldIndex = -1, object oldItem = null)
        {
            var args = base.OnCollectionChanged(type, item, index, oldIndex, oldItem);
            if (Schema != null
                && !Schema.IsSynchronizing
                && Schema.Provider != null)
            {
                Schema.Provider.OnItemsListChanged(this, args ?? ListHelper.GenerateArgs(type, item, index, oldIndex, oldItem));
            }
            return args;
        }

        //public DateTime GetMaxStamp ()
        //{
        //  DateTime value = this [0].Stamp;
        //  for (int i = 1; i < Count; i++)
        //      if (this [i].Stamp > value)
        //          value = this [i].Stamp;
        //  return value;
        //}

        public override void InsertInternal(int index, T item)
        {
            if (Contains(item.Name))
                throw new Exception($"{typeof(T).Name} with name {item.Name} already exist");

            if (item.Schema == null && Schema != null)
                item.Schema = Schema;

            base.InsertInternal(index, item);
        }

        public virtual DDLType GetInsertType(T item)
        {
            return DDLType.Create;
        }

        public virtual DDLType GetRemoveType(T item)
        {
            return DDLType.Drop;
        }

        public override object NewItem()
        {
            T item = (T)base.NewItem();
            item.Schema = Schema;
            return item;
        }
    }
}
