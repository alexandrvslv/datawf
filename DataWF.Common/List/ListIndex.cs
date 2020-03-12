﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DataWF.Common
{
    public class ListIndex<T, K> : IListIndex<T, K>
    {
        protected IDictionary<K, ThreadSafeList<T>> Dictionary;
        protected IEqualityComparer<K> Comparer;
        protected readonly K NullKey;

        public ListIndex(IInvoker<T, K> invoker, K nullKey, IEqualityComparer<K> comparer = null, bool concurrent = false)
        {
            NullKey = nullKey;
            Invoker = invoker;
            Comparer = comparer ?? EqualityComparer<K>.Default;
            Dictionary = concurrent
                ? (IDictionary<K, ThreadSafeList<T>>)new ConcurrentDictionary<K, ThreadSafeList<T>>(Comparer)
                : new Dictionary<K, ThreadSafeList<T>>(Comparer);
        }

        public IInvoker<T, K> Invoker { get; }

        IInvoker IListIndex.Invoker => Invoker;

        public bool CheckParameter(QueryParameter<T> param)
        {
            return !(!(param.TypedValue is IComparable)
                && (param.Comparer.Type == CompareTypes.Greater
                    || param.Comparer.Type == CompareTypes.GreaterOrEqual
                    || param.Comparer.Type == CompareTypes.Less
                    || param.Comparer.Type == CompareTypes.LessOrEqual));
        }

        public void Add(T item)
        {
            var key = Invoker.GetValue(item);
            CheckNull(ref key);
            Add(item, key);
        }

        public void Add(T item, K key)
        {
            if (!Dictionary.TryGetValue(key, out var refs))
            {
                Dictionary[key] = refs = new ThreadSafeList<T>(1);
            }
            refs.Add(item);
        }

        public void Remove(T item)
        {
            var key = Invoker.GetValue(item);
            CheckNull(ref key);
            Remove(item, key);
        }

        public void Remove(T item, K key)
        {
            if (!Dictionary.TryGetValue(key, out var refs) || !refs.Remove(item))
            {
                foreach (var entry in Dictionary)
                {
                    if (entry.Value.Remove(item))
                    {
                        key = entry.Key;
                        refs = entry.Value;
                        break;
                    }
                }
            }
            if (refs != null && refs.Count == 0)
            {
                Dictionary.Remove(key);
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

        public IEnumerable Scan(IQueryParameter param)
        {
            return Scan((QueryParameter<T>)param);
        }

        public IEnumerable<T> Scan(QueryParameter<T> param)
        {
            if (!CheckParameter(param))
            {
                yield break;
            }
            var index = Dictionary;
            switch (param.Comparer.Type)
            {
                case CompareTypes.Equal:
                    {
                        var key = CheckNull(param.TypedValue);
                        if (param.Comparer.Not)
                        {
                            foreach (var entry in index)
                            {
                                if (!entry.Key.Equals(key))
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
                        var key = CheckNull(param.TypedValue);
                        foreach (var entry in index)

                        {
                            if (((IComparable)entry.Key).CompareTo(key) > 0)
                            {
                                foreach (var item in entry.Value)
                                    yield return item;
                            }
                        }
                    }
                    break;
                case CompareTypes.GreaterOrEqual:
                    {
                        var key = CheckNull(param.TypedValue);
                        foreach (var entry in index)
                        {
                            if (((IComparable)entry.Key).CompareTo(key) >= 0)
                            {
                                foreach (var item in entry.Value)
                                    yield return item;
                            }
                        }
                    }
                    break;
                case CompareTypes.Less:
                    {
                        var key = CheckNull(param.TypedValue);
                        foreach (var entry in index)
                        {
                            if (((IComparable)entry.Key).CompareTo(key) < 0)
                            {
                                foreach (var item in entry.Value)
                                    yield return item;
                            }
                        }
                    }
                    break;
                case CompareTypes.LessOrEqual:
                    {
                        var key = CheckNull(param.TypedValue);
                        foreach (var entry in index)
                        {
                            if (((IComparable)entry.Key).CompareTo(key) <= 0)
                            {
                                foreach (var item in entry.Value)
                                    yield return item;
                            }
                        }
                    }
                    break;
                case CompareTypes.Like:
                    {
                        var key = CheckNull(param.TypedValue);
                        var stringkey = key.ToString().Trim(new char[] { '%' });
                        foreach (var entry in index)
                        {
                            if (!entry.Key.Equals(entry) && (entry.Key.ToString()).IndexOf(stringkey, StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                foreach (var item in entry.Value)
                                    yield return item;
                            }
                        }
                    }
                    break;
                case CompareTypes.Is:
                    if (param.Comparer.Not)
                    {
                        foreach (var entry in index)
                        {
                            if (!entry.Key.Equals(NullKey))
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
                case CompareTypes.In:
                    var list = param.Value as IEnumerable;
                    if (param.Comparer.Not)
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

        public void Refresh(T item)
        {
            Remove(item);
            Add(item);
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
            var typeKey = (K)key;
            CheckNull(ref typeKey);
            Add((T)item, typeKey);
        }

        public void Remove(object item, object key)
        {
            var typeKey = (K)key;
            CheckNull(ref typeKey);
            Remove((T)item, typeKey);
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

