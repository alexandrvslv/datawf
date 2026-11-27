using DataWF.Invokers;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections;

public interface INamedList : IList
{
    object? Get(string name);
    void Set(object value);
    void Set(object value, int index);
}

public interface INamedListGetter<out T> : INamedList, IReadOnlyList<T>
{
    new T? Get(string name);
    object? INamedList.Get(string name) => Get(name)!;
}

public interface INamedListSetter<in T> : INamedList
{
    void Set(T value);
    void Set(T value, int index);

    void INamedList.Set(object value) => Set((T)value);
    void INamedList.Set(object value, int index) => Set((T)value, index);
}

public interface INamedList<T> : INamedListGetter<T>, INamedListSetter<T>, IList<T>
{ }

