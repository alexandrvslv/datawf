using DataWF.Collections.Query;
using DataWF.Collections.Query.Evaluators;
using DataWF.Comparers;
using DataWF.Invokers;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DataWF.Collections.Indexes;

public partial class InvokerIndex<T, K> : IIndex<T, K>
    where K : notnull
{
    protected readonly IDictionary<K, ArrayPointerList<T>> store;
    protected readonly IUniversalComparer<K> keyComparer;
    protected readonly IComparer<T> valueComparer;

    public InvokerIndex(IInvoker<T, K> invoker, bool concurrent = false)
        : this(invoker, IndexFactory.GetNullKey<K>(), DefaultComparer<T>.Default, UniversalComparer<K>.Default, concurrent)
    { }

    public InvokerIndex(IInvoker<T, K> invoker, K nullKey, IComparer<T> valueComparer, IUniversalComparer<K> keyComparer, bool concurrent = false)
    {
        NullKey = nullKey;
        Invoker = invoker;
        this.keyComparer = keyComparer;
        this.valueComparer = valueComparer;
        store = concurrent
            ? (IDictionary<K, ArrayPointerList<T>>)new ConcurrentDictionary<K, ArrayPointerList<T>>(keyComparer)
            : new Dictionary<K, ArrayPointerList<T>>(keyComparer);
    }

    public readonly K NullKey;

    public IEnumerable<T> Source { get; set; }

    public IInvoker<T, K> Invoker { get; }

    IInvoker IInvokerDependant.Invoker => Invoker;

    public IComparer<T> ValueComparer => valueComparer;

    public bool CheckParameter(object value, CompareOperator comparer) => !(!(value is IComparable)
            && (comparer.Type == CompareOperatorType.Greater
                || comparer.Type == CompareOperatorType.GreaterOrEqual
                || comparer.Type == CompareOperatorType.Less
                || comparer.Type == CompareOperatorType.LessOrEqual));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual K GetKey(T item) => Invoker.GetValue(item);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual int IndexOf(ArrayPointerList<T> list, T item) => list.IndexOf(item);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void Add(ArrayPointerList<T> list, T item) => list.Add(item);

    public void Add(T item) => Add(item, GetKey(item));

    public virtual void Add(T item, K key)
    {
        CheckNull(ref key);
        lock (store)
        {
            if (!store.TryGetValue(key, out var refs))
            {
                store[key] = new ArrayPointerList<T>(item);
            }
            else
            {
                Add(refs, item);
            }
        }
    }

    public void Remove(T item) => Remove(item, GetKey(item));

    public void Remove(T item, K key)
    {
        CheckNull(ref key);
        lock (store)
        {
            int index = -1;
            if (!store.TryGetValue(key, out var list)
                || (index = IndexOf(list, item)) < 0)
            {
                RemoveScan(item, ref key, ref list, ref index);
            }
            if (list != null)
            {
                if (list.Count <= 1)
                {
                    store.Remove(key);
                }
                else if (index >= 0)
                {
                    list.RemoveAt(index);
                }
            }
        }
    }

    protected void RemoveScan(T item, ref K key, ref ArrayPointerList<T>? refs, ref int index)
    {
        foreach (var entry in store)
        {
            var list = entry.Value;
            index = IndexOf(list, item);
            if (index >= 0)
            {
                key = entry.Key;
                refs = entry.Value;
                break;
            }
        }
    }

    public void Refresh(IEnumerable items) => Refresh(items.TypeOf<T>());

    public void Refresh(IEnumerable<T> items)
    {
        Source = items;
        Clear();
        foreach (var item in items)
        {
            Add(item);
        }
    }

    public void RefreshItem(T item, PropertyChangedEventArgs arg)
    {
        if (arg is PropertyChangedDetailEventArgs<K> targ)
        {
            Remove(item, targ.OldValue);
            Add(item, targ.NewValue);
        }
        else if (arg is PropertyChangedDetailEventArgs args)
        {
            Remove(item, (K)args.GetOldValue()!);
            Add(item, (K)args.GetNewValue()!);
        }
        else
            RefreshItem(item);
    }

    public void RefreshItem(object item) => RefreshItem((T)item);

    public void RefreshItem(T item)
    {
        var key = GetKey(item);
        CheckNull(ref key);
        var index = -1;
        if (!store.TryGetValue(key, out var list)
                || (index = list.BinarySearch(item, valueComparer)) < 0
                || valueComparer.Compare(list[index], item) != 0)
        {
            Remove(item, key);
            Add(item, key);
        }
    }

    public void RefreshSort(object item) => RefreshSort((T)item);

    public virtual void RefreshSort(T item)
    {
        var key = GetKey(item);
        CheckNull(ref key);
        if (store.TryGetValue(key, out var list))
        {
            var index = list.BinarySearch(item, valueComparer);
            if (index < 0 || valueComparer.Compare(list[index], item) != 0)
            {
                list.Sort(valueComparer);
            }
        }
    }

    public ReadOnlySpan<T> SelectSpan(K key)
    {
        CheckNull(ref key);
        if (store.TryGetValue(key, out var list))
        {
            return list.AsReadOnlySpan();
        }
        return ReadOnlySpan<T>.Empty;
    }

    public T? SelectOne(object value)
    {
        var key = CheckNull(value);
        return store.TryGetValue(key, out var list) ? list.FirstOrDefault() : default(T);
    }

    public T? SelectOne(K key)
    {
        CheckNull(ref key);
        return store.TryGetValue(key, out var list) ? list.FirstOrDefault() : default(T);
    }

    public PredicateScanner Select(Predicate<K> predicate) =>
        new PredicateScanner(this, predicate);

    public CompareTypedScanner Select(ICompareEvaluator<K> compare, K key)
    {
        CheckNull(ref key);
        return new CompareTypedScanner(this, compare, key);
    }

    public CompareObjectScanner Select(ICompareEvaluator<K> compare, object key) =>
        new CompareObjectScanner(this, compare, key);

    public IFilteredCollection<T> Select(CompareOperator comparer, object value) =>
        comparer.GetEvaluator<K>().Eval<T>(this, value);

    public IFilteredCollection<T> Select(CompareOperator comparer, K key) =>
       comparer.GetEvaluator<K>().Eval<T>(this, key);

    public IFilteredCollection<T> Select(IEnumerable keys) =>
        keys is IEnumerable<K> typedKeys
         ? new TypedKeysScanner(this, typedKeys)
         : new ObjectKeysScanner(this, keys);

    public TypedKeysScanner Select(IEnumerable<K> keys) =>
            new TypedKeysScanner(this, keys);

    public KeyScanner Select(K key)
    {
        CheckNull(ref key);
        return new KeyScanner(this, key);
    }

    protected T? SelectOneInternal(K key)
    {
        if (store.TryGetValue(key, out var list))
        {
            return list.FirstOrDefault();
        }
        return default(T);
    }

    object? IIndex.SelectOne(object value) => SelectOneInternal(CheckNull(value));

    T? IIndexGetter<T>.SelectOne(object value) => SelectOneInternal(CheckNull(value));

    public ICollection<K> GetKeys => store.Keys;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CheckNull(ref K key)
    {
        if (key == null)
            key = NullKey;
    }

    public K CheckNull(object key)
    {
        if (key == null)
        {
            return NullKey;
        }
        if (key is K typed)
        {
            return typed;
        }
        return (K)Helper.Parse(key, typeof(K));
    }

    public void Clear() => store.Clear();

    IFilteredCollection<T> IIndexGetter<T, K>.Select(Predicate<K> predicate) => Select(predicate);

    IFilteredCollection<T> IIndexGetter<T, K>.Select(ICompareEvaluator<K> compare, K key) => Select(compare, key);

    IFilteredCollection<T> IIndexGetter<T, K>.Select(ICompareEvaluator<K> compare, object key) => Select(compare, key);

    IFilteredCollection<T> IIndexGetter<T, K>.Select(K key) => Select(key);

    IFilteredCollection<T> IIndexGetter<T, K>.Select(IEnumerable<K> keys) => Select(keys);

    IFilteredCollection<T> IIndexGetter<T, K>.Select(CompareOperator compare, K key) => Select(compare, key);

    IFilteredCollection<T> IIndexGetter<T>.Select(IEvaluator compare, object value) =>
        value is K typedValue ? Select((ICompareEvaluator<K>)compare, typedValue) : Select((ICompareEvaluator<K>)compare, value);

    IFilteredCollection<T> IIndexGetter<T>.Select(IEnumerable keys) => Select(keys);

    IFilteredCollection<T> IIndexGetter<T>.Select(CompareOperator compare, object value) =>
        value is K typedValue ? Select(compare, typedValue) : Select(compare, value);

    IFilteredCollection IIndex.Select(CompareOperator compare, object value) =>
        value is K typedValue ? Select(compare, typedValue) : Select(compare, value);

    IFilteredCollection IIndex.Select(IEvaluator compare, object value) =>
        value is K typedValue ? Select((ICompareEvaluator<K>)compare, typedValue) : Select((ICompareEvaluator<K>)compare, value);

    IFilteredCollection IIndex.Select(IEnumerable keys) => Select(keys);

    public IQIndex<K> CreateQIndex() => new QIndex<K>(this);

    ICollection<K> IIndexGetter<T, K>.GetKeys()
    {
        return store.Keys;
    }
}

