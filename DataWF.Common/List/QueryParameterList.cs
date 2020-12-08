using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DataWF.Common
{
    public class QueryParameterList<T> : NamedList<IQueryParameter<T>>, ICollection<IQueryParameter>
    {
        public QueryParameterList()
        {
        }

        public QueryParameterList(Query<T> query)
        {
            Query = query;
        }

        public QueryParameterList(Query<T> query, IEnumerable<IQueryParameter<T>> items) : this(query)
        {
            AddRange(items);
        }

        public Query<T> Query { get; set; }

        public IQueryParameter<T> Add(IInvoker invoker, CompareType compare, object value)
        {
            var parameter = ((IInvokerExtension)invoker).CreateParameter<T>(compare, value);
            Add(parameter);
            return parameter;
        }

        public IQueryParameter<T> Add(string property, object value)
        {
            var invoker = EmitInvoker.Initialize<T>(property);
            var parameter = ((IInvokerExtension)invoker).CreateParameter<T>(CompareType.Equal, value);
            Add(parameter);
            return parameter;
        }

        public IQueryParameter<T> Add(LogicType logic, IInvoker invoker, CompareType comparer, object value, QueryGroup group = QueryGroup.None)
        {
            var parameter = ((IInvokerExtension)invoker).CreateParameter<T>(logic, comparer, value, group);
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
                item.Value = null;
            }
            Query.Suspending = false;
        }

        void ICollection<IQueryParameter>.Add(IQueryParameter item)
        {
            Add((IQueryParameter<T>)item);
        }

        bool ICollection<IQueryParameter>.Contains(IQueryParameter item)
        {
            return Contains((IQueryParameter<T>)item);
        }

        void ICollection<IQueryParameter>.CopyTo(IQueryParameter[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        bool ICollection<IQueryParameter>.Remove(IQueryParameter item)
        {
            return Remove((IQueryParameter<T>)item);
        }

        IEnumerator<IQueryParameter> IEnumerable<IQueryParameter>.GetEnumerator()
        {
            return GetEnumerator();
        }


    }
}

