using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DataWF.Common
{
    public class QueryOrdersList<T> : NamedList<InvokerComparer<T>>
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
            var parameter = new InvokerComparer<T>
            {
                Invoker = invoker,
                Direction = direction
            };
            Add(parameter);
            return parameter;
        }

        public InvokerComparer<T> AddOrUpdate(IInvoker invoker, ListSortDirection sortDirection)
        {
            var item = this[invoker.Name];
            if (item == null)
            {
                item = Add(invoker, sortDirection);
            }
            item.Direction = sortDirection;
            return item;
        }

        public override void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnItemPropertyChanged(sender, e);
            Query.OnOrdersChanged(sender, e);
        }

        public override void OnListChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnListChanged(e);
            Query.OnOrdersChanged(this, e);
        }
    }
}

