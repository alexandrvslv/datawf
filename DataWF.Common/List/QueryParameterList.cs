using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DataWF.Common
{
    public class QueryParameterList<T> : NamedList<QueryParameter<T>>, ICollection<IQueryParameter>
    {
        public QueryParameterList()
        {
        }

        public QueryParameterList(Query<T> query)
        {
            Query = query;
        }

        public QueryParameterList(Query<T> query, IEnumerable<QueryParameter<T>> items) : this(query)
        {
            AddRange(items);
        }

        public Query<T> Query { get; set; }

        public QueryParameter<T> Add(IInvoker invoker, CompareType compare, object value)
        {
            var parameter = new QueryParameter<T>()
            {
                Invoker = invoker,
                Comparer = compare,
                Value = value
            };
            Add(parameter);
            return parameter;
        }

        public QueryParameter<T> Add(string property, object value)
        {
            var parameter = new QueryParameter<T>(property) { Value = value };
            Add(parameter);
            return parameter;
        }

        public QueryParameter<T> Add(LogicType logic, IInvoker invoker, CompareType comparer, object value, QueryGroup group = QueryGroup.None)
        {
            var parameter = new QueryParameter<T>
            {
                Logic = logic,
                Invoker = invoker,
                Comparer = comparer,
                Value = value,
                Group = group
            };
            //if (parameter.Invoker?.DataType == typeof(string))
            //{
            //    parameter.Comparer = CompareType.Like;
            //}
            Add(parameter);
            return parameter;
        }

        public override void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnItemPropertyChanged(sender, e);
            Query.OnParametersChanged(sender, e);
        }

        public override void OnListChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnListChanged(e);
            Query.OnParametersChanged(this, e);
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
            Add((QueryParameter<T>)item);
        }

        bool ICollection<IQueryParameter>.Contains(IQueryParameter item)
        {
            return Contains((QueryParameter<T>)item);
        }

        void ICollection<IQueryParameter>.CopyTo(IQueryParameter[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        bool ICollection<IQueryParameter>.Remove(IQueryParameter item)
        {
            return Remove((QueryParameter<T>)item);
        }

        IEnumerator<IQueryParameter> IEnumerable<IQueryParameter>.GetEnumerator()
        {
            return GetEnumerator();
        }


    }
}

