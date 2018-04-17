namespace DataWF.Common
{
    public class QueryItemList<T> : SelectableList<T> where T : QueryItem, new()
    {
        static readonly Invoker<T, string> proeprtyInvoker = new Invoker<T, string>(nameof(QueryItem.Property), (item) => item.Property);
        private QueryParameter cacheParam;

        public QueryItemList()
        {
            Indexes.Add(proeprtyInvoker);
            cacheParam = new QueryParameter
            {
                Type = typeof(T),
                Property = nameof(QueryItem.Property),
                Comparer = CompareType.Equal
            };
        }

        public T this[string property]
        {
            get
            {
                cacheParam.Value = property;
                foreach (T item in Select(cacheParam))
                    return item;
                return null;
            }
        }

        public void Remove(string property)
        {
            var param = this[property];
            if (param != null)
                Remove(param);
        }
    }
}

