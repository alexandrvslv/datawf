using DataWF.Collections.Query;
using DataWF.Collections.Query.Evaluators;
using DataWF.Invokers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Collections.Indexes;

public interface IIndex: IInvokerDependant
{
    IEnumerable Source { get; }

    void Add(object item);
    void Add(object item, object key);
    void Remove(object item);
    void Remove(object item, object key);
    void Clear();

    object? SelectOne(object value);
    IFilteredCollection Select(IEvaluator compare, object value);
    IFilteredCollection Select(CompareOperator compare, object value);
    IFilteredCollection Select(IEnumerable keys);

    void RefreshItem(object item);
    void RefreshSort(object item);
    void Refresh(IEnumerable source);
    IQIndex CreateQIndex(string? tableAlias = null);
}

public interface IIndexGetter<out T> : IIndex
{
    new IEnumerable<T> Source { get; }
    new IFilteredCollection<T> Select(IEvaluator compare, object value);
    IFilteredCollection IIndex.Select(IEvaluator compare, object value) => Select(compare, value);
    new IFilteredCollection<T> Select(CompareOperator comparer, object value);
    IFilteredCollection IIndex.Select(CompareOperator compare, object value) => Select(compare, value);
    new IFilteredCollection<T> Select(IEnumerable keys);
    IFilteredCollection IIndex.Select(IEnumerable keys) => Select(keys);
    new T? SelectOne(object value);
    object? IIndex.SelectOne(object value) => SelectOne(value);

    IEnumerable IIndex.Source => Source;
}

public interface IIndexSetter<in T> : IIndex
{
    void Add(T item);
    void Remove(T item);
    void RefreshItem(T item);
    void RefreshItem(T item, PropertyChangedEventArgs details);
    void Refresh(IEnumerable<T> source);
    void RefreshSort(T item);

    void IIndex.Add(object item) => Add((T)item);
    void IIndex.Remove(object item) => Remove((T)item);
}

public interface IIndex<T> : IIndexGetter<T>, IIndexSetter<T>
{
}

public interface IIndexGetter<out T, K> : IIndexGetter<T>
{
    IFilteredCollection<T> Select(Predicate<K> comparer);
    IFilteredCollection<T> Select(ICompareEvaluator<K> compare, K key);
    new IFilteredCollection<T> Select(ICompareEvaluator<K> compare, object key);
    IFilteredCollection<T> Select(K key);
    IFilteredCollection<T> Select(IEnumerable<K> keys);
    IFilteredCollection<T> Select(CompareOperator compare, K key);
    new IFilteredCollection<T> Select(CompareOperator compare, object key);
    T? SelectOne(K key);
    ICollection<K> GetKeys();
}

public interface IIndexSetter<in T, in K> : IIndexSetter<T>
{
    void Add(T item, K key);
    void Remove(T item, K key);

    void IIndex.Add(object item, object key) => Add((T)item, (K)key);
    void IIndex.Remove(object item, object key) => Remove((T)item, (K)key);
}

public interface IIndex<T, K> : IIndex<T>, IIndexGetter<T, K>, IIndexSetter<T, K>
{
    new IQIndex<K> CreateQIndex(string? tableAlias = null) => new QIndex<K>(this);
    IQIndex IIndex.CreateQIndex(string? tableAlias) => CreateQIndex(tableAlias);
}


