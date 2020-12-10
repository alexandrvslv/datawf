using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace DataWF.Common
{
    public class PullIndex<T, K> : PullIndex, IDisposable
        where T : class, IPullHandler
    {
        private readonly ConcurrentDictionary<K, ThreadSafeList<T>> store;
        private readonly IComparer<T> valueComparer;
        private readonly IEqualityComparer<K> keyComparer;
        private readonly K nullKey;

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
            return Pull.GetValue(item.Block, item.BlockIndex);
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

        public F SelectOne<F>(object value) where F : T
        {
            var key = CheckNull(value);
            if (store.TryGetValue(key, out var list))
                return (F)list[0];
            else
                return default(F);
        }

        public F SelectOne<F>(K key) where F : T
        {
            CheckNull(ref key);
            if (store.TryGetValue(key, out var list))
                return (F)list[0];
            else
                return default(F);
        }

        public IEnumerable<F> Select<F>(K key) where F : T
        {
            CheckNull(ref key);
            if (store.TryGetValue(key, out var list))
            {
                foreach (var item in list)
                {
                    if (item is F fitem)
                    {
                        yield return fitem;
                    }
                }
            }
            else
            {
                yield break;
            }
        }

        public IEnumerable<F> Search<F>(Predicate<K> comparer) where F : T
        {
            foreach (var entry in store)
            {
                if (comparer(entry.Key))
                {
                    foreach (var item in entry.Value)
                    {
                        if (item is F fitem)
                        {
                            yield return fitem;
                        }
                    }
                }
            }
        }

        public IEnumerable<F> Select<F>(object value, CompareType compare) where F : T
        {
            IEnumerable<F> buf = Enumerable.Empty<F>();

            switch (compare.Type)
            {
                case CompareTypes.Like:
                    var regex = value as Regex ?? Helper.BuildLike(value.ToString());
                    buf = Search<F>((item) => regex.IsMatch(item.ToString()));
                    break;
                case CompareTypes.In:
                    //&& value is IList
                    if (!compare.Not)
                    {
                        foreach (var item in (IEnumerable)value)
                        {
                            object comp = item;
                            if (comp is IValued valued)
                                comp = valued.GetValue();
                            if (comp is string stringed)
                                comp = stringed.Trim(' ', '\'');

                            var temp = Select<F>(CheckNull(comp));
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
                        buf = Search<F>((item) =>
                        {
                            foreach (var element in (IEnumerable)value)
                            {
                                object comp = element;
                                if (comp is IValued valued)
                                    comp = valued.GetValue();
                                if (comp is string stringed)
                                    comp = stringed.Trim(' ', '\'');
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
                    buf = Select<F>(min);
                    buf = buf.Concat(Select<F>(max));
                    buf = buf.Concat(Search<F>((item) => ListHelper.Compare(item, max) > 0
                                                      && ListHelper.Compare(item, min) < 0));
                    break;
                default:
                    buf = Select<F>(CheckNull(value), compare);
                    break;
            }
            return buf;
        }

        public IEnumerable<F> Select<F>(K key, CompareType compare) where F : T
        {
            switch (compare.Type)
            {
                case CompareTypes.Is:
                    if (!compare.Not)
                        return Select<F>(nullKey);
                    else
                        return Search<F>((item) => !ListHelper.Equal<K>(item, nullKey));
                case CompareTypes.Equal:
                    if (!compare.Not)
                        return Select<F>(key);
                    else
                    {
                        CheckNull(ref key);
                        return Search<F>((item) => !ListHelper.Equal<K>(item, key));
                    }
                case CompareTypes.Greater:
                    CheckNull(ref key);
                    return Search<F>((item) => ListHelper.Compare(item, key) > 0);
                case CompareTypes.GreaterOrEqual:
                    CheckNull(ref key);
                    return Select<F>(key).Concat(Search<F>((item) => ListHelper.Compare(item, key) > 0));
                case CompareTypes.Less:
                    CheckNull(ref key);
                    return Search<F>((item) => ListHelper.Compare(item, key) < 0);
                case CompareTypes.LessOrEqual:
                    CheckNull(ref key);
                    return Select<F>(key).Concat(Search<F>((item) => ListHelper.Compare(item, key) < 0));
            }
            return Enumerable.Empty<F>();
        }

        public override void Clear()
        {
            store.Clear();
        }

        public override IEnumerable Select(object value, CompareType compare)
        {
            return Select<T>(value, compare);
        }

        public override object SelectOne(object value)
        {
            return SelectOne<T>(value);
        }

        public override void Dispose()
        {
            store?.Clear();
        }
    }



}
