using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;


namespace DataWF.Data
{

    public abstract class DBPullIndex : IDisposable
    {
        public static DBPullIndex Fabric(DBTable list, DBColumn column)
        {
            if (column.DataType == null)
                throw new ArgumentException($"Type is null on column {column.FullName}");

            Type gtype = typeof(DBPullIndex<>).MakeGenericType(column.DataType);

            return (DBPullIndex)EmitInvoker.CreateObject(gtype, new Type[] { typeof(DBTable), typeof(DBColumn) }, new object[] { list, column }, true);
        }

        public abstract void Refresh(ListChangedType type, DBItem row);
        public abstract void Refresh();
        public abstract void RefreshItem(DBItem row);
        public abstract void RefreshSort(DBItem row);
        public abstract void Add(DBItem row);
        public abstract void Add(DBItem row, object value);
        public abstract void Remove(DBItem row);
        public abstract void Remove(DBItem dBItem, object value);
        public abstract IEnumerable<T> Select<T>(object value, CompareType compare) where T : DBItem, new();
        public abstract T SelectOne<T>(object value) where T : DBItem, new();
        public abstract void Clear();
        public abstract void Dispose();
        public abstract Pull Pull { get; }
    }

    public class DBPullIndex<K> : DBPullIndex
    {
        private DBTable table;
        private Dictionary<DBNullable<K>, List<DBItem>> store;
        private Pull pull;

        public DBPullIndex(DBTable rows, DBColumn column)
            : this(rows, column.Pull)
        { }

        public DBPullIndex(DBTable table, Pull pull)
        {
            this.table = table;
            this.pull = pull;
            if (typeof(K) == typeof(string))
            {
                store = new Dictionary<DBNullable<K>, List<DBItem>>((IEqualityComparer<DBNullable<K>>)DBNullableComparer.StringOrdinalIgnoreCase);
            }
            else
            {
                store = new Dictionary<DBNullable<K>, List<DBItem>>();
            }
            Refresh();
        }

        public override Pull Pull { get { return pull; } }

        public override void Refresh(ListChangedType type, DBItem row)
        {
            if (type == ListChangedType.Reset)
            {
                Refresh();
            }
            else if (type == ListChangedType.ItemAdded)
            {
                Add(row);
            }
            else if (type == ListChangedType.ItemDeleted && row != null)
            {
                Remove(row);
            }
        }

        public override void Refresh()
        {
            store.Clear();
            foreach (var item in table)
            {
                Add(item);
            }
        }

        public DBNullable<K> ReadItem(DBItem row)
        {
            return Convert(pull.Get(row.handler));
        }

        public DBNullable<K> Convert(object value)
        {
            if (pull.EqualNull(value))
                return new DBNullable<K>();
            if (value is DBNullable<K>)
                return (DBNullable<K>)value;
            return (K)value;
        }

        public override void RefreshItem(DBItem row)
        {
            Remove(row);
            Add(row);
        }

        public override void Remove(DBItem row)
        {
            Remove(row, ReadItem(row));
        }

        public override void Remove(DBItem row, object value)
        {
            Remove(row, Convert(value));
        }

        public override void RefreshSort(DBItem row)
        {
            var key = ReadItem(row);
            if (store.TryGetValue(key, out var val))
            {
                val.Sort(row.Table.DefaultComparer);
            }
        }

        public void Remove(DBItem row, DBNullable<K> key)
        {
            lock (store)
            {
                List<DBItem> val;
                if (store.TryGetValue(key, out val))
                {
                    if (val.Count <= 1)
                    {
                        store.Remove(key);
                        return;
                    }
                    else
                    {
                        var index = val.BinarySearch(row, row.Table.DefaultComparer);
                        if (index < 0)
                        {
                            index = val.IndexOf(row);
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
                    var index = list.IndexOf(row);
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

        public override void Add(DBItem row)
        {
            Add(row, ReadItem(row));
        }

        public override void Add(DBItem row, object value)
        {
            Add(row, Convert(value));
        }

        public void Add(DBItem row, DBNullable<K> key)
        {
            lock (store)
            {
                List<DBItem> list;
                if (!store.TryGetValue(key, out list))
                {
                    list = new List<DBItem>(1);
                    list.Add(row);
                    store.Add(key, list);
                }
                else
                {
                    var index = list.BinarySearch(row, table.DefaultComparer);
                    if (index < 0)
                        list.Insert(-index - 1, row);
                }
            }
        }

        public override T SelectOne<T>(object value)
        {
            var key = Convert(value);
            List<DBItem> list;
            if (store.TryGetValue(key, out list) && list[0] is T item)
                return item;
            else
                return null;
        }

        public IEnumerable<T> Select<T>(DBNullable<K> key) where T : DBItem, new()
        {
            List<DBItem> list;
            if (store.TryGetValue(key, out list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is T item)
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

        public IEnumerable<T> Search<T>(Func<DBNullable<K>, bool> comparer) where T : DBItem, new()
        {
            foreach (var entry in store)
            {
                if (comparer(entry.Key))
                {
                    for (int i = 0; i < entry.Value.Count; i++)
                    {
                        if (entry.Value[i] is T item)
                            yield return item;
                    }
                }
            }
        }

        public override IEnumerable<T> Select<T>(object value, CompareType compare)
        {
            IEnumerable<T> buf = null;

            if (compare.Type.Equals(CompareTypes.Is))
            {
                compare.Type = CompareTypes.Equal;
                value = new DBNullable<K>();
            }
            if (compare.Type.Equals(CompareTypes.Equal))
            {
                var key = Convert(value);
                if (!compare.Not)
                {
                    buf = Select<T>(key);
                }
                else
                {
                    buf = Search<T>((item) => !item.Equals(key));
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
                        if (comp is QItem)
                            comp = ((QItem)comp).GetValue();
                        if (comp is string)
                            comp = ((string)comp).Trim(' ', '\'');

                        var temp = Select<T>(Convert(comp));
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
                    buf = Search<T>((item) =>
                    {
                        foreach (var element in (IEnumerable)value)
                        {
                            object comp = element;
                            if (comp is QItem)
                                comp = ((QItem)comp).GetValue();
                            if (comp is string)
                                comp = ((string)comp).Trim(' ', '\'');
                            if (item.Equals(comp))
                                return false;
                        }
                        return true;
                    });
                }
            }
            else if (compare.Type.Equals(CompareTypes.Between))
            {
                var between = value as QBetween;
                if (between == null)
                    throw new Exception("Expect QBetween but Get " + value == null ? "null" : value.GetType().FullName);
                var min = Convert(between.Min.GetValue());
                var max = Convert(between.Max.GetValue());
                buf = Select<T>(min);
                buf = buf.Concat(Select<T>(max));
                buf = buf.Concat(Search<T>((item) => ((IComparable)item).CompareTo(max) > 0));
                buf = buf.Concat(Search<T>((item) => ((IComparable)item).CompareTo(min) < 0));
            }
            else if (compare.Type.Equals(CompareTypes.Like))
            {
                var regex = value is Regex ? (Regex)value : Helper.BuildLike(value.ToString());
                buf = Search<T>((item) => regex.IsMatch(item.ToString()));
            }
            else if (value is IComparable)
            {
                var key = Convert(value);

                if (compare.Type.Equals(CompareTypes.Greater))
                {
                    buf = Search<T>((item) => ((IComparable)item).CompareTo(key) > 0);
                }
                else if (compare.Type.Equals(CompareTypes.GreaterOrEqual))
                {
                    buf = Select<T>(key);
                    buf = buf.Concat(Search<T>((item) => ((IComparable)item).CompareTo(key) > 0));
                }
                else if (compare.Type.Equals(CompareTypes.Less))
                {
                    buf = Search<T>((item) => ((IComparable)item).CompareTo(key) < 0);
                }
                else if (compare.Type.Equals(CompareTypes.LessOrEqual))
                {
                    buf = Select<T>(key);
                    buf = buf.Concat(Search<T>((item) => ((IComparable)item).CompareTo(key) < 0));
                }
            }
            return buf;
        }

        public override void Dispose()
        {
            store.Clear();
        }

        public override void Clear()
        {
            store.Clear();
        }
    }
}
