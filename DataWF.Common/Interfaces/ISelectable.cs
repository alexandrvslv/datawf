using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public interface ISelectable : ISortable, INotifyListPropertyChanged
    {
        IEnumerable Select(IQuery checkers);

        IEnumerable Select(IQueryParameter parameter);

        IEnumerable Select(string property, CompareType comparer, object value);

        void AddRange(IEnumerable items);
        void RemoveRange(IEnumerable items);
    }

    public interface ISelectable<T> : ISortable<T>, INotifyListPropertyChanged
    {
        ListIndexes<T> Indexes { get; }

        IEnumerable<T> Select(Query<T> checkers);

        IEnumerable<T> Select(IQueryParameter<T> parameter);

        IEnumerable<T> Select(string property, CompareType comparer, object value);

        void AddRange(IEnumerable<T> items);
        void RemoveRange(IEnumerable<T> items);
    }
}
