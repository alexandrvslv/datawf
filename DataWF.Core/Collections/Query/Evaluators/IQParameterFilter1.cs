using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections.Query.Evaluators
{
    public interface IQParameterFilter<T> : IQParameterFilter
    {
        IFilteredCollection<T> Filter(IEnumerable<T> items, IQParameter compareParameter);

        IFilteredCollection IQParameterFilter.Filter(IEnumerable items, IQParameter compareParameter) => Filter((IEnumerable<T>)items, compareParameter);
    }

}

