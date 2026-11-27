using System.Collections;

namespace DataWF.Collections.Query.Evaluators
{
    public interface IQParameterFilter
    {
        IFilteredCollection Filter(IEnumerable items, IQParameter parameter);
    }

}

