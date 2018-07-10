using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public interface ISelectable : ISortable, INotifyListPropertyChanged
    {
        IEnumerable Select(Query checkers);

        IEnumerable Select(QueryParameter parameter);

        IEnumerable Select(string property, CompareType comparer, object value);
    }

    public interface ISelectable<T> : ISortable<T>, INotifyListPropertyChanged
    {
        IEnumerable<T> Select(Query checkers);

        IEnumerable<T> Select(QueryParameter parameter);

        IEnumerable<T> Select(string property, CompareType comparer, object value);
    }
}
