using System;
using System.Collections;

namespace DataWF.Common
{
    public class QueryParameter : QueryItem
    {
        public static QueryParameter CreateTreeFilter(Type type)
        {
            return new QueryParameter()
            {
                Invoker = new TreeInvoker(),
                Comparer = CompareType.Equal,
                Value = true
            };
        }

        public object Value { get; set; }

        public CompareType Comparer { get; set; } = CompareType.Equal;

        public LogicType Logic { get; set; } = LogicType.And;

        public IComparer Comparision { get; set; }

    }
}

