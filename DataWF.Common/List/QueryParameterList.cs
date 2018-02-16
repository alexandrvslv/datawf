using System;
using System.Collections.Generic;

namespace DataWF.Common
{
    public class QueryParameterList : QueryItemList<QueryParameter>
    {
        public QueryParameterList()
        {
        }

        public QueryParameterList(IEnumerable<QueryParameter> items)
        {
            AddRange(items);
        }

        public QueryParameter Add(Type type, LogicType logic, string property, CompareType comparer, object value)
        {
            var param = new QueryParameter
            {
                Type = type,
                Logic = logic,
                Property = property,
                Comparer = comparer,
                Value = value
            };
            Add(param);
            return param;
        }
    }
}

