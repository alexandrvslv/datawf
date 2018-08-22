using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace DataWF.Common
{
    public class Query<T> : IQuery
    {
        private QueryParameterList<T> parameters;
        private InvokerComparerList<T> orders;

        public Query()
        { }

        public Query(IEnumerable<QueryParameter<T>> parameters)
        {
            Parameters.AddRange(parameters);
        }

        public QueryParameterList<T> Parameters
        {
            get { return parameters ?? (parameters = new QueryParameterList<T>()); }
            set { parameters = value; }
        }

        public InvokerComparerList<T> Orders
        {
            get { return orders ?? (orders = new InvokerComparerList<T>()); }
            set { orders = value; }
        }

        IEnumerable<IQueryParameter> IQuery.Parameters
        {
            get { return Parameters; }
        }

        public void Clear()
        {
            Parameters.Clear();
        }

        public QueryParameter<T> Add(LogicType logic, string property, CompareType comparer, object value)
        {
            return Parameters.Add(logic, property, comparer, value);
        }

        public QueryParameter<T> AddOrUpdate(LogicType logic, string property, CompareType comparer, object value)
        {
            var parameter = Parameters[property];
            if (parameter == null)
            {
                parameter = Parameters.Add(logic, property, comparer, value);
            }
            else
            {
                parameter.Logic = logic;
                parameter.Comparer = comparer;
                parameter.Value = value;
            }
            return parameter;
        }

        IQueryParameter IQuery.Add(LogicType logic, string property, CompareType comparer, object value)
        {
            return Parameters.Add(logic, property, comparer, value);
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

        public void Sort(IList<T> list)
        {
            if (Orders.Count > 0)
            {
                ListHelper.QuickSort(list, Orders);
            }
        }

        public string Format()
        {
            var logic = false;
            var builder = new StringBuilder();
            foreach (var parametr in Parameters)
            {
                if (!parametr.IsEmpty)
                {
                    parametr.Format(builder, logic);
                    logic = true;
                }
            }
            return builder.ToString();
        }

        public void Sort(IList list)
        {
            Sort((IList<T>)list);
        }
    }
}

