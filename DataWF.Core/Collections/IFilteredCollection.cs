using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections;

public interface IFilteredCollection : IEnumerable
{
    int Count { get; }
    bool Contains(object item);
}

public interface IFilteredCollection<out T> : IFilteredCollection, IReadOnlyCollection<T>
{
}