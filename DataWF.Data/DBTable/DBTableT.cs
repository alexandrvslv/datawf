//  The MIT License (MIT)
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Linq.Expressions;

namespace DataWF.Data
{
    [InvokerGenerator(Instance = true)]
    public partial class DBTable<T> : DBTable, IDBTable<T>, IIdCollection<T> where T : DBItem
    {
        protected readonly List<T> items = new List<T>();
        protected readonly List<T> insertItems = new List<T>();
        protected readonly List<IDBTableView> queryViews = new List<IDBTableView>(1);
        private IQQuery filterQuery;

        public DBTable() : this(null)
        { }

        public DBTable(string name) : base(name)
        {
            DefaultComparer = DBItemDefaultComparer<T>.Instance;
            SetItemType(typeof(T));
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public override IQQuery FilterQuery
        {
            get => ParentTable == null ? null : (filterQuery ??= ParentTable.QQuery(subQuery).Where(typeof(T)));
            set => filterQuery = value;
        }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public override bool IsSynch
        {
            get => base.IsSynch || (IsVirtual && (ParentTable?.IsSynch ?? false));
            set => base.IsSynch = value;
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public int Capacity
        {
            get => items.Capacity;
            set => items.Capacity = value;
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public override int Count => items.Count;

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public override IDBTableView DefaultItemsView => DefaultView;

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public DBTableView<T> DefaultView => (queryViews.FirstOrDefault() as DBTableView<T>) ?? CreateTableView();

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public override bool IsEdited
        {
            get => GetChanged().Any();
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public override object SyncRoot => items;

        public IEnumerable<T> Items => items;

        public override DBItem this[int index]
        {
            get => items[index];
        }

        public override bool Contains(DBItem item)
        {
            return Contains((T)item);
        }

        public bool Contains(T item)
        {
            return item.Table == this && item.Attached;
        }

        public void AddParams(params object[] fields)
        {
            var row = NewItem();
            row.SetValues(fields);
            Add(row);
        }

        void ICollection<T>.Add(T item) => Add(item);

        public override bool Add(DBItem item)
        {
            return Add((T)item);
        }

        public virtual bool Add(T item)
        {
            if (IsVirtual)
            {
                if (!item.Attached)
                {
                    return ParentTable.Add(item);
                }
                else
                {
                    items.Add(item);
                    AddIndexes(item);
                    CheckViews(item, NotifyCollectionChangedAction.Add);
                }
            }
            //if (item.Table != this)
            //{
            //    throw new ArgumentException("Wrong Table item!");
            //}
            if (item.Attached)
            {
                return false;
            }
            items.Add(item);
            AddIndexes(item);
            item.OnAttached();
            CheckViews(item, NotifyCollectionChangedAction.Add);
            return true;
        }

        protected void AddIndexes(T item)
        {
            foreach (var column in Columns)
            {
                column.AddIndex<T>(item);
            }
        }

        protected void RemoveIndexes(T item)
        {
            foreach (var column in Columns)
            {
                column.RemoveIndex<T>(item);
            }
        }

        public override bool Remove(DBItem item)
        {
            return Remove((T)item);
        }

        public virtual bool Remove(T item)
        {
            if (IsVirtual)
            {
                if (item.Attached)
                {
                    return ParentTable.Remove(item);
                }
                else
                {
                    if (items.Remove(item))
                    {
                        CheckViews(item, NotifyCollectionChangedAction.Remove);
                        RemoveIndexes(item);
                        return true;
                    }
                    return false;
                }
            }
            if (!item.Attached)
            {
                return false;
            }
            items.Remove(item);
            RemoveIndexes(item);
            item.OnDetached();
            CheckViews(item, NotifyCollectionChangedAction.Remove);
            return true;
        }

        public override void Accept(DBItem item)
        {
            if (IsVirtual)
            {
                if (item is not T tItem || tItem.GetType() != typeof(T))
                {
                    return;
                }
            }
            if (!item.Attached)
            {
                Add(item);
            }
            else
            {
                foreach (var column in Columns)
                {
                    column.PullIndex?.RefreshSort(item);
                }
            }
            foreach (var collection in virtualTables)
            {
                collection.Accept(item);
            }
        }

        protected internal override void OnBaseTableChanged(DBItem item, NotifyCollectionChangedAction type)
        {
            if (item is T view && view.GetType() == typeof(T))
            {
                switch (type)
                {
                    case NotifyCollectionChangedAction.Add:
                        if (FilterQuery.CheckItem(item))
                        {
                            Add(view);
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        Remove(view);
                        break;
                }
            }
            else if (type == NotifyCollectionChangedAction.Reset)
            {
                Clear();
                foreach (T sitem in ParentTable.Select<T>(FilterQuery))
                {
                    Add(sitem);
                }
            }
        }

        public override void RefreshVirtualTable(DBTable value)
        {
            ParentTable = value;
            GroupName = value.GroupName;
            BlockSize = value.BlockSize;
            SequenceName = value.SequenceName;
            Keys = value.Keys | DBTableKeys.Virtual;
            FilterQuery = null;
            ParentTable?.AddVirtualTable(this);
            var type = typeof(T);

            foreach (DBColumnGroup group in ParentTable.ColumnGroups)
            {
                var exist = ColumnGroups[group.Name];
                if (exist == null)
                {
                    ColumnGroups.Add(DBColumnFactory.CreateGroup(group));
                }
                else
                {
                    exist.Order = group.Order;
                }
            }

            foreach (DBColumn column in ParentTable.Columns)
            {
                var exist = Columns[column.Name];
                if (exist == null || exist.DataType != column.DataType)
                {
                    if (!(column.PropertyInvoker?.TargetType.IsAssignableFrom(type) ?? true))
                        continue;
                    Columns.Add(DBColumnFactory.CreateVirtual(column, this));
                }
                else
                {
                    exist.RefreshVirtualColumn(column);
                }
            }

            foreach (DBForeignKey reference in ParentTable.Foreigns)
            {
                var existColumn = Columns[reference.Column.Name];
                if (existColumn == null || reference.Reference == null)
                    continue;
                var exist = Foreigns.GetByColumns(existColumn, reference.Reference);
                if (exist == null)
                {
                    exist = new DBForeignKey()
                    {
                        Column = existColumn,
                        Reference = reference.Reference,
                        Property = reference.Property
                    };
                    exist.GenerateName();
                    Foreigns.Add(exist);
                }
            }

            foreach (DBReferencing reference in ParentTable.Referencings)
            {
                if (!(reference.PropertyInfo?.DeclaringType.IsAssignableFrom(type) ?? true))
                    continue;
                var exist = Referencings[reference.PropertyInfo.Name];
                if (exist == null)
                {
                    exist = new DBReferencing()
                    {
                        Name = reference.PropertyInfo.Name,
                        PropertyInfo = reference.PropertyInfo,
                        ReferenceTable = reference.ReferenceTable,
                        ReferenceColumn = reference.ReferenceColumn,
                        ForceLoadReference = reference.ForceLoadReference
                    };

                    Referencings.Add(exist);
                }
            }

            foreach (DBConstraint constraint in ParentTable.Constraints)
            {
                var existColumn = Columns[constraint.Column.Name];
                if (existColumn == null)
                    continue;
                var exist = Constraints.GetByColumnAndType(existColumn, constraint.Type).FirstOrDefault();
                if (exist == null)
                {
                    exist = new DBConstraint
                    {
                        Column = existColumn,
                        Type = constraint.Type,
                        Value = constraint.Value
                    };
                    exist.GenerateName();
                    Constraints.Add(exist);
                }
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            items.CopyTo(array, arrayIndex);
        }

        public override void CopyTo(DBItem[] array, int arrayIndex)
        {
            items.CopyTo((T[])array, arrayIndex);
        }

        public override void CopyTo(Array array, int arrayIndex)
        {
            items.CopyTo((T[])array, arrayIndex);
        }

        public override void Clear()
        {
            if (IsVirtual)
            {
                items.Clear();
                ClearColumnsData(false);
                CheckViews(null, NotifyCollectionChangedAction.Reset);
                return;
            }
            lock (Lock)
            {
                Hash = -1;
                queryChache.Clear();
                var temp = items.ToArray();
                items.Clear();
                ClearColumnsData(true);
                CheckViews(null, NotifyCollectionChangedAction.Reset);

                foreach (DBItem row in temp)
                {
                    row.Dispose();
                }
                GC.Collect();
            }
            IsSynch = false;
        }

        //public IEnumerable<T> SelectParents() where T : DBGroupItem
        //{
        //    throw new NotImplementedException();
        //}

        public IEnumerable<T> SelectParents()
        {
            return GroupKey.Select<T>(CompareType.Is, (object)null, null);
        }

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        public override IEnumerator<DBItem> GetItemEnumerator()
        {
            return GetEnumerator();
        }

        public OneListEnumerator<T> GetEnumerator()
        {
            return items.Count == 0 ? OneListEnumerator<T>.Empty : new OneListEnumerator<T>(items);
        }

        #endregion

        public override void RemoveView(IDBTableView view)
        {
            queryViews.Remove(view);
        }

        public override void AddView(IDBTableView view)
        {
            queryViews.Add(view);
        }

        protected internal override void OnItemChanging<V>(DBItem item, string property, DBColumn<V> column, V value)
        {
            if (IsVirtual)
            {
                if (item.GetType() != typeof(T))
                {
                    return;
                }
                column = column == null ? null : (DBColumn<V>)Columns[column.Name];
            }
            if (column.PullIndex is IPullInIndex<T, V> pullIndex)
                pullIndex.Remove(item, value);
            foreach (var table in virtualTables)
            {
                if (table.Columns[column.Name] is DBColumn<V> vColumn)
                {
                    table.OnItemChanging<V>(item, property, column, value);
                }
            }
        }

        protected internal override void OnItemChanged<V>(DBItem item, string property, DBColumn<V> column, V value)
        {
            if (string.Equals(property, nameof(DBItem.Attached), StringComparison.Ordinal)
                || string.Equals(property, nameof(DBItem.UpdateState), StringComparison.Ordinal))
            {
                return;
            }

            if (IsVirtual)
            {
                if (item is T tItem && tItem.GetType() == typeof(T))
                {
                    if (FilterQuery.Parameters.Count != 0 && (FilterQuery.Contains(column?.Name) && !FilterQuery.CheckItem(tItem)))
                    {
                        if (items.Remove(tItem))
                        {
                            CheckViews(item, NotifyCollectionChangedAction.Remove);
                            RemoveIndexes(tItem);
                            return;
                        }
                    }
                    column = column == null ? null : (DBColumn<V>)Columns[column.Name];
                }
                else
                {
                    return;
                }
            }

            if (column?.PullIndex is IPullInIndex<T, V> pullIndex)
                pullIndex.Add(item, value);
            foreach (var table in virtualTables)
            {
                table.OnItemChanged<V>(item, property, column, value);
            }
            CheckViews(item, property, column);
        }

        public void CheckViews(DBItem item, string property, DBColumn column)
        {
            for (int i = 0; i < queryViews.Count; i++)
            {
                queryViews[i].OnSourceItemChanged(item, property, column);
            }
        }

        public void CheckViews(DBItem item, NotifyCollectionChangedAction type)
        {
            foreach (var collection in virtualTables)
            {
                collection.OnBaseTableChanged(item, type);
            }
            for (int i = 0; i < queryViews.Count; i++)
            {
                queryViews[i].OnSourceCollectioChanged(item, type);
            }
        }

        public override void Trunc()
        {
            if (items.Count > 0)
            {
                var maxIndex = 0;
                foreach (var item in this)
                    maxIndex = Math.Max(item.handler.GetSeqence(blockSize), maxIndex);
                if (Hash > maxIndex)
                {
                    Hash = maxIndex;
                    foreach (var column in Columns)
                    {
                        column.Pull?.Trunc(Hash);
                    }
                }
            }
        }

        public override IDBTableView CreateView(string query, DBViewKeys mode, DBStatus filter)
        {
            return CreateTableView(query, mode, filter);
        }

        public DBTableView<T> CreateTableView(string query = "", DBViewKeys mode = DBViewKeys.None, DBStatus filter = DBStatus.Empty)
        {
            return new DBTableView<T>(this, query, mode, filter);
        }

        private int GetCount(IDbCommand command, DBTransaction transaction)
        {
            var whereInd = command.CommandText.IndexOf("where ", StringComparison.OrdinalIgnoreCase);
            string w = whereInd == -1 ? string.Empty : command.CommandText.Substring(whereInd);
            var val = transaction.ExecuteQuery(transaction.AddCommand(DBCommand.CloneCommand(command, BuildQuery(w, "a", DBLoadParam.None, "count(*)"))), DBExecuteType.Scalar);
            int count = val is Exception ? -1 : int.Parse(val.ToString());

            if (count < 0 || count == 0)
                return count;
            if (items.Capacity < count)
                items.Capacity = count;
            return count;
        }

        //public override IEnumerable<TT> Load<TT>(string whereText, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null)
        //{
        //    return (IEnumerable<TT>)Load(whereText, param, transaction);
        //}

        //public IEnumerable<T> Load(string whereText, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null)
        //{
        //    if (string.IsNullOrEmpty(whereText) || whereText.Trim().Equals("where", StringComparison.OrdinalIgnoreCase))
        //        whereText = string.Empty;
        //    else if (whereText.Length > 3
        //             && whereText.IndexOf("where", StringComparison.OrdinalIgnoreCase) < 0
        //             && !whereText.Trim().StartsWith("select", StringComparison.OrdinalIgnoreCase))
        //        whereText = "where " + whereText;

        //    return Load(Schema.Connection.CreateCommand(CreateQuery(whereText, "a", param)), param, transaction);
        //}

        //public Task<IEnumerable<T>> LoadAsync(string whereText, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null)
        //{
        //    if (string.IsNullOrEmpty(whereText) || whereText.Trim().Equals("where", StringComparison.OrdinalIgnoreCase))
        //        whereText = string.Empty;
        //    else if (whereText.Length > 3
        //             && whereText.IndexOf("where", StringComparison.OrdinalIgnoreCase) < 0
        //             && !whereText.Trim().StartsWith("select", StringComparison.OrdinalIgnoreCase))
        //        whereText = "where " + whereText;

        //    return LoadAsync(Schema.Connection.CreateCommand(CreateQuery(whereText, "a", param)), param, transaction);
        //}

        public override IEnumerable<TT> Load<TT>(DBLoadParam param = DBLoadParam.Referencing, DBTransaction transaction = null)
        {
            return Load<TT>(Query(param), transaction);
        }

        public IEnumerable<T> Load(DBLoadParam param = DBLoadParam.Referencing, DBTransaction transaction = null)
        {
            return Load(Query(param), transaction);
        }

        public ValueTask<IEnumerable<T>> LoadAsync(DBLoadParam param = DBLoadParam.Referencing, DBTransaction transaction = null)
        {
            return LoadAsync(Query(param), transaction);
        }

        public override IEnumerable<TT> Load<TT>(IQQuery<TT> query, DBTransaction transaction = null)
        {
            return (IEnumerable<TT>)Load(query, transaction);
        }

        public override IEnumerable<TT> Load<TT>(IQQuery query, DBTransaction transaction = null)
        {
            return (IEnumerable<TT>)Load(query, transaction);
        }

        public IEnumerable<T> Load(IQQuery query, DBTransaction transaction = null)
        {
            if (query.Table != this)
                throw new ArgumentException(nameof(query));

            CheckCacheState(query);

            if (!IsSynch
                && query.CacheState == DBCacheState.None)
            {
                query.CacheState = DBCacheState.Actualazing;
                if (Count == 0)
                {
                    query.LoadParam &= ~DBLoadParam.CheckDeleted;
                }
                if ((query.LoadParam & DBLoadParam.Referencing) == DBLoadParam.Referencing
                    && query.Tables.Count > 1)
                {
                    query.JoinAllReferencing();
                    query.LoadParam &= ~DBLoadParam.Referencing;
                }
                var buf = Load(query.ToCommand(true), query, query.LoadParam, transaction);

                if (buf != null && (query.LoadParam & DBLoadParam.CheckDeleted) == DBLoadParam.CheckDeleted)
                {
                    CheckDelete(query, buf, transaction);
                }
                if (query.Parameters.Count == 0)
                {
                    IsSynch = true;
                }
                query.CacheState = DBCacheState.Actual;
            }
            return Select(query);
        }

        public async ValueTask<IEnumerable<T>> LoadAsync(IQQuery query, DBTransaction transaction = null)
        {
            if (query.Table != this)
                throw new ArgumentException(nameof(query));

            CheckCacheState(query);
            if (!IsSynch
                && query.CacheState == DBCacheState.None)
            {
                query.CacheState = DBCacheState.Actualazing;
                if (Count == 0)
                {
                    query.LoadParam &= ~DBLoadParam.CheckDeleted;
                }
                if ((query.LoadParam & DBLoadParam.Referencing) == DBLoadParam.Referencing
                    && query.Tables.Count > 1)
                {
                    query.JoinAllReferencing();
                    query.LoadParam &= ~DBLoadParam.Referencing;
                }
                var buf = await LoadAsync(query.ToCommand(true), query, query.LoadParam, transaction);

                if (buf != null && (query.LoadParam & DBLoadParam.CheckDeleted) == DBLoadParam.CheckDeleted)
                {
                    CheckDelete(query, buf, transaction);
                }
                if (query.Parameters.Count == 0)
                {
                    IsSynch = true;
                }
                query.CacheState = DBCacheState.Actual;
            }
            return Select(query);
        }

        private void CheckCacheState(IQQuery query)
        {
            if ((query.LoadParam & DBLoadParam.NoCache) != DBLoadParam.NoCache)
            {
                if (queryChache.TryGetValue(query.WhereText, out var cacheQuery))
                {
                    query.CacheState = cacheQuery.CacheState;
                }
                else
                {
                    queryChache[query.WhereText] = query;
                }
            }
        }

        public override IEnumerable<TT> Load<TT>(IDbCommand command, IQQuery query, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null)
        {
            return (IEnumerable<TT>)Load(command, query, param, transaction);
        }

        public override async Task<IEnumerable<TT>> LoadAsync<TT>(IDbCommand command, IQQuery query, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null)
        {
            return (IEnumerable<TT>)await LoadAsync(command, query, param, transaction);
        }

        public IEnumerable<T> Load(IDbCommand command, IQQuery query, DBLoadParam param = DBLoadParam.None, DBTransaction baseTransaction = null)
        {
            var list = new List<T>();
            var transaction = baseTransaction ?? new DBTransaction(this, null, true);
            int count = -1;
            try
            {
                transaction.AddCommand(command);

                transaction.ClearView(this);

                if ((param & DBLoadParam.GetCount) == DBLoadParam.GetCount)
                {
                    count = GetCount(command, transaction);

                    if (count == 0 || transaction.State != DBTransactionState.Default)
                        return list;
                }

                if ((param & DBLoadParam.Reference) == DBLoadParam.Reference)
                {
                    LoadReferenceBlock(command, transaction);
                }

                if ((param & DBLoadParam.Referencing) == DBLoadParam.Referencing)
                {
                    LoadReferencingBlock(command, transaction);
                }

                if (transaction.State != DBTransactionState.Default)
                {
                    return list;
                }

                using (var reader = transaction.ExecuteReader(command, CommandBehavior.SequentialAccess))
                {
                    var dbReader = new DBReader(transaction, this, query, reader, param, count);

                    while (transaction.State == DBTransactionState.Default
                        && reader.Read())
                    {
                        list.Add((T)dbReader.Load());
                    }
                    reader.Close();
                }

                //Check IsSynchronized
                if (transaction.View?.Table == this)
                {
                    transaction.View.IsSynchronized = true;
                }
            }
            finally
            {
                transaction.CurrentReader = null;
                if (baseTransaction == null)
                {
                    transaction.Dispose();
                }
                RaiseLoadCompleate(transaction);
            }
            return list;
        }

        public async Task<IEnumerable<T>> LoadAsync(IDbCommand command, IQQuery query, DBLoadParam param = DBLoadParam.None, DBTransaction baseTransaction = null)
        {
            var list = new List<T>();
            var transaction = baseTransaction ?? new DBTransaction(this, null, true);
            int count = 0;
            try
            {
                transaction.AddCommand(command);

                transaction.ClearView(this);

                if ((param & DBLoadParam.GetCount) == DBLoadParam.GetCount)
                {
                    count = GetCount(command, transaction);
                    if (count == 0 || transaction.State != DBTransactionState.Default)
                        return list;
                }

                if ((param & DBLoadParam.Reference) == DBLoadParam.Reference)
                {
                    LoadReferenceBlock(command, transaction);
                }

                if ((param & DBLoadParam.Referencing) == DBLoadParam.Referencing)
                {
                    LoadReferencingBlock(command, transaction);
                }

                if (transaction.State != DBTransactionState.Default)
                {
                    return list;
                }

                using (var reader = (DbDataReader)await transaction.ExecuteQueryAsync(command, DBExecuteType.Reader, CommandBehavior.SequentialAccess))
                {
                    var dbReader = new DBReader(transaction, this, query, reader, param, count);

                    while (transaction.State == DBTransactionState.Default
                        && await reader.ReadAsync())
                    {
                        list.Add((T)dbReader.Load());
                    }
                    reader.Close();
                }

                //Check IsSynchronized
                if (transaction.View?.Table == this)
                {
                    transaction.View.IsSynchronized = true;
                }
            }
            finally
            {
                transaction.CurrentReader = null;
                if (baseTransaction == null)
                {
                    transaction.Dispose();
                }
                RaiseLoadCompleate(transaction);
            }
            return list;
        }

        public override void Reload(object id, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null)
        {
            LoadItem(id, param);
        }

        public T LoadItem(object id, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null)
        {
            return LoadItem(id, PrimaryKey, param, transaction);
        }

        public T LoadItem(object id, DBColumn column, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null)
        {
            return Load(CreateKeyCommmand(id, column, param), null, param, transaction).FirstOrDefault();
        }

        public T LoadItem<K>(K id, DBColumn<K> column, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null)
        {
            return Load(CreateKeyCommmand(id, column, param), null, param, transaction).FirstOrDefault();
        }

        public async Task<T> LoadItemAsync<K>(K id, DBColumn<K> column, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null)
        {
            return (await LoadAsync(CreateKeyCommmand(id, column, param), null, param, transaction)).FirstOrDefault();
        }

        public async Task<T> LoadItemAsync(object id, DBColumn column, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null)
        {
            return (await LoadAsync(CreateKeyCommmand(id, column, param), null, param, transaction)).FirstOrDefault();
        }

        public override TT LoadById<TT>(object id, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null)
        {
            return LoadById(id, param, transaction) as TT;
        }

        public T LoadById(object id, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null)
        {
            return LoadByKey(id, PrimaryKey, param, transaction).FirstOrDefault();
        }

        public override TT LoadById<TT, K>(K? id, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null)
        {
            return LoadById(id, param, transaction) as TT;
        }

        public T LoadById<K>(K? id, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null) where K : struct
        {
            if (id == null)
                return null;
            if (PrimaryKey is DBColumn<K> typedColumn)
                return LoadByKey(id.Value, typedColumn, param, transaction).FirstOrDefault();
            else if (PrimaryKey is DBColumn<K?> typedNColumn)
                return LoadByKey(id, typedNColumn, param, transaction).FirstOrDefault();
            return LoadByKey((object)id, PrimaryKey, param, transaction).FirstOrDefault();
        }

        public async ValueTask<T> LoadByIdAsync(object id, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null)
        {
            return (await LoadByKeyAsync(id, PrimaryKey, param, transaction)).FirstOrDefault();
        }

        public async ValueTask<T> LoadByIdAsync<K>(K? id, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null) where K : struct
        {
            if (id == null)
                return null;
            if (PrimaryKey is DBColumn<K> typedColumn)
                return (await LoadByKeyAsync(id.Value, typedColumn, param, transaction)).FirstOrDefault();
            else if (PrimaryKey is DBColumn<K?> typedNColumn)
                return (await LoadByKeyAsync(id, typedNColumn, param, transaction)).FirstOrDefault();
            return (await LoadByKeyAsync((object)id, PrimaryKey, param, transaction)).FirstOrDefault();
        }

        public T GetById(object id)
        {
            return LoadByKey(id, PrimaryKey).FirstOrDefault();
        }

        public override IEnumerable<R> LoadByKey<R, K>(K key, DBColumn<K> column, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null)
        {
            return LoadByKey(key, column, param, transaction).TypeOf<R>();
        }

        public IEnumerable<T> LoadByKey<K>(K key, DBColumn<K> column, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null)
        {
            var items = column.Select<T>(CompareType.Equal, key);

            if (!items.Any() && (param & DBLoadParam.Load) == DBLoadParam.Load)
            {
                items = Load(CreateKeyCommmand(key, column, param), null, param, transaction);
            }
            return items;
        }

        public override IEnumerable<R> LoadByKey<R>(object key, DBColumn column, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null)
        {
            return LoadByKey(key, column, param, transaction).TypeOf<R>();
        }

        public IEnumerable<T> LoadByKey(object key, DBColumn column, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null)
        {
            object val = column?.ParseValue(key);

            if (val == null || column == null)
                return null;

            var items = column.Select<T>(CompareType.Equal, val);

            if (!items.Any() && (param & DBLoadParam.Load) == DBLoadParam.Load)
            {
                items = Load(CreateKeyCommmand(val, column, param), null, param, transaction);
            }
            return items;
        }

        public async ValueTask<IEnumerable<T>> LoadByKeyAsync<K>(K key, DBColumn<K> column, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null)
        {
            var items = column.Select<T>(CompareType.Equal, key);

            if (!items.Any() && (param & DBLoadParam.Load) == DBLoadParam.Load)
            {
                items = await LoadAsync(CreateKeyCommmand(key, column, param), null, param, transaction);
            }
            return items;
        }

        public async ValueTask<IEnumerable<T>> LoadByKeyAsync(object key, DBColumn column, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null)
        {
            object val = column?.ParseValue(key);

            if (val == null || column == null)
                return null;

            var items = column.Select<T>(CompareType.Equal, key);

            if (!items.Any() && (param & DBLoadParam.Load) == DBLoadParam.Load)
            {
                items = await LoadAsync(CreateKeyCommmand(val, column, param), null, param, transaction);
            }
            return items;
        }

        public T LoadByCode(string code, DBLoadParam param = DBLoadParam.None)
        {
            if (CodeKey == null)
                return null;
            return LoadByCode(code, CodeKey, param);
        }

        public T LoadByCode(string code, string column, DBLoadParam param = DBLoadParam.None)
        {
            return LoadByCode(code, (DBColumn<string>)GetColumn(column), param);
        }

        public override TT LoadByCode<TT>(string code, DBColumn<string> column, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null)
        {
            return LoadByCode(code, column, param) as TT;
        }

        public T LoadByCode(string code, DBColumn<string> column, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null)
        {
            var row = column.FirstOrDefault<T>(code, this);
            if (row == null && (param & DBLoadParam.Load) == DBLoadParam.Load)//&& !IsSynchronized
            {
                var command = System.CreateCommand(Schema.Connection, CreateQuery($"where a.{column.SqlName}={Schema.System.ParameterPrefix}{column.SqlName}", "a", param));
                System.CreateParameter(command, Schema.System.ParameterPrefix + column.SqlName, code, column);
                row = Load(command, null, param, transaction).FirstOrDefault();
            }
            return row;
        }

        private void CheckDelete(IQQuery query, IEnumerable<T> buf, DBTransaction transaction)
        {
            var param = query.LoadParam;
            var list = Select(query).ToList();
            var bufList = buf.ToList();
            if (list.Count > bufList.Count)
            {
                foreach (var item in list)
                {
                    if ((item.UpdateState & DBUpdateState.Insert) != DBUpdateState.Insert && !bufList.Contains(item))
                    {
                        if (transaction.View != null && transaction.View.IsStatic)
                            transaction.View.Remove(item);
                        Remove(item);
                    }
                }
            }
        }

        private IEnumerable<DBTuple> AsTuple(IEnumerable<T> items = null)
        {
            if (items == null || items == this || items == this.items)
            {
                for (int i = 0; i < this.items.Count; i++)
                {
                    yield return new DBTuple { Item0 = this.items[i] };
                }
            }
            else
            {
                foreach (var item in items)
                {
                    yield return new DBTuple { Item0 = item };
                }
            }
        }

        private IEnumerable<T> AsReadOnly()
        {
            for (int i = 0; i < items.Count; i++)
            {
                yield return items[i];
            }
        }

        public override IEnumerable<DBItem> GetChangedItems()
        {
            return GetChanged();
        }

        public IEnumerable<T> GetChanged()
        {
            foreach (var item in this.items)
            {
                if (item.IsChanged)
                    yield return item;
            }
        }

        public IEnumerable<DBTuple> Join(IQQuery query, IEnumerable<DBTuple> list)
        {
            var joins = query.Tables.Skip(1)
                                    .Where(table => !table.Join.IsLeft
                                                || table.HasParameters)
                                    .ToList();

            var count = joins.Count;
            foreach (var tuple in list)
            {
                var tuples = Enumerable.Repeat(tuple, 1);
                foreach (var table in joins)
                {
                    tuples = Join(table, tuples);
                }
                foreach (var joinedTuple in tuples)
                {
                    yield return joinedTuple;
                }
            }

            int Compare(QTable x, QTable y) // TODO if order of join is not inclusive
            {
                var raiting = ((QColumn)x.On.LeftItem).Column.Table == this;
                return x.Order.CompareTo(y.Order);
            }
        }

        public IEnumerable<DBTuple> Join(QTable rtable, IEnumerable<DBTuple> list)
        {
            foreach (var tuple in list)
            {
                foreach (var joined in Join(rtable, tuple))
                {
                    yield return joined;
                }
            }
        }

        public IEnumerable<DBTuple> Join(QTable rtable, DBTuple tuple)
        {
            //if(!rtable.On.IsCompaund)
            {
                var rcolumn = (QColumn)(rtable.On.LeftItem.QTable == rtable ? rtable.On.LeftItem : rtable.On.RightItem);
                var lcolumn = (QColumn)(rtable.On.LeftItem != rcolumn ? rtable.On.LeftItem : rtable.On.RightItem);

                //if(rtable.On.Comparer == CompareType.Equal)
                {
                    var litem = tuple.Get(lcolumn.QTable);
                    if (litem == null)
                    {
                        yield break;
                    }
                    var items = lcolumn.Column.IsPrimaryKey
                        ? rcolumn.Column.Select<DBItem>(rtable.On.Comparer, litem)
                        : rcolumn.Column.Select<DBItem>(rtable.On.Comparer, lcolumn.GetValue(litem));

                    bool first = true;
                    foreach (var item in items)
                    {
                        //if (QParam.CheckItem(item, rtable.GetParameters()))
                        {
                            var itemTurple = first ? tuple : tuple.Clone();
                            itemTurple.Set(rtable, item);
                            yield return itemTurple;
                            first = false;
                        }
                    }
                    if (first && rtable.Join.IsLeft)// && !rtable.HasParameters)
                    {
                        yield return tuple;
                    }
                }
            }
        }

        public IEnumerable<T> Select(DBColumn column, CompareType comparer, object value)
        {
            return column.Select<T>(comparer, value);
        }

        public IEnumerable<T> Select<V>(DBColumn<V> column, CompareType comparer, V value)
        {
            return column.Select<T>(comparer, value);
        }

        public override IEnumerable<TT> Select<TT>(IQQuery query)
        {
            return Select(query).Cast<TT>();
        }

        public override IEnumerable<TT> Select<TT>(IQQuery<TT> query)
        {
            return Select(query).Cast<TT>();
        }

        public IEnumerable<T> Select(IQQuery query, IEnumerable<T> list = null)
        {
            IEnumerable<T> buf = null;
            if (query.IsJoinAffectResult)
            {
                var joinSource = Join(query, AsTuple(list));
                buf = Select(query, joinSource);
            }
            else if (query.Parameters.Count == 0)
            {
                buf = list ?? AsReadOnly();
            }
            else if (query.Parameters.Count == 1)
            {
                buf = Select(query.Parameters[0], list);
            }
            else
            {
                buf = Select(query.Parameters, list);
            }
            var comparers = query.GetComparer<T>();
            if (query.TreeComparer is ITreeComparer treeComparer)
            {
                treeComparer.Comparer = comparers;
                var bufList = buf.ToList();
                buf = bufList;
                ListHelper.QuickSort<T>(bufList, (IComparer<T>)treeComparer);
            }
            else if (comparers != null)
            {
                var bufList = buf.ToList();
                buf = bufList;
                ListHelper.QuickSort<T>(bufList, comparers);
            }

            return buf;
        }

        private IEnumerable<T> Select(IQQuery query, IEnumerable<DBTuple> joinSource)
        {
            IEnumerable<DBTuple> buf = null;
            if (query.Parameters.Count == 0)
            {
                buf = joinSource;
            }
            else if (query.Parameters.Count == 1)
            {
                buf = Select(query.Parameters[0], joinSource);
            }
            else
            {
                buf = Select(query.Parameters, joinSource);
            }

            return buf.Select(p => p.Item0 as T).Distinct();
        }

        private IEnumerable<T> Select(IEnumerable<QParam> parameters, IEnumerable<T> list = null)
        {
            IEnumerable<T> buffer = null;
            foreach (QParam param in parameters)
            {
                if (buffer != null && param.Logic.Type == LogicTypes.And)
                {
                    //if (!buffer.Any())
                    //    break;
                    list = buffer;
                }
                var temp = Select(param, list);

                if (buffer == null)
                    buffer = temp;
                else if (param.Logic.Type == LogicTypes.Undefined)
                    buffer = buffer.Concat(temp);
                else if (param.Logic.Type == LogicTypes.Or)
                    buffer = param.Logic.Not
                               ? buffer.Except(temp)
                               : buffer.Union(temp);
                else if (param.Logic.Type == LogicTypes.And)
                    buffer = param.Logic.Not
                               ? buffer.Except(temp).Union(temp.Except(buffer))
                               : buffer.Intersect(temp);
            }

            return buffer;
        }

        private IEnumerable<DBTuple> Select(IEnumerable<QParam> parameters, IEnumerable<DBTuple> joinSourc)
        {
            IEnumerable<DBTuple> buffer = null;
            foreach (QParam param in parameters)
            {
                if (buffer != null && param.Logic.Type == LogicTypes.And)
                {
                    //if (!buffer.Any())
                    //    break;
                    joinSourc = buffer;
                }
                var temp = Select(param, joinSourc);

                if (buffer == null)
                    buffer = temp;
                else if (param.Logic.Type == LogicTypes.Undefined)
                    buffer = buffer.Concat(temp);
                else if (param.Logic.Type == LogicTypes.Or)
                    buffer = param.Logic.Not
                               ? buffer.Except(temp)
                               : buffer.Union(temp);
                else if (param.Logic.Type == LogicTypes.And)
                    buffer = param.Logic.Not
                               ? buffer.Except(temp).Union(temp.Except(buffer))
                               : buffer.Intersect(temp);
            }

            return buffer;
        }

        private IEnumerable<T> Select(QParam param, IEnumerable<T> list = null)
        {
            if (param.LeftItem is QFunction func
                && func.Type == QFunctionType.distinct
                && Enumerable.FirstOrDefault(func.Items) is QItem item)
            {
                return ListHelper.Distinct(list ?? items, item, param.Query?.GetComparer<T>());//
            }
            if (param.IsCompaund)
            {
                return Select(param.Parameters.OfType<QParam>(), list);
            }
            if (param.LeftItem is QColumn lqColumn
                && param.LeftItem.IsReference)
            {
                var lColumn = lqColumn.Column;
                if (param.RightItem is QColumn rqColumn)
                {
                    if (rqColumn.Value != null)
                        return lColumn.Select(param.Comparer, rqColumn.Value, list);

                    return lColumn.Search(param.Comparer, rqColumn.Column, list ?? this);
                }
                if (!param.RightItem.IsReference)
                    return lColumn.Select(param.Comparer, param.RightItem.GetValue<T>(), list);
            }
            else if (param.RightItem is QColumn rqColumn
                && !param.LeftItem.IsReference)
            {
                var rColumn = rqColumn.Column;
                return rColumn.Select(param.Comparer, param.LeftItem.GetValue<T>(), list);
            }
            return param.Search<T>(list ?? this);
        }

        private IEnumerable<DBTuple> Select(QParam param, IEnumerable<DBTuple> list)
        {
            if (param.LeftItem is QFunction func
               && func.Type == QFunctionType.distinct
               && Enumerable.FirstOrDefault(func.Items) is QItem item)
            {
                return ListHelper.Distinct(list, item, param.Query?.GetComparer<T>());//
            }
            if (param.IsCompaund)
            {
                return Select(param.Parameters.OfType<QParam>(), list);
            }
            if (param.LeftItem is QColumn lqColumn
                && param.LeftItem.IsReference)
            {
                var lColumn = lqColumn.Column;
                if (param.RightItem is QColumn rqColumn)
                {
                    if (rqColumn.Value != null)
                        return lColumn.Select<DBItem>(param.Comparer, rqColumn.Value, lqColumn.QTable, list);

                    return lColumn.Search<DBItem>(param.Comparer, rqColumn, lqColumn.QTable, list);
                }
                if (!param.RightItem.IsReference)
                    return lColumn.Select<DBItem>(param.Comparer, param.RightItem.GetValue<T>(), lqColumn.QTable, list);
            }
            else if (param.RightItem is QColumn rqColumn
                && !param.LeftItem.IsReference)
            {
                var rColumn = rqColumn.Column;
                return rColumn.Select<DBItem>(param.Comparer, param.LeftItem.GetValue<T>(), rqColumn.QTable, list);
            }
            return param.Search<DBItem>(list);
        }

        public override IQQuery QQuery(DBLoadParam loadParam = DBLoadParam.None)
        {
            return Query(loadParam);
        }

        public override IQQuery QQuery(IQQuery baseQuery)
        {
            return Query(baseQuery);
        }

        public override IQQuery QQuery(string filter, DBLoadParam loadParam = DBLoadParam.None)
        {
            return Query(filter, loadParam);
        }

        public QQuery<T> Query(IQQuery query) => new QQuery<T>(query, string.Empty)
        {
            Table = this
        };

        public QQuery<T> Query(DBLoadParam loadParam = DBLoadParam.Load) => new QQuery<T>(this)
        {
            LoadParam = loadParam
        };

        public QQuery<T> Query(Expression<Func<T, bool>> expression, DBLoadParam loadParam = DBLoadParam.Load) => new QQuery<T>(this, expression)
        {
            LoadParam = loadParam
        };

        public QQuery<T> Query(string filter, DBLoadParam loadParam = DBLoadParam.Load)
        {
            if (!queryChache.TryGetValue(filter, out var query))
            {
                query = new QQuery<T>(this, filter) { LoadParam = loadParam };
                queryChache.TryAdd(filter, query);
            }
            return (QQuery<T>)query;
        }

        public override void Dispose()
        {
            if (IsVirtual)
            {
                ParentTable?.RemoveVirtual(this);
                filterQuery = null;
            }
            base.Dispose();
            Clear();
            queryViews.Clear();
            queryViews.TrimExcess();
        }

        public override DBItem NewItem(DBUpdateState state, bool def, int typeIndex)
        {
            if (IsVirtual)
            {
                if (typeIndex == 0)
                    typeIndex = GetTypeIndex(typeof(T));
                return ParentTable.NewItem(state, def, typeIndex);
            }

            return base.NewItem(state, def, typeIndex);
        }

        public override DBItem NewItem(DBUpdateState state = DBUpdateState.Insert, bool def = true)
        {
            var item = (T)FormatterServices.GetUninitializedObject(typeof(T));
            item.Build(this, def, TypeId);
            item.update = state;
            return item;
        }

        public override void SaveFile(string fileName)
        {
            if (Count == 0)
                return;

            if (File.Exists(fileName))
                File.Delete(fileName);

            string directory = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.Create(fileName).Close();

            using (var file = File.Open(fileName, FileMode.Open, FileAccess.Write))
            {
                using (var writer = new BinaryWriter(file))
                using (var invokerWriter = new BinaryInvokerWriter(writer))
                {
                    invokerWriter.WriteArrayBegin();
                    invokerWriter.WriteArrayLength(Count);
                    var itemSerializer = new DBItemSerializer<T>(this);
                    var map = itemSerializer.WriteMap(invokerWriter, ItemType);
                    foreach (var item in this)
                    {
                        invokerWriter.WriteArrayEntry();
                        itemSerializer.Write(invokerWriter, item, null, map);
                    }
                    invokerWriter.WriteArrayEnd();
                }
            }
        }

        public override void LoadFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName))
                return;
            using (var file = File.Open(fileName, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new BinaryReader(file))
                using (var invokerReader = new BinaryInvokerReader(reader))
                {
                    var itemSerializer = new DBItemSerializer<T>(this);
                    invokerReader.ReadToken();
                    if (invokerReader.CurrentToken == BinaryToken.ArrayBegin)
                    {
                        invokerReader.ReadToken();
                    }
                    if (invokerReader.CurrentToken == BinaryToken.ArrayLength)
                    {
                        var count = Int32Serializer.Instance.Read(invokerReader.Reader);
                        invokerReader.ReadToken();
                    }
                    var type = ItemType;
                    var map = (Dictionary<ushort, IPropertySerializeInfo>)null;
                    if (invokerReader.CurrentToken == BinaryToken.SchemaBegin)
                    {
                        map = itemSerializer.ReadMap(invokerReader, out type);
                        invokerReader.ReadToken();
                    }

                    while (invokerReader.CurrentToken == BinaryToken.ArrayEntry)
                    {
                        var item = (T)NewItem(DBUpdateState.Default, false, type);
                        itemSerializer.Read(invokerReader, item, null, map);
                        Add(item);
                        item.Accept((IUserIdentity)null);
                    }
                }
            }
        }
    }

    public enum DBCacheState
    {
        None,
        Actual,
        Actualazing
    }
}
