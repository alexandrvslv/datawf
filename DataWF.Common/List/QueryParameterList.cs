using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DataWF.Common
{
    public class QueryParameterList<T> : NamedList<QueryParameter<T>>
    {
        private bool suspend;

        public QueryParameterList()
        {
        }

        public QueryParameterList(IEnumerable<QueryParameter<T>> items) : this()
        {
            AddRange(items);
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
                        OnListChanged(NotifyCollectionChangedAction.Reset);
                    }
                }
            }
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
            if (Suspending)
            {
                return;
            }
            base.OnItemPropertyChanged(sender, e);
        }

        public override void OnListChanged(NotifyCollectionChangedEventArgs args)
        {
            if (Suspending)
            {
                return;
            }
            base.OnListChanged(args);
        }

        public void ClearValues()
        {
            Suspending = true;
            foreach (var item in this)
            {
                item.Value = null;
            }
            Suspending = false;
        }
    }
}

