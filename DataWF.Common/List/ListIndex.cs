using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DataWF.Common
{
    public class ListIndex<T, K> : IListIndex<T, K>
    {
        public static IEqualityComparer<K> GetComparer(IValuedInvoker<K> invoker)
        {
            var comparer = (IEqualityComparer<K>)EqualityComparer<K>.Default;
            if (invoker.DataType == typeof(string))
                comparer = (IEqualityComparer<K>)StringComparer.OrdinalIgnoreCase;
            if (invoker.DataType == typeof(byte[]))
                comparer = (IEqualityComparer<K>)ByteArrayComparer.Default;
            return comparer;
        }
        protected readonly IDictionary<K, ThreadSafeList<T>> Dictionary;
        protected readonly IEqualityComparer<K> Comparer;
        protected K NullKey;

        public ListIndex(IValuedInvoker<K> invoker, K nullKey, bool concurrent = false)
            : this(invoker, nullKey, GetComparer(invoker), concurrent)
        { }

        public ListIndex(IValuedInvoker<K> invoker, K nullKey, IEqualityComparer<K> comparer = null, bool concurrent = false)
        {
            NullKey = nullKey;
            Invoker = invoker;
            Comparer = comparer ?? EqualityComparer<K>.Default;
            Dictionary = concurrent
                ? (IDictionary<K, ThreadSafeList<T>>)new ConcurrentDictionary<K, ThreadSafeList<T>>(Comparer)
                : new Dictionary<K, ThreadSafeList<T>>(Comparer);
        }

        public IValuedInvoker<K> Invoker { get; }

        IInvoker IListIndex.Invoker => Invoker;

        public bool CheckParameter(object value, CompareType comparer)
        {
            return !(!(value is IComparable)
                && (comparer.Type == CompareTypes.Greater
                    || comparer.Type == CompareTypes.GreaterOrEqual
                    || comparer.Type == CompareTypes.Less
                    || comparer.Type == CompareTypes.LessOrEqual));
        }

        public void Add(T item)
        {
            Add(item, Invoker.GetValue(item));
        }

        public void Add(T item, K key)
        {
            CheckNull(ref key);
            if (!Dictionary.TryGetValue(key, out var refs))
            {
                Dictionary[key] = refs = new ThreadSafeList<T>(item);
            }
            else
            {
                refs.Add(item);
            }
        }

        public void Remove(T item)
        {
            Remove(item, Invoker.GetValue(item));
        }

        public void Remove(T item, K key)
        {
            CheckNull(ref key);
            if (!Dictionary.TryGetValue(key, out var refs) || !refs.Contains(item))
            {
                RemoveScan(item, ref key, ref refs);
            }
            if (refs != null)
            {
                if (refs.Count == 1)
                {
                    Dictionary.Remove(key);
                }
                else
                {
                    refs.Remove(item);
                }
            }
        }

        private void RemoveScan(T item, ref K key, ref ThreadSafeList<T> refs)
        {
            foreach (var entry in Dictionary)
            {
                if (entry.Value.Contains(item))
                {
                    key = entry.Key;
                    refs = entry.Value;
                    break;
                }
            }
        }

        public T SelectOne(K key)
        {
            CheckNull(ref key);
            return SelectOneInternal(key);
        }

        protected T SelectOneInternal(K key)
        {
            if (Dictionary.TryGetValue(key, out var list))
            {
                return list[0];
            }
            return default(T);
        }

        IEnumerable IListIndex.Scan(IQueryParameter param)
        {
            return Scan((IQueryParameter<T>)param);
        }

        public IEnumerable<T> Scan(IQueryParameter<T> param)
        {
            return ScanTyped(param.Comparer, param.TypedValue);
        }

        IEnumerable IListIndex.Scan(CompareType comparer, object value)
        {
            return Scan(comparer, value);
        }

        public IEnumerable<T> Scan(CompareType comparer, object value)
        {
            return ScanTyped(comparer, Helper.ParseParameter<K>(value, comparer));
        }

        public IEnumerable<T> Scan(CompareType comparer, K key)
        {
            var index = Dictionary;
            switch (comparer.Type)
            {
                case CompareTypes.Is:
                    if (comparer.Not)
                    {
                        foreach (var entry in index)
                        {
                            if (!ListHelper.Equal(entry.Key, NullKey))
                            {
                                foreach (var item in entry.Value)
                                    yield return item;
                            }
                        }
                    }
                    else
                    {
                        if (index.TryGetValue(NullKey, out var value))
                        {
                            foreach (var item in value)
                                yield return item;
                        }
                    }
                    break;
                case CompareTypes.Equal:
                    {
                        if (comparer.Not)
                        {
                            foreach (var entry in index)
                            {
                                if (!ListHelper.Equal(entry.Key, key))
                                {
                                    foreach (var item in entry.Value)
                                        yield return item;
                                }
                            }
                        }
                        else
                        {
                            if (index.TryGetValue(key, out var value) && value != null)
                            {
                                foreach (var item in value)
                                    yield return item;
                            }
                        }
                    }
                    break;
                case CompareTypes.Greater:
                    {
                        foreach (var entry in index)
                        {
                            if (ListHelper.Compare<K>(entry.Key, key) > 0)
                            {
                                foreach (var item in entry.Value)
                                    yield return item;
                            }
                        }
                    }
                    break;
                case CompareTypes.GreaterOrEqual:
                    {
                        foreach (var entry in index)
                        {
                            if (ListHelper.Compare<K>(entry.Key, key) >= 0)
                            {
                                foreach (var item in entry.Value)
                                    yield return item;
                            }
                        }
                    }
                    break;
                case CompareTypes.Less:
                    {
                        foreach (var entry in index)
                        {
                            if (ListHelper.Compare<K>(entry.Key, key) < 0)
                            {
                                foreach (var item in entry.Value)
                                    yield return item;
                            }
                        }
                    }
                    break;
                case CompareTypes.LessOrEqual:
                    {
                        foreach (var entry in index)
                        {
                            if (ListHelper.Compare<K>(entry.Key, key) <= 0)
                            {
                                foreach (var item in entry.Value)
                                    yield return item;
                            }
                        }
                    }
                    break;
            }

        }

        public IEnumerable<T> ScanTyped(CompareType comparer, object typedValue)
        {
            var index = Dictionary;
            switch (comparer.Type)
            {
                case CompareTypes.Like:
                    {
                        var key = CheckNull(typedValue);
                        var stringkey = key.ToString().Trim(new char[] { '%' });
                        foreach (var entry in index)
                        {
                            if (entry.Key.ToString().IndexOf(stringkey, StringComparison.OrdinalIgnoreCase) > -1)
                            {
                                foreach (var item in entry.Value)
                                    yield return item;
                            }
                        }
                    }
                    break;
                case CompareTypes.In:
                    var list = typedValue.ToEnumerable();
                    if (comparer.Not)
                    {
                        foreach (var entry in index)
                        {
                            if (!ListHelper.Contains(list, entry.Key))
                            {
                                foreach (var item in entry.Value)
                                    yield return item;
                            }
                        }
                    }
                    else
                    {
                        foreach (var inItem in list)
                        {
                            if (index.TryGetValue(CheckNull(inItem), out var value))
                            {
                                foreach (T item in value)
                                    yield return item;
                            }
                        }
                    }
                    break;
                default:
                    foreach (T item in Scan(comparer, CheckNull(typedValue)))
                        yield return item;
                    break;
            }
        }

        private void CheckNull(ref K key)
        {
            if (Comparer.Equals(key, default(K)))
                key = NullKey;
        }

        private K CheckNull(object key)
        {
            if (key == null)
            {
                return NullKey;
            }
            if (key is K typed)
            {
                return Comparer.Equals(typed, default(K)) ? NullKey : typed;
            }
            return (K)Helper.Parse(key, typeof(K));
        }

        public void Refresh(IList items)
        {
            Refresh((IList<T>)items);
        }

        public void Refresh(IList<T> items)
        {
            Clear();
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public void Refresh(T item, PropertyChangedDetailEventArgs arg)
        {
            if (arg is PropertyChangedDetailEventArgs<K> targ)
            {
                Remove(item, targ.OldValue);
                Add(item, targ.NewValue);
            }
            else
            {
                Remove(item, (K)arg.OldObjectValue);
                Add(item, (K)arg.NewObjectValue);
            }
        }

        public void Refresh(T item)
        {
            var key = Invoker.GetValue(item);
            CheckNull(ref key);
            if (!Dictionary.TryGetValue(key, out var refs) || !refs.Contains(item))
            {
                Remove(item, key);
                Add(item, key);
            }
        }

        public void Add(object item)
        {
            Add((T)item);
        }

        public void Remove(object item)
        {
            Remove((T)item);
        }

        public void Add(object item, object key)
        {
            Add((T)item, (K)key);
        }

        public void Remove(object item, object key)
        {
            Remove((T)item, (K)key);
        }

        object IListIndex.SelectOne(object value)
        {
            return SelectOneInternal(CheckNull(value));
        }

        T IListIndex<T>.SelectOne(object value)
        {
            return SelectOneInternal(CheckNull(value));
        }

        public void Clear()
        {
            Dictionary.Clear();
        }
    }
}

