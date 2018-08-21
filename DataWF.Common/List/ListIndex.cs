using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Common
{
    public class ListIndex<T, K> : IListIndex<T, K>
    {
        public ConcurrentDictionary<DBNullable<K>, List<T>> Dictionary;
        public IInvoker<T, K> Invoker;

        public ListIndex(IInvoker<T, K> accessor)
        {
            Invoker = accessor;
            if (typeof(K) == typeof(string))
            {
                Dictionary = new ConcurrentDictionary<DBNullable<K>, List<T>>((IEqualityComparer<DBNullable<K>>)DBNullableComparer.StringOrdinalIgnoreCase);
            }
            else
            {
                Dictionary = new ConcurrentDictionary<DBNullable<K>, List<T>>();
            }
        }

        public bool CheckParameter(QueryParameter<T> param)
        {
            return !(!(param.Value is IComparable)
                && (param.Comparer.Type == CompareTypes.Greater
                    || param.Comparer.Type == CompareTypes.GreaterOrEqual
                    || param.Comparer.Type == CompareTypes.Less
                    || param.Comparer.Type == CompareTypes.LessOrEqual));
        }

        public void Add(T item)
        {
            var key = Invoker.GetValue(item);
            if (!Dictionary.TryGetValue(key, out var refs))
            {
                Dictionary[key] = refs = new List<T>();
            }
            refs.Add(item);
        }

        public void Remove(T item)
        {
            lock (Dictionary)
            {
                var key = Invoker.GetValue(item);
                if (!Dictionary.TryGetValue(key, out var refs) || !refs.Remove(item))
                {
                    foreach (var entry in Dictionary)
                    {
                        key = entry.Key;
                        refs = entry.Value;
                        if (refs.Remove(item))
                            break;
                    }
                }
                if (refs != null && refs.Count == 0)
                {
                    Dictionary.TryRemove(key, out refs);
                }
            }
        }

        public T SelectOne(K key)
        {
            Dictionary.TryGetValue(key, out var list);
            return list == null ? default(T) : list.FirstOrDefault();
        }

        public T SelectOne(DBNullable<K> key)
        {
            Dictionary.TryGetValue(key, out var list);
            return list == null ? default(T) : list.FirstOrDefault();
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
                        var key = DBNullable<K>.CheckNull(param.Value);
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
                            if (index.TryGetValue(key, out var value))
                            {
                                foreach (var item in value)
                                    yield return item;
                            }
                        }
                    }
                    break;
                case CompareTypes.Greater:
                    {
                        var key = DBNullable<K>.CheckNull(param.Value);
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
                        var key = DBNullable<K>.CheckNull(param.Value);
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
                        var key = DBNullable<K>.CheckNull(param.Value);
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
                        var key = DBNullable<K>.CheckNull(param.Value);
                        foreach (var entry in index)
                        {
                            if (((IComparable)entry.Key).CompareTo(param.Value) <= 0)
                            {
                                foreach (var item in entry.Value)
                                    yield return item;
                            }
                        }
                    }
                    break;
                case CompareTypes.Like:
                    {
                        var key = DBNullable<K>.CheckNull(param.Value);
                        var stringkey = key.ToString().Trim(new char[] { '%' });
                        foreach (var entry in index)
                        {
                            if (entry.Key.NotNull && ((string)(object)entry.Key.Value).IndexOf(stringkey, StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                foreach (var item in entry.Value)
                                    yield return item;
                            }
                        }
                    }
                    break;
                case CompareTypes.Is:
                    var nullKey = DBNullable<K>.NullKey;
                    if (param.Comparer.Not)
                    {
                        foreach (var entry in index)
                        {
                            if (!entry.Key.Equals(nullKey))
                            {
                                foreach (var item in entry.Value)
                                    yield return item;
                            }
                        }
                    }
                    else
                    {
                        if (index.TryGetValue(nullKey, out var value))
                        {
                            foreach (var item in value)
                                yield return item;
                        }
                    }
                    break;
                case CompareTypes.In:
                    var list = param.Value as IList;
                    if (param.Comparer.Not)
                    {
                        foreach (var entry in index)
                        {
                            if (!list.Contains(entry.Key.Value))
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
                            if (index.TryGetValue(DBNullable<K>.CheckNull(inItem), out var value))
                            {
                                foreach (T item in value)
                                    yield return item;
                            }
                        }
                    }
                    break;
            }
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

        object IListIndex.SelectOne(object value)
        {
            return SelectOne(DBNullable<K>.CheckNull(value));
        }

        T IListIndex<T>.SelectOne(object value)
        {
            return SelectOne(DBNullable<K>.CheckNull(value));
        }

        public void Clear()
        {
            Dictionary.Clear();
        }
    }
}

