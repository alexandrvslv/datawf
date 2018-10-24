using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace DataWF.Data
{
    public class NullablePullIndex<T, K> : PullIndex<T, K?> where T : class, IPullHandler where K : struct
    {

        public NullablePullIndex(Pull pull, object nullKey, IComparer valueComparer, IEqualityComparer keyComparer = null)
            : base(pull, nullKey.GetType() == typeof(K) ? (K?)(K)nullKey : (K?)nullKey, valueComparer, keyComparer)
        {
        }
    }

    public abstract class PullIndex : IDisposable
    {
        public abstract Pull BasePull { get; }
        public abstract void Refresh(IEnumerable items);
        public abstract void RefreshItem(object item);
        public abstract void RefreshSort(object item);
        public abstract void Add(object item);
        public abstract void Add(object item, object value);
        public abstract void Remove(object item);
        public abstract void Remove(object item, object value);
        public abstract IEnumerable Select(object value, CompareType compare);
        public abstract IEnumerable<F> Select<F>(object value, CompareType compare) where F : class;
        public abstract object SelectOne(object value);
        public abstract F SelectOne<F>(object value) where F : class;
        public abstract void Clear();
        public abstract void Dispose();
    }

    public class PullIndex<T, K> : PullIndex, IDisposable where T : class, IPullHandler
    {
        private Dictionary<K, List<T>> store;
        private readonly IComparer<T> comparer;
        private readonly K nullKey;

        public PullIndex(Pull pull, object nullKey, IComparer valueComparer = null, IEqualityComparer keyComparer = null)
        {
            Pull = (Pull<K>)pull;
            this.nullKey = (K)nullKey;
            comparer = valueComparer as IComparer<T>;
            if (keyComparer is IEqualityComparer<K> typedKeyComparer)
            {
                store = new Dictionary<K, List<T>>(typedKeyComparer);//(IEqualityComparer<DBNullable<K>>)DBNullableComparer.StringOrdinalIgnoreCase
            }
            else
            {
                store = new Dictionary<K, List<T>>();
            }
        }

        public override Pull BasePull => Pull;

        public Pull<K> Pull { get; }

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
                if (!store.TryGetValue(key, out List<T> list))
                {
                    list = new List<T>(1) { item };
                    store.Add(key, list);
                }
                else
                {
                    var index = list.BinarySearch(item, comparer);
                    if (index < 0)
                        list.Insert(-index - 1, item);
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
                        store.Remove(key);
                        return;
                    }
                    else
                    {
                        var index = val.BinarySearch(item, comparer);
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
                            store.Remove(de.Key);
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
                val.Sort(comparer);
            }
        }

        public K ReadItem(T item)
        {
            var key = Pull.GetValueInternal(item.Handler);
            CheckNull(ref key);
            return key;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckNull(ref K key)
        {
            if (EqualityComparer<K>.Default.Equals(key, default(K)))
            {
                key = nullKey;
            }
        }

        public K CheckNull(object value)
        {
            if (value == null)
                return nullKey;
            if (value is K typed)
                return EqualityComparer<K>.Default.Equals(typed, default(K)) ? nullKey : typed;
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

        public IEnumerable<F> Select<F>(K key) where F : class
        {
            CheckNull(ref key);
            if (store.TryGetValue(key, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    F item = list[i] as F;
                    if (item != null)
                    {
                        yield return item;
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
                    for (int i = 0; i < entry.Value.Count; i++)
                    {
                        var item = entry.Value[i] as F;
                        if (item != null)
                            yield return item;
                    }
                }
            }
        }

        public override IEnumerable<F> Select<F>(object value, CompareType compare)
        {
            IEnumerable<F> buf = null;

            if (compare.Type.Equals(CompareTypes.Is))
            {
                compare.Type = CompareTypes.Equal;
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
