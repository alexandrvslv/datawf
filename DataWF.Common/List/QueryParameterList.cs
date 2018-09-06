using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DataWF.Common
{
    public class QueryParameterList<T> : SelectableList<QueryParameter<T>>
    {
        static readonly Invoker<QueryParameter<T>, string> propertyInvoker = new Invoker<QueryParameter<T>, string>(nameof(QueryParameter<T>.Property), (item) => item.Property);
        private bool clearing;

        public QueryParameterList()
        {
            Indexes.Add(propertyInvoker);
        }

        public QueryParameterList(IEnumerable<QueryParameter<T>> items) : this()
        {
            AddRange(items);
        }

        public QueryParameter<T> this[string property]
        {
            get { return SelectOne(propertyInvoker.Name, property); }
        }

        public void Remove(string property)
        {
            var param = this[property];
            if (param != null)
            {
                Remove(param);
            }
        }

        public QueryParameter<T> Add(LogicType logic, IInvoker invoker, CompareType comparer, object value)
        {
            var parameter = new QueryParameter<T>
            {
                Logic = logic,
                Invoker = invoker,
                Comparer = comparer,
                Value = value
            };
            if (parameter.Invoker?.DataType == typeof(string))
            {
                parameter.Comparer = CompareType.Like;
            }
            Add(parameter);
            return parameter;
        }

        public override void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (clearing)
            {
                return;
            }
            base.OnItemPropertyChanged(sender, e);
        }

        public void ClearValues()
        {
            clearing = true;
            foreach (var item in this)
            {
                item.Value = null;
            }
            clearing = false;
            OnListChanged(NotifyCollectionChangedAction.Reset);
        }
    }
}

