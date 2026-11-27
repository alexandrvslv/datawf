using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DataWF.Common.Collections
{
    public class QueryParameterList<T> : NamedList<IQParameter<T>>, ICollection<IQParameter>
    {
        public QueryParameterList()
        {
        }

        public QueryParameterList(Query<T> query)
        {
            Query = query;
        }

        public QueryParameterList(Query<T> query, IEnumerable<IQParameter<T>> items) : this(query)
        {
            AddRange(items);
        }

        public Query<T> Query { get; set; }

        public IQParameter<T> Add(IInvoker invoker, CompareOperator compare, object value)
        {
            var parameter = invoker.CreateParameter<T>(compare, value);
            Add(parameter);
            return parameter;
        }

        public IQParameter<T> Add(string property, object value)
        {
            var invoker = EmitInvoker.Initialize<T>(property);
            var parameter = invoker.CreateParameter<T>(CompareOperator.Equal, value);
            Add(parameter);
            return parameter;
        }

        public IQParameter<T> Add(LogicOperator logic, IInvoker invoker, CompareOperator comparer, object value, QueryGroup group = QueryGroup.None)
        {
            var parameter = invoker.CreateParameter<T>(logic, comparer, value, group);
            Add(parameter);
            return parameter;
        }

        public override void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnItemPropertyChanged(sender, e);
            Query.OnParametersChanged(sender, e);
        }

        public override NotifyCollectionChangedEventArgs OnCollectionChanged(NotifyCollectionChangedAction type, object item = null, int index = -1, int oldIndex = -1, object oldItem = null)
        {
            var args = base.OnCollectionChanged(type, item, index, oldIndex, oldItem);
            Query.OnParametersChanged(this, args ?? ListHelper.GenerateArgs(type, item, index, oldIndex, oldItem));
            return args;
        }

        public void ClearValues()
        {
            Query.Suspending = true;
            foreach (var item in this)
            {
                item.RightValue = null;
            }
            Query.Suspending = false;
        }

        void ICollection<IQParameter>.Add(IQParameter item)
        {
            if (!Contains(item))
                Add((IQParameter<T>)item);
        }

        bool ICollection<IQParameter>.Contains(IQParameter item)
        {
            return Contains((IQParameter<T>)item);
        }

        void ICollection<IQParameter>.CopyTo(IQParameter[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        bool ICollection<IQParameter>.Remove(IQParameter item)
        {
            return Remove((IQParameter<T>)item);
        }

        IEnumerator<IQParameter> IEnumerable<IQParameter>.GetEnumerator()
        {
            return GetEnumerator();
        }


    }
}

