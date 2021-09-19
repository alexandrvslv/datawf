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
using System.ComponentModel;

namespace DataWF.Data
{

    public class QItemList<T> : SelectableList<T>, IQItemList where T : QItem
    {
        protected IQuery query;

        public QItemList(int capacity = 2) : base(capacity)
        {
            ApplySort(new InvokerComparer<T, int>(QItem.OrderInvoker.Instance, ListSortDirection.Ascending));
        }

        public QItemList(IEnumerable<T> items) : this()
        {
            AddRangeInternal(items, false);
        }

        public QItemList(IQItem owner) : this()
        {
            Owner = owner;
        }

        public QItemList(IQItem owner, int capacity) : this(capacity)
        {
            Owner = owner;
        }

        public IQItem Owner { get; set; }

        public IQuery Query => Owner.Query;

        void IQItemList.Add(QItem item)
        {
            this.Add((T)item);
        }

        public void Delete(QItem item)
        {
            Remove((T)item);
        }

        public override int AddInternal(T item)
        {
            item.Order = Count;
            return base.AddInternal(item);
        }

        public override void InsertInternal(int index, T item)
        {
            var itemList = item.Container;
            if (itemList != this && itemList != null)
            {
                itemList.Delete(item);
            }
            item.Container = this;
            if (item.Order == -1)
                item.Order = index;

            base.InsertInternal(index, item);
        }

        public override void Dispose()
        {
            query = null;
            foreach (T c in this)
                c.Dispose();
            base.Dispose();
        }

        public IEnumerable<IT> GetAllQItems<IT>() where IT : IQItem
        {
            foreach (var item in this)
            {
                if (item is IT typeItem)
                    yield return typeItem;
                else if (item is IQItemList list)
                {
                    foreach (var subItem in list.GetAllQItems<IT>())
                    {
                        yield return subItem;
                    }
                }

            }
        }
    }
}
