using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections.Query.Evaluators;

public abstract class ParameterFilter<T, V> : IQParameterFilter<T>
{
    public abstract IFilteredCollection<T> Filter(IEnumerable<T> items, IQParameter parameter);

    public IFilteredCollection<T> Filter(IEnumerable items, IQParameter parameter) => Filter((IEnumerable<T>)items, parameter);
}

