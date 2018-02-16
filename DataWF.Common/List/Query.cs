using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DataWF.Common
{
    public class Query
    {
        private QueryParameterList parameters;
        private List<QueryOrder> orders;

        public Query()
        { }

        public Query(IEnumerable<QueryParameter> parameters)
        {
            Parameters.AddRange(parameters);
        }

        public QueryParameterList Parameters
        {
            get { return parameters ?? (parameters = new QueryParameterList()); }
            set { parameters = value; }
        }

        public List<QueryOrder> Orders
        {
            get { return orders ?? (orders = new List<QueryOrder>()); }
            set { orders = value; }
        }

        public void Sort<T>(IList<T> list)
        {
            if (Orders.Count > 0)
            {
                var comparer = new InvokerComparerList<T>();
                foreach (QueryOrder order in Orders)
                    comparer.Comparers.Add(new InvokerComparer<T>(order.Property, order.Direction));
                ListHelper.QuickSort(list, comparer);
            }
        }

        public void Sort(IList list)
        {
            if (Orders.Count > 0 && list.Count > 0)
            {
                var comparer = new InvokerComparerList();
                foreach (QueryOrder order in Orders)
                    comparer.Comparers.Add(new InvokerComparer(list[0].GetType(), order.Property, order.Direction));
                ListHelper.QuickSort(list, comparer);
            }
        }
    }

}

