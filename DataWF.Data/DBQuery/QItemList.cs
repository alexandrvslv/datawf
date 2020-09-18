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
    public interface IQItemList
    {
        IQuery Query { get; }
        IQItemList Owner { get; }
        void Delete(QItem item);
    }

    public class QItemList<T> : SelectableList<T>, IQItemList where T : QItem, new()
    {
        protected IQuery query;

        public QItemList()
        {
            Indexes.Add(QItem.TextInvoker<T>.Instance);
            ApplySort(new InvokerComparer(QItem.OrderInvoker<T>.Instance, ListSortDirection.Ascending));
        }

        public QItemList(IEnumerable<T> items) : this()
        {
            AddRangeInternal(items, false);
        }

        public QItemList(IQItemList owner) : this()
        {
            Owner = owner;
        }

        public IQItemList Owner { get; set; }

        public IQuery Query
        {
            get { return Owner.Query; }
        }

        public T this[string name]
        {
            get { return SelectOne(nameof(QItem.Text), CompareType.Equal, name); }
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
            if (item.List != this)
            {
                if (item.List != null)
                    item.List.Delete(item);
                if (item.Order == -1)
                    item.Order = index;
            }
            base.InsertInternal(index, item);
        }

        public virtual T Add()
        {
            T item = new T() { Text = "Param" + Count.ToString() };
            Add(item);
            return item;
        }

        public override void Dispose()
        {
            query = null;
            foreach (T c in this)
                c.Dispose();
            base.Dispose();
        }
    }
}
