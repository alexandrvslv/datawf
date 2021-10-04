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

        public Query(IEnumerable<IQueryParameter<T>> parameters)
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

        public bool IsEnabledFormatting => ((IEnumerable<IQueryParameter<T>>)Parameters).Any(p => !p.FormatIgnore && p.IsEnabled);

        public bool IsEnabled => ((IEnumerable<IQueryParameter<T>>)Parameters).Any(p => p.IsEnabled);

        public bool HasOrderBy => Orders.Count > 0 || TreeComparer != null;

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

        public IEnumerable<IQueryParameter<T>> GetGlobal()
        {
            return ((IEnumerable<IQueryParameter<T>>)Parameters).Where(p => p.IsEnabled && p.IsGlobal);
        }

        public void Clear()
        {
            Parameters.Clear();
        }

        public IQueryParameter<T> Add(IInvoker invoker, CompareType compare, object value)
        {
            return Parameters.Add(invoker, compare, value);
        }

        public IQueryParameter<T> Add(string property, object value)
        {
            return Parameters.Add(property, value);
        }

        public IQueryParameter<T> Add(LogicType logic, IInvoker invoker, CompareType comparer, object value, QueryGroup group = QueryGroup.None)
        {
            return Parameters.Add(logic, invoker, comparer, value, group);
        }

        public IQueryParameter<T> AddOrUpdate(IInvoker invoker, object value)
        {
            return AddOrUpdate(invoker, CompareType.Equal, value);
        }

        public IQueryParameter<T> AddOrUpdate(IInvoker invoker, CompareType comparer, object value)
        {
            var parameter = GetParameter(invoker);
            return AddOrUpdate(parameter?.Logic ?? LogicType.And, invoker, parameter?.Comparer ?? comparer, value);
        }

        public IQueryParameter<T> AddOrUpdate(LogicType logic, IInvoker invoker, CompareType comparer, object value, QueryGroup group = QueryGroup.None)
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
        private IQueryParameter<T> GetParameter(IInvoker invoker)
        {
            return Parameters[invoker.Name];
        }

        IQueryParameter IQuery.GetParameter(string name)
        {
            return GetParameter(name);
        }

        private IQueryParameter<T> GetParameter(string name)
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
            return Remove((IQueryParameter<T>)parameter);
        }

        public bool Remove(IQueryParameter<T> parameter)
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

        public IEnumerable<IQueryParameter<T>> GetEnabled()
        {
            return ((IEnumerable<IQueryParameter<T>>)Parameters).Where(p => p.IsEnabled);
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

        public IEnumerable Select(IEnumerable items, IListIndexes indexes = null)
        {
            return Select((IEnumerable<T>)items, (IListIndexes<T>)indexes);
        }

        public IEnumerable<T> Select(IEnumerable<T> items, IListIndexes<T> indexes = null)
        {
            IEnumerable<T> buffer = items;
            var stack = new Stack<SelectStackEntry>(0);
            bool? flag = null;
            foreach (var parameter in GetEnabled())
            {
                var curParameter = parameter;
                var temp = curParameter.Select(curParameter.Logic == LogicType.And ? buffer : items, indexes);
                if ((curParameter.Group & QueryGroup.Begin) == QueryGroup.Begin)
                {
                    stack.Push(new SelectStackEntry() { Buffer = temp, Parameter = curParameter });
                    continue;
                }
                else if (stack.Count > 0)
                {
                    var entry = stack.Pop();
                    entry.Buffer = curParameter.Logic.Concat(entry.Buffer, temp);
                    if ((curParameter.Group & QueryGroup.End) == QueryGroup.End)
                    {
                        temp = entry.Buffer;
                        curParameter = entry.Parameter;
                    }
                    else
                    {
                        stack.Push(entry);
                        continue;
                    }
                }
                if (flag == null)
                {
                    buffer = temp;
                    flag = true;
                }
                else
                {
                    buffer = curParameter.Logic.Concat(buffer, temp);
                }
            }
            return buffer;
        }

        public bool CheckItem(object item)
        {
            return CheckItem((T)item);
        }

        public bool CheckItem(T item)
        {
            bool? flag = null;
            var stack = new Stack<CheckStackEntry>(0);
            foreach (var parameter in GetEnabled())
            {
                bool rez = parameter.AlwaysTrue ? true : parameter.CheckItem(item);
                var currParameter = parameter;
                if ((currParameter.Group & QueryGroup.Begin) == QueryGroup.Begin)
                {
                    stack.Push(new CheckStackEntry { Flag = rez, Parameter = currParameter });
                    continue;
                }
                else if (stack.Count > 0)
                {
                    var entry = stack.Pop();
                    entry.Flag = currParameter.Logic.Concat(entry.Flag, rez);

                    if ((currParameter.Group & QueryGroup.End) == QueryGroup.End)
                    {
                        rez = entry.Flag;
                        currParameter = entry.Parameter;
                    }
                    else
                    {
                        stack.Push(entry);
                        continue;
                    }
                }
                if (flag == null)
                {
                    flag = rez;
                }
                else
                {
                    flag = currParameter.Logic.Concat(flag.Value, rez);
                }
            }
            return flag ?? true;
        }

        public struct SelectStackEntry
        {
            public IEnumerable<T> Buffer;
            public IQueryParameter<T> Parameter;
        }

        public struct CheckStackEntry
        {
            public bool Flag;
            public IQueryParameter<T> Parameter;
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
                return FormatOrders(builder);
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

        public string FormatOrders(StringBuilder builder = null)
        {
            builder = builder ?? new StringBuilder();
            builder.Append(Orders.Format());
            if (TreeComparer != null)
            {
                if (Orders.Count > 0)
                    builder.Append(',');
                else
                    builder.Append(" order by");
                builder.Append(' ');
                builder.Append(nameof(IGroup));
            }
            return builder.ToString();
        }
    }
}

