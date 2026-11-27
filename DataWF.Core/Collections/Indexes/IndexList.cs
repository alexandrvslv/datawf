using DataWF.Invokers;
using System;
using System.Collections.Generic;

namespace DataWF.Collections.Indexes;

public class IndexList<T> : IIndexList<T>
{
    protected Dictionary<string, IIndex<T>> indexes = new(StringComparer.Ordinal);

    public bool Concurrent { get; set; }
    public int Count => indexes.Count;
    public IReadOnlyList<T> Source { set; get; }

    public IndexList(IReadOnlyList<T> source)
    {
        Source = source;
    }

    public void Add(string name, IIndex<T> index) => indexes[name] = index;

    IIndex IIndexList.AddIndex(IQueryInvoker invoker) => AddIndex(invoker);

    public IIndex<T, V> AddIndex<V>(IInvokerGetter<T, V> invoker)
    {
        if (!indexes.TryGetValue(invoker.Name, out var index))
        {
            index = new InvokerIndex<T, V>(invoker, Concurrent);
            indexes.Add(invoker.Name, index);
            index.Refresh(Source);
        }
        return (IIndex<T, V>)index;
    }

    public IIndex<T> AddIndex(IQueryInvoker invoker)
    {
        if (!indexes.TryGetValue(invoker.Name, out var index))
        {
            index = (IIndex<T>)invoker.CreateIndex(Concurrent);
            indexes.Add(invoker.Name, index);
            index.Refresh(Source);
        }
        return index;
    }

    public IIndex<T>? GetIndex(string property)
        => indexes.TryGetValue(property, out var index) ? index : null;

    public IIndex<T>? GetIndex(IInvoker invoker)
        => indexes.TryGetValue(invoker.Name, out var index) ? index : null;

    public InvokerIndex<T, K>? GetIndex<K>(string property) where K : notnull
        => (InvokerIndex<T, K>?)GetIndex(property);

    public void AddToIndex(T item)
    {
        foreach (var index in indexes.Values)
            index.Add(item);
    }

    public void RemoveFromIndex(T item)
    {
        foreach (var index in indexes.Values)
            index.Remove(item);
    }

    public void Clear()
    {
        foreach (var index in indexes.Values)
            index.Clear();
    }

    public void AddToIndex(object item) => AddToIndex((T)item);

    IIndex? IIndexList.GetIndex(string property) => GetIndex(property);

    public void RemoveFromIndex(object item) => RemoveFromIndex((T)item);

    IIndexGetter<T> IIndexedListGetter<T>.AddIndex(IQueryInvoker invoker) => AddIndex(invoker);

    IIndexGetter<T>? IIndexedListGetter<T>.GetIndex(string property) => GetIndex(property);

    IIndexGetter<T>? IIndexedListGetter<T>.GetIndex(IInvoker invoker) => GetIndex(invoker);

    IIndex? IIndexList.GetIndex(IInvoker invoker) => GetIndex(invoker);
}
