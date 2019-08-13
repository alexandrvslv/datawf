/*
 QItemList.cs
 
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
            Indexes.Add(QItemTextInvoker<T>.Instance);
            ApplySort(new InvokerComparer(QItemOrderInvoker<T>.Instance, ListSortDirection.Ascending));
        }

        public QItemList(IEnumerable<T> items) : this()
        {
            AddRangeInternal(items);
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

    [Invoker(typeof(QItem), nameof(QItem.Text))]
    public class QItemTextInvoker<T> : Invoker<T, string> where T : QItem
    {
        public static readonly QItemTextInvoker<T> Instance = new QItemTextInvoker<T>();

        public override string Name => nameof(QItem.Text);

        public override bool CanWrite => true;

        public override string GetValue(T target) => target.Text;

        public override void SetValue(T target, string value) => target.Text = value;
    }

    [Invoker(typeof(QItem), nameof(QItem.Order))]
    public class QItemOrderInvoker<T> : Invoker<T, int> where T : QItem
    {
        public static readonly QItemOrderInvoker<T> Instance = new QItemOrderInvoker<T>();
        public override string Name => nameof(QItem.Order);

        public override bool CanWrite => true;

        public override int GetValue(T target) => target.Order;

        public override void SetValue(T target, int value) => target.Order = value;
    }

}
