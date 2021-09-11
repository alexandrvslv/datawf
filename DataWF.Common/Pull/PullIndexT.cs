using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace DataWF.Common
{
    public class PullIndex<T, K> : PullIndex, IDisposable, IPullInIndex<T, K>, IPullOutIndex<T, K> where T : class, IPullHandler
    {
        private readonly ConcurrentDictionary<K, ThreadSafeList<T>> store;
        private readonly IComparer<T> valueComparer;
        private readonly IEqualityComparer<K> keyComparer;
        private K nullKey;

        public PullIndex(Pull pull, object nullKey, IComparer valueComparer = null, IEqualityComparer keyComparer = null)
            : this((GenericPull<K>)pull,
                  (K)nullKey,
                  valueComparer as IComparer<T> ?? Comparer<T>.Default,
                  keyComparer as IEqualityComparer<K> ?? EqualityComparer<K>.Default)
        { }

        public PullIndex(GenericPull<K> pull, K nullKey, IComparer<T> valueComparer, IEqualityComparer<K> keyComparer)
        {
            Pull = pull;
            this.nullKey = (K)nullKey;
            this.valueComparer = valueComparer;
            this.keyComparer = keyComparer;
            this.store = new ConcurrentDictionary<K, ThreadSafeList<T>>(this.keyComparer);
        }

        public override Pull BasePull => Pull;

        public GenericPull<K> Pull { get; }

        public K NullKey => nullKey;

        public override void Refresh(IEnumerable items)
        {
            Refresh(items.TypeOf<T>());
        }

        public virtual void Refresh(IEnumerable<T> items)
        {
            store.Clear();
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public override void RefreshItem(object item)
        {
            RefreshItem((T)item);
        }

        public void RefreshItem(T item)
        {
            Remove(item);
            Add(item);
        }

        public override void Add(object item)
        {
            Add((T)item);
        }

        public virtual void Add(T item)
        {
            Add(item, ReadItem(item));
        }

        public override void Add(object item, object key)
        {
            Add((T)item, CheckNull(key));
        }

        public void Add(T item, K key)
        {
            CheckNull(ref key);
            lock (store)
            {
                if (!store.TryGetValue(key, out ThreadSafeList<T> list))
                {
                    store[key] = new ThreadSafeList<T>(item);
                }
                else
                {
                    var index = list.BinarySearch(item, valueComparer);
                    if (index < 0)
                    {
                        list.Insert(-index - 1, item);
                    }
                }
            }
        }

        public override void Remove(object item)
        {
            Remove((T)item);
        }

        public virtual void Remove(T item)
        {
            Remove(item, ReadItem(item));
        }

        public override void Remove(object item, object value)
        {
            Remove((T)item, CheckNull(value));
        }

        public void Remove(T item, K key)
        {
            CheckNull(ref key);
            lock (store)
            {
                if (store.TryGetValue(key, out var val))
                {
                    if (val.Count <= 1)
                    {
                        store.TryRemove(key, out val);
                        return;
                    }
                    else
                    {
                        var index = val.BinarySearch(item, valueComparer);
                        if (index < 0)
                        {
                            index = val.IndexOf(item);
                        }

                        if (index >= 0)
                        {
                            val.RemoveAt(index);
                            return;
                        }
                    }
                }
                foreach (var de in store)
                {
                    var list = de.Value;
                    var index = list.IndexOf(item);
                    if (index >= 0)
                    {
                        if (list.Count == 1)
                            store.TryRemove(de.Key, out list);
                        else
                            list.RemoveAt(index);
                        break;
                    }
                }
            }
        }

        public override void RefreshSort(object item)
        {
            RefreshSort((T)item);
        }

        public virtual void RefreshSort(T item)
        {
            var key = ReadItem(item);
            CheckNull(ref key);
            if (store.TryGetValue(key, out var val))
            {
                var index = val.BinarySearch(item, valueComparer);
                if (index < 0 || val[index] != item)
                {
                    val.Sort(valueComparer);
                }
            }
        }

        public K ReadItem(T item)
        {
            return Pull.GetValue(in item.GetRefHandler());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CheckNull(ref K key)
        {
            if (keyComparer.Equals(key, default(K)))
            {
                key = nullKey;
            }
        }

        public K CheckNull(object value)
        {
            if (value == null)
            {
                return nullKey;
            }

            if (value is K typed)
            {
                return keyComparer.Equals(typed, default(K)) ? nullKey : typed;
            }

            return (K)Helper.Parse(value, typeof(K));
        }

        public T SelectOne(object value)
        {
            var key = CheckNull(value);
            if (store.TryGetValue(key, out var list))
                return list[0];
            else
                return default(T);
        }

        public T SelectOne(K key)
        {
            CheckNull(ref key);
            if (store.TryGetValue(key, out var list))
                return list[0];
            else
                return default(T);
        }

        public ReadOnlySpan<T> SelectSpan(K key)
        {
            CheckNull(ref key);
            if (store.TryGetValue(key, out var list))
            {
                return list.AsSpan();
            }
            return ReadOnlySpan<T>.Empty;
        }

        public IPullIndexCollection<T> Select(K key)
        {
            CheckNull(ref key);
            if (store.TryGetValue(key, out var list))
            {
                return new PullIndexCollection<T>(Enumerable.Repeat(list, 1), valueComparer);
            }
            return PullIndexCollection<T>.Empty;
        }

        protected IEnumerable<ThreadSafeList<T>> SelectInternal(K key)
        {
            CheckNull(ref key);
            if (store.TryGetValue(key, out var list))
            {
                return Enumerable.Repeat(list, 1);
            }
            return Enumerable.Empty<ThreadSafeList<T>>();
        }

        public IPullIndexCollection<T> Search(Predicate<K> comparer)
        {
            return new PullIndexCollection<T>(SearchInternal(comparer), valueComparer);
        }

        protected IEnumerable<ThreadSafeList<T>> SearchInternal(Predicate<K> comparer)
        {
            foreach (var entry in store)
            {
                if (comparer(entry.Key))
                {
                    yield return entry.Value;
                }
            }
        }

        public IPullIndexCollection<T> Select(object value, CompareType comparer)
        {
            return new PullIndexCollection<T>(SelectInternal(value, comparer), valueComparer);
        }

        protected IEnumerable<ThreadSafeList<T>> SelectInternal(object value, CompareType comparer)
        {
            IEnumerable<ThreadSafeList<T>> buf = null;

            switch (comparer.Type)
            {
                case CompareTypes.Like:
                    var regex = value as Regex ?? Helper.BuildLike(value.ToString());
                    buf = SearchInternal((item) => regex.IsMatch(item.ToString()));
                    break;
                case CompareTypes.In:
                    //&& value is IList
                    if (!comparer.Not)
                    {
                        foreach (var item in (IEnumerable)value)
                        {
                            object comp = item;
                            if (comp is IValued valued)
                            {
                                comp = valued.GetValue<T>();
                            }

                            var temp = SelectInternal(CheckNull(comp));
                            if (buf == null)
                            {
                                buf = temp;
                            }
                            else
                            {
                                buf = buf.Concat(temp);
                            }
                        }
                    }
                    else
                    {
                        buf = SearchInternal((item) =>
                        {
                            foreach (var element in (IEnumerable)value)
                            {
                                object comp = element;
                                if (comp is IValued valued)
                                {
                                    comp = valued.GetValue<T>();
                                }

                                if (item.Equals(comp))
                                    return false;
                            }
                            return true;
                        });
                    }
                    break;
                case CompareTypes.Between:
                    if (!(value is IBetween between))
                        throw new Exception("Expect QBetween but Get " + value == null ? "null" : value.GetType().FullName);
                    var min = CheckNull(between.MinValue());
                    var max = CheckNull(between.MaxValue());
                    buf = SearchInternal((item) => ListHelper.Compare(item, max) >= 0
                                        && ListHelper.Compare(item, min) <= 0);
                    break;
                default:
                    buf = SelectInternal(CheckNull(value), comparer);
                    break;
            }
            return buf ?? Enumerable.Empty<ThreadSafeList<T>>();
        }

        public IPullIndexCollection<T> Select(K key, CompareType comparer)
        {
            return new PullIndexCollection<T>(SelectInternal(key, comparer), valueComparer);
        }

        protected IEnumerable<ThreadSafeList<T>> SelectInternal(K key, CompareType compare)
        {
            switch (compare.Type)
            {
                case CompareTypes.Is:
                    if (!compare.Not)
                        return SelectInternal(nullKey);
                    else
                        return SearchInternal((item) => !ListHelper.Equal<K>(item, nullKey));
                case CompareTypes.Equal:
                    if (!compare.Not)
                        return SelectInternal(key);
                    else
                    {
                        CheckNull(ref key);
                        return SearchInternal((item) => !ListHelper.Equal<K>(item, key));
                    }
                case CompareTypes.Greater:
                    CheckNull(ref key);
                    return SearchInternal((item) => ListHelper.Compare(item, key) > 0);
                case CompareTypes.GreaterOrEqual:
                    CheckNull(ref key);
                    return SelectInternal(key).Concat(SearchInternal((item) => ListHelper.Compare(item, key) > 0));
                case CompareTypes.Less:
                    CheckNull(ref key);
                    return SearchInternal((item) => ListHelper.Compare(item, key) < 0);
                case CompareTypes.LessOrEqual:
                    CheckNull(ref key);
                    return SelectInternal(key).Concat(SearchInternal((item) => ListHelper.Compare(item, key) < 0));
            }
            return Enumerable.Empty<ThreadSafeList<T>>();
        }

        public override void Clear()
        {
            store.Clear();
        }

        public override IPullIndexCollection SelectObjects(object value, CompareType compare)
        {
            return Select(value, compare);
        }

        public override object SelectOneObject(object value)
        {
            return SelectOne(value);
        }

        public override void Dispose()
        {
            store?.Clear();
        }
    }



}
