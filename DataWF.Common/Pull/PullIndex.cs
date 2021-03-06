﻿using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace DataWF.Data
{
    public abstract class PullIndex : IDisposable
    {
        public abstract Pull BasePull { get; }
        public abstract void Refresh(IEnumerable items);
        public abstract void RefreshItem(object item);
        public abstract void RefreshSort(object item);
        public abstract void Add(object item);
        public abstract void Add(object item, object value);
        public void Add<T, V>(T item, V value) where T : class, IPullHandler
        {
            var pull = (PullIndex<T, V>)this;
            pull.CheckNull(ref value);
            pull.Add(item, value);
        }

        public abstract void Remove(object item);
        public abstract void Remove(object item, object value);
        public void Remove<T, V>(T item, V value) where T : class, IPullHandler
        {
            var pull = (PullIndex<T, V>)this;
            pull.CheckNull(ref value);
            pull.Remove(item, value);
        }
        public abstract IEnumerable Select(object value, CompareType compare);
        public abstract IEnumerable<F> Select<F>(object value, CompareType compare) where F : class;
        public abstract object SelectOne(object value);
        public abstract F SelectOne<F>(object value) where F : class;
        public abstract F SelectOne<F, K>(K value) where F : class;
        public abstract void Clear();
        public abstract void Dispose();
    }

    public class PullIndex<T, K> : PullIndex, IDisposable where T : class, IPullHandler
    {
        private ConcurrentDictionary<K, ThreadSafeList<T>> store;
        private readonly IComparer<T> valueComparer;
        private readonly IEqualityComparer<K> keyComparer;
        private readonly K nullKey;

        public PullIndex(Pull pull, object nullKey, IComparer valueComparer = null, IEqualityComparer keyComparer = null)
        {
            Pull = (GenericPull<K>)pull;
            this.nullKey = (K)nullKey;
            this.valueComparer = (valueComparer as IComparer<T>) ?? Comparer<T>.Default;
            this.keyComparer = (keyComparer as IEqualityComparer<K>) ?? EqualityComparer<K>.Default;
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
            var key = Pull.GetValue(item.Block, item.BlockIndex);
            CheckNull(ref key);
            return key;
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

        public override F SelectOne<F>(object value)
        {
            var key = CheckNull(value);
            if (store.TryGetValue(key, out var list))
                return list[0] as F;
            else
                return default(F);
        }

        public override F SelectOne<F, KV>(KV value)
        {
            var key = Unsafe.As<KV, K>(ref value);
            CheckNull(ref key);
            if (store.TryGetValue(key, out var list))
                return list[0] as F;
            else
                return default(F);
        }

        public IEnumerable<F> Select<F>(K key) where F : class
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

        public IEnumerable<F> Search<F>(Predicate<K> comparer) where F : class
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

        public override IEnumerable<F> Select<F>(object value, CompareType compare)
        {
            IEnumerable<F> buf = Enumerable.Empty<F>();

            if (compare.Type.Equals(CompareTypes.Is))
            {
                compare = new CompareType(CompareTypes.Equal, compare.Not);
                value = nullKey;
            }
            if (compare.Type.Equals(CompareTypes.Equal))
            {
                var key = CheckNull(value);
                if (!compare.Not)
                {
                    buf = Select<F>(key);
                }
                else
                {
                    buf = Search<F>((item) => !item.Equals(key));
                }
            }
            else if (compare.Type.Equals(CompareTypes.In))
            {
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
            }
            else if (compare.Type.Equals(CompareTypes.Between))
            {
                if (!(value is IBetween between))
                    throw new Exception("Expect QBetween but Get " + value == null ? "null" : value.GetType().FullName);
                var min = CheckNull(between.MinValue());
                var max = CheckNull(between.MaxValue());
                buf = Select<F>(min);
                buf = buf.Concat(Select<F>(max));
                buf = buf.Concat(Search<F>((item) => ((IComparable)item).CompareTo(max) > 0));
                buf = buf.Concat(Search<F>((item) => ((IComparable)item).CompareTo(min) < 0));
            }
            else if (compare.Type.Equals(CompareTypes.Like))
            {
                var regex = value is Regex ? (Regex)value : Helper.BuildLike(value.ToString());
                buf = Search<F>((item) => regex.IsMatch(item.ToString()));
            }
            else if (value is IComparable)
            {
                var key = CheckNull(value);

                if (compare.Type.Equals(CompareTypes.Greater))
                {
                    buf = Search<F>((item) => ((IComparable)item).CompareTo(key) > 0);
                }
                else if (compare.Type.Equals(CompareTypes.GreaterOrEqual))
                {
                    buf = Select<F>(key);
                    buf = buf.Concat(Search<F>((item) => ((IComparable)item).CompareTo(key) > 0));
                }
                else if (compare.Type.Equals(CompareTypes.Less))
                {
                    buf = Search<F>((item) => ((IComparable)item).CompareTo(key) < 0);
                }
                else if (compare.Type.Equals(CompareTypes.LessOrEqual))
                {
                    buf = Select<F>(key);
                    buf = buf.Concat(Search<F>((item) => ((IComparable)item).CompareTo(key) < 0));
                }
            }
            return buf;
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
            store = null;
        }
    }



}
