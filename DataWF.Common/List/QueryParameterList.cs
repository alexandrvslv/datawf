using System.Collections.Generic;

namespace DataWF.Common
{
    public class QueryParameterList<T> : SelectableList<QueryParameter<T>>
    {
        static readonly Invoker<QueryParameter<T>, string> propertyInvoker = new Invoker<QueryParameter<T>, string>(nameof(QueryParameter<T>.Property), (item) => item.Property);

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

        public QueryParameter<T> Add(LogicType logic, string property, CompareType comparer, object value)
        {
            var parameter = new QueryParameter<T>
            {
                Logic = logic,
                Property = property,
                Comparer = comparer,
                Value = value
            };
            if (parameter.Invoker.DataType == typeof(string))
            {
                parameter.Comparer = CompareType.Like;
            }
            Add(parameter);
            return parameter;
        }
    }
}

