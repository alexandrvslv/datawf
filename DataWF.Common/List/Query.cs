using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataWF.Common
{
    public class Query<T> : IQuery
    {
        private QueryParameterList<T> parameters;
        //private InvokerComparerList<T> orders;

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

        //public InvokerComparerList<T> Orders
        //{
        //    get { return orders ?? (orders = new InvokerComparerList<T>()); }
        //    set { orders = value; }
        //}

        IEnumerable<IQueryParameter> IQuery.Parameters
        {
            get { return Parameters; }
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

        public QueryParameter<T> Add(LogicType logic, string property, CompareType comparer, object value)
        {
            return Parameters.Add(logic, property, comparer, value);
        }

        public QueryParameter<T> AddOrUpdate(string property, object value)
        {
            var parameter = Parameters[property];
            return AddOrUpdate(parameter?.Logic ?? LogicType.And, property, parameter?.Comparer ?? CompareType.Equal, value);
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

        public bool Remove(IQueryParameter parameter)
        {
            return Remove((QueryParameter<T>)parameter);
        }

        public bool Remove(QueryParameter<T> parameter)
        {
            return Parameters.Remove(parameter);
        }

        public void Sort(IList<T> list)
        {
            var comparers = Parameters.Where(p => p.SortDirection != null).Select(p => p.GetComparer());
            if (comparers.Any())
            {
                //if (list is ISortable sortable)
                //{
                //    sortable.ApplySort(Orders)
                //}
                ListHelper.QuickSort(list, new InvokerComparerList<T>(comparers));
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

