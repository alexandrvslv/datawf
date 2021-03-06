﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;

namespace DataWF.Common
{
    public class QueryOrdersList<T> : NamedList<InvokerComparer<T>>, ICollection<IComparer>
    {
        public QueryOrdersList()
        {
        }

        public QueryOrdersList(Query<T> query)
        {
            Query = query;
        }

        public QueryOrdersList(Query<T> query, IEnumerable<InvokerComparer<T>> items) : this(query)
        {
            AddRange(items);
        }

        public Query<T> Query { get; set; }

        public InvokerComparer<T> Add(IInvoker invoker, ListSortDirection direction)
        {
            var comparer = (InvokerComparer<T>)null;
            if (invoker is IInvokerExtension invokerExtension)
            {
                comparer = (InvokerComparer<T>)invokerExtension.CreateComparer<T>();
            }
            else
            {
                comparer = new InvokerComparer<T>(invoker, direction);
            };
            Add(comparer);
            return comparer;
        }

        public InvokerComparer<T> AddOrUpdate(IInvoker invoker, ListSortDirection direction)
        {
            var item = this[invoker.Name];
            if (item == null)
            {
                item = Add(invoker, direction);
            }
            item.Direction = direction;
            return item;
        }

        public void CopyTo(IComparer[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public override void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnItemPropertyChanged(sender, e);
            Query.OnOrdersChanged(sender, e);
        }

        public override NotifyCollectionChangedEventArgs OnCollectionChanged(NotifyCollectionChangedAction type, object item = null, int index = -1, int oldIndex = -1, object oldItem = null)
        {
            var args = base.OnCollectionChanged(type, item, index, oldIndex, oldItem);
            Query.OnOrdersChanged(this, args ?? ListHelper.GenerateArgs(type, item, index, oldIndex, oldItem));
            return args;
        }

        void ICollection<IComparer>.Add(IComparer item)
        {
            Add((InvokerComparer<T>)item);
        }

        bool ICollection<IComparer>.Contains(IComparer item)
        {
            return Contains((InvokerComparer)item);
        }

        bool ICollection<IComparer>.Remove(IComparer item)
        {
            return Remove((InvokerComparer<T>)item);
        }

        IEnumerator<IComparer> IEnumerable<IComparer>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string Format()
        {
            if (Count == 0)
                return string.Empty;
            var builder = new StringBuilder(" order by ");
            foreach (var comparer in this)
            {
                comparer.Format(builder);
                builder.Append(",");
            }
            builder.Length--;
            return builder.ToString();
        }
    }
}

