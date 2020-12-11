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
        IEnumerable Select(object value, CompareType compare);
        object SelectOne(object value);

    }

    public interface IPullIndex<T, K> : IPullIndex where T : class, IPullHandler
    {
        void Add(T item);
        void Add(T item, K key);
        void Remove(T item);
        void Remove(T item, K key);
        void Refresh(IEnumerable<T> items);
        void RefreshItem(T item);
        void RefreshSort(T item);
        IEnumerable<F> Search<F>(Predicate<K> comparer) where F : T;
        IEnumerable<F> Select<F>(K key) where F : T;
        IEnumerable<F> Select<F>(K key, CompareType compare) where F : T;
        IEnumerable<F> Select<F>(object value, CompareType compare) where F : T;
        F SelectOne<F>(K key) where F : T;
        F SelectOne<F>(object value) where F : T;
    }
}