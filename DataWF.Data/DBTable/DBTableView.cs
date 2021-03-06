﻿//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBTableView<T> : SelectableList<T>, IDBTableView, IIdCollection<T> where T : DBItem, new()
    {
        protected DBViewKeys keys = DBViewKeys.Lock;
        protected QParam defaultParam;
        protected QQuery query;

        protected IDbCommand command;
        protected DBTable<T> table;
        private Query<T> filterQuery;

        public DBTableView()
           : this(DBTable.GetTable<T>(null, false), (QParam)null, DBViewKeys.None, DBStatus.Empty)
        { }

        public DBTableView(string defaultFilter, DBViewKeys mode = DBViewKeys.None, DBStatus statusFilter = DBStatus.Empty)
            : this(DBTable.GetTable<T>(null, false), defaultFilter, mode, statusFilter)
        { }

        public DBTableView(QParam defaultFilter, DBViewKeys mode = DBViewKeys.None, DBStatus statusFilter = DBStatus.Empty)
            : this(DBTable.GetTable<T>(null, false), defaultFilter, mode, statusFilter)
        { }

        public DBTableView(DBTable<T> table, string defaultFilter, DBViewKeys mode = DBViewKeys.None, DBStatus statusFilter = DBStatus.Empty)
            : this(table, !string.IsNullOrEmpty(defaultFilter) ? new QParam(table, defaultFilter) : null, mode, statusFilter)
        { }

        public DBTableView(DBTable<T> table, QParam defaultFilter, DBViewKeys mode = DBViewKeys.None, DBStatus statusFilter = DBStatus.Empty)
        {
            propertyHandler = null;
            this.table = table;
            FilterQuery = new Query<T>();
            Query = new QQuery();
            TypeFilter = typeof(T);
            DefaultParam = defaultFilter;
            StatusFilter = statusFilter;
            keys = mode;
            table.AddView(this);
            if ((keys & DBViewKeys.Empty) != DBViewKeys.Empty)
            {
                UpdateFilter();
            }
        }

        ~DBTableView()
        {
            Dispose();
        }

        public bool AutoAttach
        {
            get { return (keys & DBViewKeys.NoAttach) != DBViewKeys.NoAttach; }
            set
            {
                if (value)
                    keys &= ~DBViewKeys.NoAttach;
                else
                    keys |= DBViewKeys.NoAttach;
            }
        }

        public bool CheckAccess
        {
            get { return (keys & DBViewKeys.Access) == DBViewKeys.Access; }
            set
            {
                if (CheckAccess == value)
                    return;
                if (value)
                {
                    keys |= DBViewKeys.Access;
                }
                else
                {
                    keys &= ~DBViewKeys.Access;
                }
            }
        }

        public bool IsStatic
        {
            get { return (keys & DBViewKeys.Static) == DBViewKeys.Static; }
            set
            {
                if (IsStatic == value)
                    return;
                if (value)
                    keys |= DBViewKeys.Static;
                else
                    keys &= ~DBViewKeys.Static;
                Clear();
            }
        }

        public override bool IsSynchronized
        {
            get { return (keys & DBViewKeys.Synch) == DBViewKeys.Synch; }
            set
            {
                if (IsSynchronized != value)
                {
                    if (value)
                        keys |= DBViewKeys.Synch;
                    else
                        keys &= ~DBViewKeys.Synch;
                }
            }
        }

        public T this[string code]
        {
            get
            {
                if (table.CodeKey == null)
                    return null;
                return (T)table.LoadItemByCode(code, table.CodeKey, DBLoadParam.Load);
            }
        }

        public QQuery Query
        {
            get { return query; }
            set
            {
                if (value == null)
                    throw new InvalidAsynchronousStateException();
                if (value != null)
                {
                    query = value;
                    query.Table = table;
                    UpdateFilter();
                }
            }
        }

        Common.IQuery IFilterable.FilterQuery
        {
            get => FilterQuery;
            set => FilterQuery = (Query<T>)value;
        }

        public Query<T> FilterQuery
        {
            get { return filterQuery; }
            set
            {
                filterQuery = value;
                filterQuery.Parameters.CollectionChanged += (s, e) =>
                {
                    CheckFilterQuery();
                };
            }
        }

        public DBStatus StatusFilter
        {
            get { return Query.StatusFilter; }
            set
            {
                if (Query.StatusFilter == value)
                    return;
                Query.StatusFilter = value;

                UpdateFilter();
            }
        }

        public Type TypeFilter
        {
            get { return Query.TypeFilter; }
            set
            {
                if (Query.TypeFilter == value)
                    return;
                Query.TypeFilter = value;

                UpdateFilter();
            }
        }

        public event EventHandler DefaultFilterChanged;
        public event EventHandler FilterChanged;

        public virtual QParam DefaultParam
        {
            get { return defaultParam; }
            set
            {
                if (defaultParam == value)
                    return;
                if (defaultParam != null)
                {
                    Query.Parameters.Remove(defaultParam);
                }
                defaultParam = value;
                if (defaultParam != null)
                {
                    defaultParam.IsDefault = true;
                    Query.Parameters.Insert(0, defaultParam);
                }
                UpdateFilter();
                DefaultFilterChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public IDbCommand Command
        {
            get { return command; }
            set { command = value; }
        }

        public bool IsEdited
        {
            get { return GetEdited().Any(); }
        }

        public DBSchema Schema
        {
            get { return Table?.Schema; }
        }

        public DBTable Table
        {
            get { return table; }
            set { table = value as DBTable<T>; }
        }

        public IEnumerable Source { get { return table; } set { return; } }

        public IList ToList()
        {
            List<T> list = new List<T>(Count);
            foreach (T item in this)
                list.Add(item);
            return list;
        }

        IEnumerable<DBItem> IDBTableView.Load(DBLoadParam param)
        {
            return Load(param);
        }

        public IEnumerable<T> Load(DBLoadParam param = DBLoadParam.None)
        {
            using (var transaction = new DBTransaction(Table.Connection, null, true) { View = this })
            {
                return table.Load(Query, param, transaction);
            }
        }

        public async void LoadAsynch(DBLoadParam param = DBLoadParam.None)
        {
            using (var transaction = new DBTransaction(Table.Connection, null, true) { View = this })
            {
                var items = await table.LoadAsync(Query, param, transaction).ConfigureAwait(false);
            }
        }

        public void OnSourceItemChanged(object sender, PropertyChangedEventArgs args)
        {
            OnSourceItemChanged((T)sender, args.PropertyName, Table.ParseColumnProperty(args.PropertyName));
        }

        public void OnSourceItemChanged(DBItem item, string propertyName, DBColumn column)
        {
            if (item is T titem)
            {
                OnSourceItemChanged(titem, propertyName, column);
            }
        }

        public void OnSourceItemChanged(T item, string propertyName, DBColumn column)
        {
            var indexes = GetIndex(item);

            if (indexes.Item1 < 0)
            {
                if (table.CheckItem(item, query))
                    Insert(indexes.Item2, item);
            }
            else if (!table.CheckItem(item, query))
            {
                RemoveAt(indexes.Item1);
            }
            else
            {
                OnItemPropertyChanged(item, indexes.Item1, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Reset)
            {
                OnSourceCollectioChanged((T)null, args.Action);
            }
            else if (args.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (T item in args.NewItems)
                {
                    OnSourceCollectioChanged(item, args.Action);
                }
            }
            else if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (T item in args.OldItems)
                {
                    OnSourceCollectioChanged(item, args.Action);
                }
            }
        }

        public void OnSourceCollectioChanged(DBItem item, NotifyCollectionChangedAction type)
        {
            if (item == null || item is T)
            {
                OnSourceCollectionChanged((T)item, type);
            }
        }

        public void OnSourceCollectionChanged(T item, NotifyCollectionChangedAction type)
        {
            lock (items)
            {
                switch (type)
                {
                    case NotifyCollectionChangedAction.Reset:
                        UpdateFilter();
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        var indexes = GetIndex(item);
                        if (indexes.Item1 >= 0)
                        {
                            RemoveAt(indexes.Item1);
                        }
                        break;
                    case NotifyCollectionChangedAction.Add:
                        if ((keys & DBViewKeys.Static) != DBViewKeys.Static && table.CheckItem(item, query))
                        {
                            Add(item);
                        }
                        break;
                }
            }
        }

        public (int, int) GetIndex(T item)
        {
            var index = items.BinarySearch(item, comparer);
            var newIndex = index;
            if (index < 0)
            {
                newIndex = (-index) - 1;
                if (newIndex > items.Count)
                    newIndex = items.Count;
            }
            if (index < 0 || !item.Equals(items[index]))
            {
                index = items.IndexOf(item);
            }
            return (index, newIndex);
        }

        private void SetItems(List<DBItem> list)
        {
            AddRange(list.Cast<T>());
        }

        #region IBindingListView Members

        public void UpdateFilter()
        {
            if (keys == DBViewKeys.Lock)
                return;

            ClearInternal();
            if (!query.IsEmpty())
            {
                AddRangeInternal(table.Select(query), false);
            }
            else
            {
                AddRangeInternal(table, false);
            }
            SortInternal();
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        private void CheckFilterQuery()
        {
            ClearFilter();
            FilterChanged?.Invoke(this, EventArgs.Empty);

            foreach (var filter in FilterQuery.Parameters)
            {
                if (filter.Invoker == null
                    || filter.Value == null
                    || filter.Value == DBNull.Value
                    || filter.Value.ToString().Length == 0)
                    if (filter.Comparer.Type != CompareTypes.Is)
                        continue;
                var pcolumn = filter.Invoker as DBColumn;

                if (filter.Invoker.Name == nameof(Object.ToString))
                {
                    Query.SimpleFilter(filter.Value as string);
                }
                else if (pcolumn != null)
                {
                    string code = pcolumn.Name;
                    QParam param = new QParam()
                    {
                        Column = pcolumn,
                        Logic = filter.Logic,
                        Comparer = filter.Comparer,
                        Value = filter.Comparer.Type != CompareTypes.Is ? filter.Value : null
                    };
                    if (param.Value is string && param.Comparer.Type == CompareTypes.Like)
                    {
                        string s = (string)param.Value;
                        if (s.IndexOf('%') < 0)
                            param.Value = string.Format("%{0}%", s);
                    }
                    int i = code.IndexOf('.');
                    if (i >= 0)
                    {
                        int s = 0;
                        QQuery sexpression = Query;
                        QQuery newQuery = null;
                        while (i > 0)
                        {
                            string iname = code.Substring(s, i - s);
                            if (s == 0)
                            {
                                var pc = table.Columns[iname];
                                if (pc != null)
                                    iname = pc.Name;
                            }
                            var c = sexpression.Table.Columns[iname];
                            if (c.IsReference)
                            {
                                newQuery = new QQuery(string.Empty, c.ReferenceTable);
                                sexpression.BuildParam(c, CompareType.In, newQuery);
                                sexpression = newQuery;
                            }
                            s = i + 1;
                            i = code.IndexOf('.', s);
                        }
                        newQuery.Parameters.Add(param);
                    }
                    else
                    {
                        Query.Parameters.Add(param);//.BuildParam(col, column.Value, true);
                    }
                }
                else
                {
                    var param = new QParam()
                    {
                        ValueLeft = new QReflection(filter.Invoker),
                        Logic = filter.Logic,
                        Comparer = filter.Comparer,
                        Value = filter.Comparer.Type != CompareTypes.Is ? filter.Value : null
                    };
                    Query.Parameters.Add(param);
                }
            }
        }

        public void Accept(IUserIdentity user)
        {
            var edited = GetEdited().ToList();
            foreach (T item in edited)
            {
                item.Accept(user);
            }
        }

        public void Reject(IUserIdentity user)
        {
            var edited = GetEdited().ToList();
            foreach (T item in edited)
            {
                item.Reject(user);
            }
        }

        public IEnumerable<T> GetEdited()
        {
            for (int i = 0; i < items.Count; i++)
            {
                T view = items[i];
                if (view != null && view.UpdateState != DBUpdateState.Default)
                    yield return view;
            }
        }

        public async Task Save()
        {
            using (var transaction = new DBTransaction(Table.Connection))
            {
                try
                {
                    await Save(transaction);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                    transaction.Rollback();
                }

            }
        }

        public Task Save(DBTransaction transaction)
        {
            return Table.Save(transaction, GetEdited().ToList());
        }

        public void Sort(params DBColumn[] columns)
        {
            items.Sort(new DBComparerList(columns));
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        public void Sort(params string[] columns)
        {
            items.Sort(new DBComparerList(table, columns));
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        public bool ClearFilter()
        {
            var flag = false;
            foreach (var parameter in query.Parameters.ToList())
            {
                if (!parameter.IsDefault)
                {
                    flag = true;
                    query.Parameters.Remove(parameter);
                }
            }
            return flag;
        }

        public void ResetFilter()
        {
            ClearFilter();
            UpdateFilter();
        }

        public override void InsertInternal(int index, T item)
        {
            lock (items)
            {
#if DEBUG
                if (Contains(item))
                {
                }
#endif 
                if (AutoAttach && !item.Attached)
                    table.Add(item);
                base.InsertInternal(index, item);
            }
        }

        #endregion

        public override void Dispose()
        {
            if (table != null)
            {
                table.RemoveView(this);
            }
            base.Dispose();
            Query?.Dispose();
        }

        public IEnumerable<T> GetTop()
        {
            return (IEnumerable<T>)table.SelectItems(Table.GroupKey, CompareType.Is, null);
        }

        public IEnumerable<T> GetItems()
        {
            for (int i = 0; i < items.Count; i++)
                yield return items[i];
        }

        public override string ToString()
        {
            return table == null ? base.ToString() : table.ToString();
        }

        public List<DBGroup<T>> GroupBy(DBColumn column)
        {
            items.Sort((IComparer<T>)column.CreateComparer(ListSortDirection.Ascending));
            var groups = new List<DBGroup<T>>();
            DBGroup<T> group = null;
            foreach (T item in items)
            {
                if (group == null || !group.Value.Equals(item[column]))
                {
                    group = new DBGroup<T> { Value = item[column] };
                    groups.Add(group);
                }
                group.List.Add(item);
            }
            return groups;
        }

        public T GetById(object id)
        {
            return table.GetById(id);
        }
    }
}
