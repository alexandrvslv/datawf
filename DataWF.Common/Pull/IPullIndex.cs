using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public interface IPullIndex
    {
        Pull BasePull { get; }
        void Add(object item);
        void Add(object item, object key);
        void Remove(object item);
        void Remove(object item, object value);
        void Clear();
        void Dispose();
        void Refresh(IEnumerable items);
        void RefreshItem(object item);
        void RefreshSort(object item);
        IPullIndexCollection SelectObjects(object value, CompareType compare);
        object SelectOneObject(object value);

    }

    public interface IPullInIndex<in T, K> : IPullIndex where T : class, IPullHandler
    {
        void Add(T item);
        void Add(T item, K key);
        void Remove(T item);
        void Remove(T item, K key);
        void Refresh(IEnumerable<T> items);
        void RefreshItem(T item);
        void RefreshSort(T item);

    }

    public interface IPullOutIndex<out T, K> : IPullIndex where T : class, IPullHandler
    {
        IPullIndexCollection<T> Search(Predicate<K> comparer);
        IPullIndexCollection<T> Select(K key);
        IPullIndexCollection<T> Select(K key, CompareType compare);
        IPullIndexCollection<T> Select(object value, CompareType compare);
        T SelectOne(K key);
        T SelectOne(object value);
    }

    public interface IPullIndexCollection : IEnumerable
    {
        int Count { get; }
        bool Contains(object item);
    }

    public interface IPullIndexCollection<out T> : IPullIndexCollection, IReadOnlyCollection<T> where T : class, IPullHandler
    {
    }
}