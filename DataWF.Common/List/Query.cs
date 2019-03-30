using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace DataWF.Common
{
    public class Query<T> : IQuery
    {
        private QueryParameterList<T> parameters;
        private QueryOrdersList<T> orders;
        private bool suspend;

        public Query()
        { }

        public Query(IEnumerable<QueryParameter<T>> parameters)
        {
            Parameters.AddRange(parameters);
        }

        public bool Suspending
        {
            get => suspend;
            set
            {
                if (suspend != value)
                {
                    suspend = value;
                    if (!suspend)
                    {
                        OnParametersChanged(Parameters, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    }
                }
            }
        }
        public bool HaveGlobal => ((IEnumerable<QueryParameter<T>>)Parameters).Any(p => p.IsEnabled && p.IsGlobal);

        public QueryParameterList<T> Parameters
        {
            get { return parameters ?? (Parameters = new QueryParameterList<T>(this)); }
            set
            {
                if (parameters != value)
                {
                    parameters = value;
                    if (parameters != null)
                    {
                        parameters.Query = this;
                    }
                }
            }
        }

        public QueryOrdersList<T> Orders
        {
            get { return orders ?? (Orders = new QueryOrdersList<T>(this)); }
            set
            {
                if (orders != value)
                {
                    orders = value;
                    if (orders != null)
                    {
                        orders.Query = this;
                    }
                }
            }
        }

        public event EventHandler OrdersChanged;

        public event EventHandler ParametersChanged;

        internal void OnOrdersChanged(object sender, EventArgs e)
        {
            OrdersChanged?.Invoke(sender, e);
        }

        internal void OnParametersChanged(object sender, EventArgs e)
        {
            if (!Suspending)
            {
                ParametersChanged?.Invoke(sender, e);
            }
        }

        ICollection<IQueryParameter> IQuery.Parameters
        {
            get { return Parameters; }
        }

        ICollection<IComparer> IQuery.Orders
        {
            get { return Orders; }
        }

        public bool IsEnabled
        {
            get { return ((IEnumerable<QueryParameter<T>>)Parameters).Any(p => !p.FormatIgnore && p.IsEnabled); }
        }

        public void Clear()
        {
            Parameters.Clear();
        }

        public void Add(IQueryParameter parameter)
        {
            Add((QueryParameter<T>)parameter);
        }

        public void Add(QueryParameter<T> parameter)
        {
            Parameters.Add(parameter);
        }

        public QueryParameter<T> Add(LogicType logic, IInvoker invoker, CompareType comparer, object value, QueryGroup group = QueryGroup.None)
        {
            return Parameters.Add(logic, invoker, comparer, value, group);
        }

        public QueryParameter<T> AddOrUpdate(IInvoker invoker, object value)
        {
            var parameter = Parameters[invoker.Name];
            return AddOrUpdate(parameter?.Logic ?? LogicType.And, invoker, parameter?.Comparer ?? CompareType.Equal, value);
        }

        public QueryParameter<T> AddOrUpdate(LogicType logic, IInvoker invoker, CompareType comparer, object value, QueryGroup group = QueryGroup.None)
        {
            var parameter = Parameters[invoker.Name];
            if (parameter == null)
            {
                parameter = Parameters.Add(logic, invoker, comparer, null);
            }
            parameter.Logic = logic;
            parameter.Comparer = comparer;
            parameter.Value = value;
            parameter.Group = group;
            return parameter;
        }

        IQueryParameter IQuery.Add(LogicType logic, IInvoker invoker, CompareType comparer, object value)
        {
            return Add(logic, invoker, comparer, value);
        }

        IQueryParameter IQuery.AddOrUpdate(LogicType logic, IInvoker invoker, CompareType comparer, object value)
        {
            return AddOrUpdate(logic, invoker, comparer, value);
        }

        public InvokerComparer AddOrder(IInvoker invoker, ListSortDirection sortDirection)
        {
            return Orders.AddOrUpdate(invoker, sortDirection);
        }

        public IQueryParameter AddTreeParameter()
        {
            var parameter = new QueryParameter<T>()
            {
                Invoker = new TreeInvoker<IGroup>(),
                Comparer = CompareType.Equal,
                Value = true
            };
            Parameters.Add(parameter);
            return parameter;
        }

        public bool Remove(string parameter)
        {
            return Remove(Parameters[parameter]);
        }

        public bool Remove(IQueryParameter parameter)
        {
            return Remove((QueryParameter<T>)parameter);
        }

        public bool Remove(QueryParameter<T> parameter)
        {
            return Parameters.Remove(parameter);
        }

        public InvokerComparerList<T> GetComparer()
        {
            if (Orders.Count > 0)
            {
                return new InvokerComparerList<T>(Orders);
            }
            return null;
        }

        public void Sort(IList<T> list)
        {
            var comparers = GetComparer();
            if (comparers != null)
            {
                ListHelper.QuickSort(list, comparers);
            }
        }

        public string Format(bool ckeckEmpty = true)
        {
            var logic = false;
            var builder = new StringBuilder();
            foreach (var parametr in GetEnabled().Where(p => !p.FormatIgnore))
            {
                if (ckeckEmpty && parametr.FormatEmpty)
                {
                    continue;
                }
                parametr.Format(builder, logic);
                logic = true;
            }
            return builder.ToString();
        }

        public string FormatEnabled()
        {
            var logic = false;
            var builder = new StringBuilder();
            foreach (var parametr in GetEnabled())
            {
                if (parametr.FormatEmpty)
                {
                    continue;
                }
                parametr.Format(builder, logic);
                logic = true;
            }
            return builder.ToString();
        }

        public IEnumerable<QueryParameter<T>> GetEnabled()
        {
            return ((IEnumerable<QueryParameter<T>>)Parameters).Where(p => p.IsEnabled);
        }

        void IQuery.Sort(IList list)
        {
            Sort((IList<T>)list);
        }

        public void ClearValues()
        {
            Parameters.ClearValues();
        }


    }
}

