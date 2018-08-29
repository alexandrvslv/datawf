using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public interface ISelectable : ISortable, INotifyListPropertyChanged
    {
        IEnumerable Select(IQuery checkers);

        IEnumerable Select(IQueryParameter parameter);

        IEnumerable Select(string property, CompareType comparer, object value);
    }

    public interface ISelectable<T> : ISortable<T>, INotifyListPropertyChanged
    {
        ListIndexes<T> Indexes { get; }

        IEnumerable<T> Select(Query<T> checkers);

        IEnumerable<T> Select(QueryParameter<T> parameter);

        IEnumerable<T> Select(string property, CompareType comparer, object value);
    }
}
