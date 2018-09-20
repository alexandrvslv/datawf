using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Common
{
    public class QueryOrdersList<T> : NamedList<InvokerComparer<T>>
    {
        public QueryOrdersList()
        {
        }

        public QueryOrdersList(IEnumerable<InvokerComparer<T>> items) : this()
        {
            AddRange(items);
        }

        public void Remove(string property)
        {
            var param = this[property];
            if (param != null)
            {
                Remove(param);
            }
        }

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
    }
}

