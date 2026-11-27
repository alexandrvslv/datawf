using DataWF.Invokers;

namespace DataWF.Collections.Indexes;

public interface IIndexList
{
    IIndex AddIndex(IQueryInvoker invoker);
    void AddToIndex(object item);
    void Clear();
    IIndex? GetIndex(string property);
    IIndex? GetIndex(IInvoker property);
    void RemoveFromIndex(object item);
}

public interface IIndexedListGetter<out T> : IIndexList
{
    new IIndexGetter<T> AddIndex(IQueryInvoker invoker);
    new IIndexGetter<T>? GetIndex(string property);
    new IIndexGetter<T>? GetIndex(IInvoker property);
}

public interface IIndexedListSetter<in T> : IIndexList
{
    void AddToIndex(T item);
    void RemoveFromIndex(T item);
}

public interface IIndexList<T> : IIndexedListGetter<T>, IIndexedListSetter<T>
{ }
