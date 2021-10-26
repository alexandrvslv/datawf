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
        private ITreeComparer treeComparer;

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

        public IEnumerable<QueryParameter<T>> GetGlobal()
        {
            return ((IEnumerable<QueryParameter<T>>)Parameters).Where(p => p.IsEnabled && p.IsGlobal);
        }

        public QueryParameterList<T> Parameters
        {
            get => parameters ?? (Parameters = new QueryParameterList<T>(this));
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
            get => orders ?? (Orders = new QueryOrdersList<T>(this));
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

        ICollection<IQueryParameter> IQuery.Parameters => Parameters;

        ICollection<IComparer> IQuery.Orders => Orders;

        public ITreeComparer TreeComparer
        {
            get => treeComparer;
            set
            {
                if (treeComparer != value)
                {
                    treeComparer = value;
                    OnOrdersChanged(treeComparer, EventArgs.Empty);
                }
            }
        }

        public bool IsEnabledFormatting => ((IEnumerable<QueryParameter<T>>)Parameters).Any(p => !p.FormatIgnore && p.IsEnabled);

        public bool IsEnabled => ((IEnumerable<QueryParameter<T>>)Parameters).Any(p => p.IsEnabled);

        public event EventHandler OrdersChanged;

        public event EventHandler ParametersChanged;

        internal void OnOrdersChanged(object sender, EventArgs e)
        {
            OrdersChanged?.Invoke(sender, e);
        }

        internal void OnParametersChanged(object sender, EventArgs e)
        {
            ParametersChanged?.Invoke(sender, e);
        }

        public void Clear()
        {
            Parameters.Clear();
        }

        public QueryParameter<T> Add(IInvoker invoker, CompareType compare, object value)
        {
            return Parameters.Add(invoker, compare, value);
        }

        public QueryParameter<T> Add(string property, object value)
        {
            return Parameters.Add(property, value);
        }

        public QueryParameter<T> Add(LogicType logic, IInvoker invoker, CompareType comparer, object value, QueryGroup group = QueryGroup.None)
        {
            return Parameters.Add(logic, invoker, comparer, value, group);
        }

        public QueryParameter<T> AddOrUpdate(IInvoker invoker, object value)
        {
            return AddOrUpdate(invoker, CompareType.Equal, value);
        }

        public QueryParameter<T> AddOrUpdate(IInvoker invoker, CompareType comparer, object value)
        {
            var parameter = GetParameter(invoker);
            return AddOrUpdate(parameter?.Logic ?? LogicType.And, invoker, parameter?.Comparer ?? comparer, value);
        }

        public QueryParameter<T> AddOrUpdate(LogicType logic, IInvoker invoker, CompareType comparer, object value, QueryGroup group = QueryGroup.None)
        {
            var parameter = GetParameter(invoker);
            if (parameter == null)
            {
                parameter = Parameters.Add(logic, invoker, comparer, value, group);
            }
            parameter.Logic = logic;
            parameter.Comparer = comparer;
            parameter.Value = value;
            parameter.Group = group;
            return parameter;
        }
        private QueryParameter<T> GetParameter(IInvoker invoker)
        {
            return Parameters[invoker.Name];
        }

        IQueryParameter IQuery.GetParameter(string name)
        {
            return GetParameter(name);
        }

        private QueryParameter<T> GetParameter(string name)
        {
            return Parameters[name];
        }

        IQueryParameter IQuery.Add(string property, object value)
        {
            return Add(property, value);
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

        public IComparer<T> GetComparer()
        {
            if (Orders.Count > 0)
            {
                var comparer = new InvokerComparerList<T>(Orders);
                if (TreeComparer != null)
                {
                    TreeComparer.Comparer = comparer;
                    return (IComparer<T>)TreeComparer;
                }
                return comparer;
            }
            else if (TreeComparer != null)
            {
                TreeComparer.Comparer = null;
                return (IComparer<T>)TreeComparer;
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

        public string Format(bool ckeckEmpty = true, bool formatOrder = false)
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
            if (formatOrder)
            {
                builder.Append(Orders.Format());
            }
            return builder.ToString();
        }

        public string FormatEnabled()
        {
            var logic = false;
            var builder = new StringBuilder();
            foreach (var parametr in GetEnabled())
            {
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

        public bool IsEnabledParameter(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return true;
            var item = Parameters[propertyName];
            return item?.IsEnabled ?? false;
        }

        public bool IsGlobalParameter(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return false;
            var item = Parameters[propertyName];
            return (item?.IsEnabled ?? false) && item.IsGlobal;
        }
    }
}

