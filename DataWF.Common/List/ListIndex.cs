using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public interface IListIndex
    {
        void Add(object item);
        void Clear();
        void Remove(object item);
        object SelectOne(object value);
        IEnumerable Scan(QueryParameter parameter);
    }

    public class ListIndex<T> : IListIndex
    {
        public Hashtable Hash;
        public IInvoker Invoker;

        public ListIndex(IInvoker accessor)
        {
            Invoker = accessor;
            //var type = typeof(Dictionary<,>);
            //Type dictionary = type.MakeGenericType(accessor.ValueType, accessor.Info.DeclaringType);
            //if (accessor.ValueType == typeof(string))
            //    Dict = (IDictionary)ReflectionAccessor.CreateObject(dictionary, new Type[] { typeof(IEqualityComparer<string>) }, new object[] { StringComparer.OrdinalIgnoreCase }, true);
            //else
            //    Dict = (IDictionary)ReflectionAccessor.CreateObject(dictionary, new Type[] { }, new object[] { }, true);
            if (accessor.DataType == typeof(string))
                Hash = new Hashtable(StringComparer.OrdinalIgnoreCase);
            else
                Hash = new Hashtable();
        }

        public bool CheckParameter(QueryParameter param)
        {
            return !(!(param.Value is IComparable)
                && (param.Comparer.Type == CompareTypes.Greater
                    || param.Comparer.Type == CompareTypes.GreaterOrEqual
                    || param.Comparer.Type == CompareTypes.Less
                    || param.Comparer.Type == CompareTypes.LessOrEqual));
        }

        public void Add(T item)
        {
            if (!Invoker.TargetType.IsInstanceOfType(item))
                return;
            var value = Invoker.Get(item);
            value = value ?? DBNull.Value;

            var refs = Hash[value] as List<T>;
            if (refs == null)
            {
                refs = new List<T>();
                Hash.Add(value, refs);
            }
            refs.Add(item);
        }

        internal void Remove(T item)
        {
            object val = Invoker.Get(item);
            val = val ?? DBNull.Value;

            lock (Hash)
            {
                var refs = Hash[val] as List<T>;
                if (refs == null || !refs.Remove(item))
                {
                    foreach (DictionaryEntry entry in Hash)
                    {
                        val = entry.Key;
                        refs = (List<T>)entry.Value;
                        if (refs.Remove(item))
                            break;
                    }
                }
                //if (refs.Count == 0)
                //    index.Remove(val);
            }
        }

        public T SelectOne(object value)
        {
            if (value == null)
                value = DBNull.Value;

            var list = Hash[value] as List<T>;
            return list != null && list.Count > 0 ? list[0] : default(T);
        }

        public IEnumerable<T> Scan(QueryParameter param)
        {
            if (param.Value == null)
                param.Value = DBNull.Value;
            if (!CheckParameter(param))
            {
                yield break;
            }
            var index = Hash;
            switch (param.Comparer.Type)
            {
                case CompareTypes.Equal:
                    if (param.Comparer.Not)
                    {
                        foreach (DictionaryEntry entry in index)
                        {
                            if (!entry.Key.Equals(param.Value))
                            {
                                foreach (T item in (IEnumerable<T>)entry.Value)
                                    yield return item;
                            }
                        }
                    }
                    else
                    {
                        var value = index[param.Value] as List<T>;
                        if (value != null)
                        {
                            foreach (T item in value)
                                yield return item;
                        }
                    }
                    break;
                case CompareTypes.Greater:
                    foreach (DictionaryEntry entry in index)
                    {
                        if (((IComparable)entry.Key).CompareTo(param.Value) > 0)
                        {
                            foreach (T item in (IEnumerable<T>)entry.Value)
                                yield return item;
                        }
                    }
                    break;
                case CompareTypes.GreaterOrEqual:
                    foreach (DictionaryEntry entry in index)
                    {
                        if (((IComparable)entry.Key).CompareTo(param.Value) >= 0)
                        {
                            foreach (T item in (IEnumerable<T>)entry.Value)
                                yield return item;
                        }
                    }
                    break;
                case CompareTypes.Less:
                    foreach (DictionaryEntry entry in index)
                    {
                        if (((IComparable)entry.Key).CompareTo(param.Value) < 0)
                        {
                            foreach (T item in (IEnumerable<T>)entry.Value)
                                yield return item;
                        }
                    }
                    break;
                case CompareTypes.LessOrEqual:
                    foreach (DictionaryEntry entry in index)
                    {
                        if (((IComparable)entry.Key).CompareTo(param.Value) <= 0)
                        {
                            foreach (T item in (IEnumerable<T>)entry.Value)
                                yield return item;
                        }
                    }
                    break;
                case CompareTypes.Like:
                    if (param.Value is string)
                    {
                        param.Value = ((string)param.Value).Trim(new char[] { '%' });
                        foreach (DictionaryEntry entry in index)
                        {
                            if (((string)entry.Key).IndexOf((string)param.Value, StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                foreach (T item in (IEnumerable<T>)entry.Value)
                                    yield return item;
                            }
                        }
                    }
                    break;
                case CompareTypes.Is:
                    if (param.Comparer.Not)
                    {
                        foreach (DictionaryEntry entry in index)
                        {
                            if (!entry.Key.Equals(DBNull.Value))
                            {
                                foreach (T item in (IEnumerable<T>)entry.Value)
                                    yield return item;
                            }
                        }
                    }
                    else
                    {
                        var value = index[DBNull.Value] as List<T>;
                        if (value != null)
                        {
                            foreach (T item in value)
                                yield return item;
                        }
                    }
                    break;
                case CompareTypes.In:
                    var list = param.Value as IList;
                    if (param.Comparer.Not)
                    {
                        foreach (DictionaryEntry entry in index)
                        {
                            if (!list.Contains(entry.Key))
                            {
                                foreach (T item in (IEnumerable<T>)entry.Value)
                                    yield return item;
                            }
                        }
                    }
                    else
                    {
                        foreach (var inItem in list)
                        {
                            var value = index[inItem] as List<T>;
                            if (value != null)
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
            return SelectOne(value);
        }

        IEnumerable IListIndex.Scan(QueryParameter parameter)
        {
            return Scan(parameter);
        }

        public void Clear()
        {
            Hash.Clear();
        }
    }
}

