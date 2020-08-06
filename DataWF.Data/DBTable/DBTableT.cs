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
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBTable<T> : DBTable, IIdCollection<T> where T : DBItem, new()
    {
        protected readonly List<T> items = new List<T>();
        protected readonly List<T> insertItems = new List<T>();
        protected readonly List<IDBTableView> queryViews = new List<IDBTableView>(1);

        public DBTable()
        {
            SetItemType(typeof(T));
        }

        public DBTable(string name) : base(name)
        {
            SetItemType(typeof(T));
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public int Capacity
        {
            get { return items.Capacity; }
            set { items.Capacity = value; }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public override int Count
        {
            get { return items.Count; }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public override IDBTableView DefaultItemsView
        {
            get { return DefaultView; }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public DBTableView<T> DefaultView
        {
            get
            {
                if (queryViews.Count == 0)
                    CreateView();
                return queryViews.Count > 0 ? (DBTableView<T>)queryViews[0] : null;
            }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public override bool IsEdited
        {
            get { return GetChanged().Any(); }
        }

        public override DBItem this[int index]
        {
            get { return items[index]; }
        }

        public override bool Contains(DBItem item)
        {
            return Contains((T)item);
        }

        public IEnumerable<T1> Select<T1>(QQuery query) where T1 : T
        {
            return Select(query).Cast<T1>();
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

        public override void Add(DBItem item)
        {
            Add((T)item);
        }

        public virtual void Add(T item)
        {
            if (item.Table != this)
            {
                throw new ArgumentException("Wrong Table item!");
            }
            if (item.Attached)
            {
                return;
            }
            items.Add(item);
            AddIndexes(item);
            item.OnAttached();
            CheckViews(item, NotifyCollectionChangedAction.Add);
        }

        protected void AddIndexes(T item)
        {
            foreach (var column in Columns.Where(p => p.Index != null))
            {
                column.Index.Add(item);
            }
        }

        public override bool Remove(DBItem item)
        {
            return Remove((T)item);
        }

        public virtual bool Remove(T item)
        {
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

        protected void RemoveIndexes(T item)
        {
            foreach (var column in Columns.Where(p => p.Index != null))
            {
                column.Index.Remove(item);
            }
        }

        public override void Accept(DBItem item)
        {
            if (!item.Attached)
            {
                Add(item);
            }
            else
            {
                foreach (var column in Columns.Where(p => p.Index != null))
                {
                    column.Index.RefreshSort(item);
                }
            }
            foreach (var collection in virtualTables)
            {
                collection.Accept(item);
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

        public override void Clear()
        {
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
            IsSynchronized = false;
        }

        //public IEnumerable<T> SelectParents() where T : DBGroupItem
        //{
        //    throw new NotImplementedException();
        //}

        public IEnumerable<T> SelectParents()
        {
            return Select(GroupKey, CompareType.Is, null);
        }

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.Count == 0 ? (IEnumerator)EmptyEnumerator<T>.Default : new ThreadSafeEnumerator<T>(items);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return items.Count == 0 ? (IEnumerator<T>)EmptyEnumerator<T>.Default : new ThreadSafeEnumerator<T>(items);
        }

        public override IEnumerator<DBItem> GetEnumerator()
        {
            return items.Count == 0 ? (IEnumerator<T>)EmptyEnumerator<T>.Default : new ThreadSafeEnumerator<T>(items);
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

        public override void OnItemChanging<V>(DBItem item, string property, DBColumn column, V value)
        {
            column?.Index?.Remove<T, V>((T)item, value);
            foreach (var table in virtualTables)
            {
                table.OnItemChanging<V>(item, property, column, value);
            }
        }

        public override void OnItemChanging(DBItem item, string property, DBColumn column, object value)
        {
            column?.Index?.Remove((object)item, value);
            foreach (var table in virtualTables)
            {
                table.OnItemChanging(item, property, column, value);
            }
        }

        public override void OnItemChanged<V>(DBItem item, string property, DBColumn column, V value)
        {
            if (string.Equals(property, nameof(DBItem.Attached), StringComparison.Ordinal)
                || string.Equals(property, nameof(DBItem.UpdateState), StringComparison.Ordinal))
            {
                return;
            }

            column?.Index?.Add<T, V>((T)item, value);
            foreach (var table in virtualTables)
            {
                table.OnItemChanged<V>(item, property, column, value);
            }
            CheckViews(item, property, column);
        }

        public override void OnItemChanged(DBItem item, string property, DBColumn column, object value)
        {
            if (string.Equals(property, nameof(DBItem.Attached), StringComparison.Ordinal)
                || string.Equals(property, nameof(DBItem.UpdateState), StringComparison.Ordinal))
            {
                return;
            }

            column?.Index?.Add((object)item, value);
            foreach (var table in virtualTables)
            {
                table.OnItemChanged(item, property, column, value);
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
                collection.OnTableChanged(item, type);
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
                    maxIndex = item.handler > maxIndex ? item.handler : maxIndex;
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

        public override IDBTableView CreateItemsView(string query, DBViewKeys mode, DBStatus filter)
        {
            return CreateView(query, mode, filter);
        }

        public DBTableView<T> CreateView(string query = "", DBViewKeys mode = DBViewKeys.None, DBStatus filter = DBStatus.Empty)
        {
            return new DBTableView<T>(this, query, mode, filter);
        }

        public IEnumerable<T> LoadCache(string filter, DBLoadParam loadParam = DBLoadParam.Referencing, DBTransaction transaction = null)
        {
            if (!queryChache.TryGetValue(filter, out var query))
            {
                query = new QQuery(filter, this);
                Load(query, loadParam, transaction);
                queryChache.TryAdd(filter, query);
            }
            if (TypeHelper.IsInterface(typeof(T), typeof(IGroup)))
            {
                var temp = Select(query).ToList();
                ListHelper.QuickSort(temp, TreeComparer<IGroup>.Default);
                return temp;
            }
            else
            {
                return Select(query);
            }
        }

        public async ValueTask<IEnumerable<T>> LoadCacheAsync(string filter, DBLoadParam loadParam = DBLoadParam.Referencing, DBTransaction transaction = null)
        {
            if (!ParseQuery(filter, out var query))
            {
                await LoadAsync(query, loadParam, transaction);
            }
            var result = Select(query);
            if (TypeHelper.IsInterface(typeof(T), typeof(IGroup)))
            {
                var temp = result.ToList();
                ListHelper.QuickSort(temp, TreeComparer<IGroup>.Default);
                return temp;
            }
            else
            {
                return result;
            }
        }

        public override IEnumerable<DBItem> LoadItems(string whereText = null, DBLoadParam param = DBLoadParam.None, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null)
        {
            return Load(whereText, param, cols, transaction);
        }

        public IEnumerable<T> Load(string whereText = null, DBLoadParam param = DBLoadParam.None, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null)
        {
            if (string.IsNullOrEmpty(whereText) || whereText.Trim().Equals("where", StringComparison.OrdinalIgnoreCase))
                whereText = string.Empty;
            else if (whereText.Length > 3
                     && whereText.IndexOf("where", StringComparison.OrdinalIgnoreCase) < 0
                     && !whereText.Trim().StartsWith("select", StringComparison.OrdinalIgnoreCase))
                whereText = "where " + whereText;

            return Load(Schema.Connection.CreateCommand(CreateQuery(whereText, "a", cols)), param, transaction);
        }

        public Task<IEnumerable<T>> LoadAsync(string whereText = null, DBLoadParam param = DBLoadParam.None, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null)
        {
            if (string.IsNullOrEmpty(whereText) || whereText.Trim().Equals("where", StringComparison.OrdinalIgnoreCase))
                whereText = string.Empty;
            else if (whereText.Length > 3
                     && whereText.IndexOf("where", StringComparison.OrdinalIgnoreCase) < 0
                     && !whereText.Trim().StartsWith("select", StringComparison.OrdinalIgnoreCase))
                whereText = "where " + whereText;

            return LoadAsync(Schema.Connection.CreateCommand(CreateQuery(whereText, "a", cols)), param, transaction);
        }

        public override IEnumerable<DBItem> LoadItems(QQuery query, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null)
        {
            return Load(query, param, transaction);
        }

        public IEnumerable<T> Load(QQuery query, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null)
        {
            if (query.Table != this)
                throw new ArgumentException(nameof(query));
            if (Count == 0)
            {
                param &= ~DBLoadParam.CheckDeleted;
            }
            var buf = Load(query.ToCommand(true), param, transaction);

            if (buf != null && (param & DBLoadParam.CheckDeleted) == DBLoadParam.CheckDeleted)
            {
                CheckDelete(query, buf, param, transaction);
            }
            if (query.Parameters.Count == 0)
            {
                IsSynchronized = true;
            }

            return buf;
        }

        public async Task<IEnumerable<T>> LoadAsync(QQuery query, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null)
        {
            if (query.Table != this)
                throw new ArgumentException(nameof(query));
            if (Count == 0)
            {
                param &= ~DBLoadParam.CheckDeleted;
            }
            var buf = await LoadAsync(query.ToCommand(true), param, transaction);

            if (buf != null && (param & DBLoadParam.CheckDeleted) == DBLoadParam.CheckDeleted)
            {
                CheckDelete(query, buf, param, transaction);
            }
            if (query.Parameters.Count == 0)
            {
                IsSynchronized = true;
            }
            return buf;
        }

        public override IEnumerable<DBItem> LoadItems(IDbCommand command, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null)
        {
            return Load(command, param, transaction);
        }

        public override async Task<IEnumerable<DBItem>> LoadItemsAsync(IDbCommand command, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null)
        {
            return await LoadAsync(command, param, transaction);
        }

        public List<T> Load(IDbCommand command, DBLoadParam param = DBLoadParam.None, DBTransaction baseTransaction = null)
        {
            var list = new List<T>();
            var transaction = baseTransaction ?? new DBTransaction(Connection, null, true);
            if (transaction.ReaderParam == DBLoadParam.None)
                transaction.ReaderParam = param;
            try
            {
                transaction.AddCommand(command);

                var whereInd = command.CommandText.IndexOf("where ", StringComparison.OrdinalIgnoreCase);
                var arg = new DBLoadProgressEventArgs(transaction.View, 0, 0, null);

                if (transaction.View != null && transaction.View.Table == this && transaction.View.IsStatic)
                    transaction.View.Clear();

                if ((transaction.ReaderParam & DBLoadParam.GetCount) == DBLoadParam.GetCount)
                {
                    string w = whereInd == -1 ? string.Empty : command.CommandText.Substring(whereInd);
                    var val = transaction.ExecuteQuery(transaction.AddCommand(DBCommand.CloneCommand(command, BuildQuery(w, "a", null, "count(*)"))), DBExecuteType.Scalar);
                    arg.TotalCount = val is Exception ? -1 : int.Parse(val.ToString());

                    if (arg.TotalCount < 0 || arg.TotalCount == 0)
                        return list;
                    if (items.Capacity < arg.TotalCount)
                        items.Capacity = arg.TotalCount;
                    //arg.TotalCount = Rows._items.Capacity;
                }
                if (transaction.Canceled)
                {
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

                if (transaction.Canceled)
                {
                    return list;
                }

                using (transaction.Reader = transaction.ExecuteQuery(command, DBExecuteType.Reader) as IDataReader)
                {
                    CheckColumns(transaction);
                    while (!transaction.Canceled && transaction.Reader.Read())
                    {
                        T row = null;
                        lock (Lock)
                        {
                            row = LoadFromReader(transaction);

                            if (!row.Attached && (transaction.ReaderParam & DBLoadParam.NoAttach) != DBLoadParam.NoAttach)
                            {
                                Add(row);
                            }
                        }
                        if (arg.TotalCount > 0)
                        {
                            arg.Current++;
                            arg.CurrentRow = row;
                            RaiseLoadProgress(arg);
                        }
                        list.Add(row);
                        if (transaction.View?.Table == this && transaction.View.IsStatic)
                            transaction.View.Add(row);
                    }
                    transaction.Reader.Close();
                }

                //Check IsSynchronized
                if (transaction.View?.Table == this)
                {
                    transaction.View.IsSynchronized = true;
                }

                if (whereInd < 0)
                {
                    IsSynchronized = true;
                }
                //return buffer;
            }
            finally
            {
                transaction.Reader = null;
                if (baseTransaction == null)
                {
                    transaction.Dispose();
                }
                RaiseLoadCompleate(transaction);
            }
            return list;
        }

        public async Task<IEnumerable<T>> LoadAsync(IDbCommand command, DBLoadParam param = DBLoadParam.None, DBTransaction baseTransaction = null)
        {
            var list = new List<T>();
            var transaction = baseTransaction ?? new DBTransaction(Connection, null, true);
            if (transaction.ReaderParam == DBLoadParam.None)
                transaction.ReaderParam = param;
            try
            {
                transaction.AddCommand(command);

                var whereInd = command.CommandText.IndexOf("where ", StringComparison.OrdinalIgnoreCase);
                var arg = new DBLoadProgressEventArgs(transaction.View, 0, 0, null);

                if (transaction.View != null && transaction.View.Table == this && transaction.View.IsStatic)
                    transaction.View.Clear();

                if ((transaction.ReaderParam & DBLoadParam.GetCount) == DBLoadParam.GetCount)
                {
                    string w = whereInd == -1 ? string.Empty : command.CommandText.Substring(whereInd);
                    var val = transaction.ExecuteQuery(transaction.AddCommand(DBCommand.CloneCommand(command, BuildQuery(w, "a", null, "count(*)"))), DBExecuteType.Scalar);
                    arg.TotalCount = val is Exception ? -1 : int.Parse(val.ToString());

                    if (arg.TotalCount < 0 || arg.TotalCount == 0)
                        return list;
                    if (items.Capacity < arg.TotalCount)
                        items.Capacity = arg.TotalCount;
                    //arg.TotalCount = Rows._items.Capacity;
                }
                if (transaction.Canceled)
                {
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

                if (transaction.Canceled)
                {
                    return list;
                }

                using (transaction.Reader = (IDataReader)await transaction.ExecuteQueryAsync(command, DBExecuteType.Reader))
                {
                    CheckColumns(transaction);
                    while (!transaction.Canceled && await transaction.ReadAsync())
                    {
                        T row = null;
                        lock (Lock)
                        {
                            row = LoadFromReader(transaction);

                            if (!row.Attached && (transaction.ReaderParam & DBLoadParam.NoAttach) != DBLoadParam.NoAttach)
                            {
                                Add(row);
                            }
                        }
                        if (arg.TotalCount > 0)
                        {
                            arg.Current++;
                            arg.CurrentRow = row;
                            RaiseLoadProgress(arg);
                        }
                        list.Add(row);
                        if (transaction.View?.Table == this && transaction.View.IsStatic)
                            transaction.View.Add(row);
                    }
                    transaction.Reader.Close();
                }

                //Check IsSynchronized
                if (transaction.View?.Table == this)
                {
                    transaction.View.IsSynchronized = true;
                }

                if (whereInd < 0)
                {
                    IsSynchronized = true;
                }
                //return buffer;
            }
            finally
            {
                transaction.Reader = null;
                if (baseTransaction == null)
                {
                    transaction.Dispose();
                }
                RaiseLoadCompleate(transaction);
            }
            return list;
        }

        public override void ReloadItem(object id, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null)
        {
            LoadItem(id, param);
        }

        public T LoadItem(object id, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null)
        {
            return LoadItem(id, PrimaryKey, param, cols, transaction);
        }

        public T LoadItem(object id, DBColumn column, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null)
        {
            return Load(CreateKeyCommmand(id, column, cols), param, transaction).FirstOrDefault();
        }

        public async Task<T> LoadItemAsync(object id, DBColumn column, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null)
        {
            return (await LoadAsync(CreateKeyCommmand(id, column, cols), param, transaction)).FirstOrDefault();
        }

        public override DBItem LoadItemById(object id, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null)
        {
            return LoadById(id, param, cols, transaction);
        }

        public T1 LoadById<T1>(object id, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null) where T1 : T
        {
            return (T1)LoadById(id, param, cols, transaction);
        }

        public T LoadById(object id, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null)
        {
            return LoadByKey(id, PrimaryKey, param, cols, transaction);
        }

        public ValueTask<T> LoadByIdAsync(object id, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null)
        {
            return LoadByKeyAsync(id, PrimaryKey, param, cols, transaction);
        }

        public T GetById(object id)
        {
            return LoadByKey(id, PrimaryKey);
        }

        public T LoadByKey(object key, DBColumn column, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null)
        {
            object val = column?.ParseValue(key);

            if (val == null || column == null)
                return null;

            T row = SelectOne(column, key) as T;

            if (row == null && (param & DBLoadParam.Load) == DBLoadParam.Load)
            {
                row = LoadItem(val, column, param, cols, transaction);
            }
            return row;
        }

        public async ValueTask<T> LoadByKeyAsync(object key, DBColumn column, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null)
        {
            object val = column?.ParseValue(key);

            if (val == null || column == null)
                return null;

            T row = SelectOne(column, key) as T;

            if (row == null && (param & DBLoadParam.Load) == DBLoadParam.Load)
            {
                row = await LoadItemAsync(val, column, param, cols, transaction);
            }
            return row;
        }

        public T LoadByCode(string code, DBLoadParam param = DBLoadParam.None)
        {
            if (CodeKey == null)
                return null;
            return LoadByCode(code, CodeKey, param);
        }

        public T LoadByCode(string code, string column, DBLoadParam param = DBLoadParam.None)
        {
            return LoadByCode(code, ParseColumn(column), param);
        }

        public override DBItem LoadItemByCode(string code, DBColumn column, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null)
        {
            return LoadByCode(code, column, param);
        }

        public T LoadByCode(string code, DBColumn column, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null)
        {
            var row = SelectOne(column, code);
            if (row == null && (param & DBLoadParam.Load) == DBLoadParam.Load)//&& !IsSynchronized
            {
                var command = System.CreateCommand(Schema.Connection, CreateQuery($"where a.{column.Name}={Schema.System.ParameterPrefix}{column.Name}", "a", Columns));
                System.CreateParameter(command, Schema.System.ParameterPrefix + column.Name, code, column);
                row = Load(command, param, transaction).FirstOrDefault();
            }
            return row;
        }

        public IEnumerable<T> LoadByStamp(QQuery query, DBTransaction transaction = null)
        {
            if (items.Count == 0)
                return Load(query);

            query.Columns.Clear();
            query.Columns.Add(new QColumn(PrimaryKey));
            query.Columns.Add(new QColumn(StampKey));

            return Load(query.ToCommand(), DBLoadParam.Synchronize, transaction);
        }

        private void CheckDelete(QQuery filter, IEnumerable<T> buf, DBLoadParam param, DBTransaction transaction)
        {
            var list = Select(filter).ToList();
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

        public override DBItem LoadItemFromReader(DBTransaction transaction)
        {
            return LoadFromReader(transaction);
        }

        public virtual T LoadFromReader(DBTransaction transaction)
        {
            T item = null;

            if (transaction.ReaderPrimaryKey >= 0)
            {
                item = PrimaryKey.SelectOneFromReader<T>(transaction, transaction.ReaderPrimaryKey);
            }
            if (transaction.ReaderStampKey >= 0 && !transaction.Reader.IsDBNull(transaction.ReaderStampKey))
            {
                if (item != null && (transaction.ReaderParam & DBLoadParam.Referencing) != 0)
                {
                    var stamp = transaction.Reader.GetDateTime(transaction.ReaderStampKey);
                    stamp = DateTime.SpecifyKind(stamp, DateTimeKind.Utc);
                    if (DateTime.Compare((DateTime)item.Stamp, stamp) >= 0)
                    {
                        return item;
                    }
                }
                else if ((transaction.ReaderParam & DBLoadParam.Synchronize) != 0)
                {
                    var stamp = transaction.Reader.GetDateTime(transaction.ReaderStampKey);
                    stamp = DateTime.SpecifyKind(stamp, DateTimeKind.Utc);
                    if (item != null && DateTime.Compare((DateTime)item.Stamp, stamp) >= 0)
                    {
                        return item;
                    }
                    else if (transaction.ReaderColumns.Count < 4)
                    {
                        return LoadById(transaction.Reader.GetValue(transaction.ReaderPrimaryKey));
                    }
                }
            }
            if (item == null)
            {
                var typeIndex = 0;
                if (transaction.ReaderItemTypeKey >= 0)
                    typeIndex = transaction.Reader.GetInt32(transaction.ReaderItemTypeKey);
                item = (T)NewItem(transaction.ReaderState, false, typeIndex);
            }

            for (int i = 0; i < transaction.ReaderColumns.Count; i++)
            {
                var column = transaction.ReaderColumns[i];
                column.LoadFromReader(transaction, item, i);
            }
            return item;
        }

        public override IEnumerable<DBItem> GetChangedItems()
        {
            return GetChanged();
        }

        public IEnumerable<T> GetChanged()
        {
            foreach (var item in (ICollection<T>)this)
            {
                if (item.IsChanged)
                    yield return item;
            }
        }

        public override IEnumerable<DBItem> SelectItems(string filter)
        {
            return Select(filter);
        }

        public IEnumerable<T> Select(string filter, IEnumerable<T> list = null)
        {
            using (var query = new QQuery(filter, this))
            {
                return Select(query, list);
            }
        }

        public IEnumerable<T> Select(QItemList<QParam> parameters, IEnumerable<T> list = null)
        {
            IEnumerable<T> buffer = null;
            for (int i = 0; i < parameters.Count; i++)
            {
                QParam param = parameters[i];
                if (buffer != null && param.Logic.Type == LogicTypes.And)
                {
                    if (!buffer.Any())
                        break;
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

        public override IEnumerable<DBItem> SelectItems(QQuery query)
        {
            return Select(query);
        }

        public IEnumerable<T> Select(QQuery query, IEnumerable<T> list = null)
        {
            IEnumerable<T> buf = null;
            if (query.Parameters.Count == 0)
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
            return buf;
        }

        public IEnumerable<T> Select(QParam param, IEnumerable<T> list = null)
        {
            if (param.ValueLeft is QFunc func
                && func.Type == QFunctionType.distinct
                && func.Items.FirstOrDefault() is QItem item)
            {
                return ListHelper.Distinct(list ?? items, item);
            }
            IEnumerable<T> buf = param.IsCompaund ? Select(param.Parameters, list) : null;
            if (buf == null)
            {
                if (param.ValueLeft is QColumn)
                {
                    buf = Select(param.Column, param.Comparer, param.Value, list);
                }
                else if (param.ValueLeft is QReflection)
                {
                    buf = Select(((QReflection)param.ValueLeft).Invoker, param.Comparer, param.Value, list);
                }
                else
                {
                    buf = Search(param, list);
                }
            }
            return buf;
        }

        public IEnumerable<T> Search(QParam param, IEnumerable<T> list = null)
        {
            list = list ?? AsReadOnly();
            foreach (T row in list)
            {
                if (param.ValueLeft == null || param.ValueRight == null)
                { }
                if (CheckItem(row, param.ValueLeft.GetValue(row), param.ValueRight.GetValue(row), param.Comparer))
                    yield return row;
            }
        }

        private IEnumerable<T> AsReadOnly()
        {
            for (int i = 0; i < items.Count; i++)
            {
                yield return items[i];
            }
        }

        public object Optimisation(DBColumn column, CompareType comparer, object value)
        {
            if (value == null)
                return value;
            if (value is QQuery)
            {
                if (column.IsPrimaryKey)
                {
                    var query = (QQuery)value;
                    if (query.Columns[0] is QColumn qcolumn && !query.IsRefence)
                    {
                        var buf = new List<T>();
                        foreach (DBItem item in query.Select())
                        {
                            var reference = item.GetReference<T>(qcolumn.Column, DBLoadParam.None);
                            //if (reference != null && reference.Table != this)
                            //{
                            //    throw new Exception(string.Format("Value {0} Table {1}", reference.Table, this));
                            //}
                            if (reference != null)
                            {

                                var index = buf.BinarySearch(reference);
                                if (index < 0)
                                    buf.Insert(-index - 1, reference);
                            }
                        }
                        value = buf;
                    }
                }
                else
                {
                    value = SelectQuery(null, (QQuery)value, comparer);
                }
            }
            else if (value.GetType() == typeof(QEnum))
            {
                value = ((QEnum)value).Items;
            }
            else if (comparer.Type == CompareTypes.In && value is string)
            {
                value = value.ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
            else if (comparer.Type == CompareTypes.Like)
            {
                value = Helper.BuildLike(value == null ? string.Empty : value.ToString());
            }
            return value;
        }

        public IEnumerable<T> Select(IInvoker invoker, CompareType comparer, object value, IEnumerable<T> list = null)
        {
            list = list ?? this;
            if (invoker == null)
                yield break;

            value = Optimisation(null, comparer, value);

            foreach (T row in list)
            {
                if (CheckItem(row, invoker.GetValue(row), value, comparer))
                    yield return row;
            }
        }

        public T SelectOne(DBColumn column, object val)
        {
            var value = column.ParseValue(val);
            if (column.Index != null)
            {
                return column.Index.SelectOne<T>(value);
            }
            return Select(column, CompareType.Equal, value).FirstOrDefault();
        }

        public override IEnumerable<DBItem> SelectItems(DBColumn column, CompareType comparer, object val)
        {
            return Select(column, comparer, val);
        }

        public IEnumerable<T> Select(DBColumn column, CompareType comparer, object value, IEnumerable<T> list = null)
        {
            list = list ?? this;
            if (column == null)
                return list;

            value = Optimisation(column, comparer, value);
            if (value is IEnumerable<T> enumerabble)
            {
                return enumerabble;
            }

            if (column.Index != null)
            {
                return column.Index.Select<T>(value, comparer);
            }
            return Search(column, comparer, value, list);
        }

        public IEnumerable<T> Search(DBColumn column, CompareType comparer, object value, IEnumerable<T> list)
        {
            list = list ?? this;
            foreach (T row in list)
            {
                if (CheckItem(row, row.GetValue(column), value, comparer))
                    yield return row;
            }
        }

        public T SelectRow(DBColumn column, CompareType comparer, object value, IEnumerable<T> list = null)
        {
            return Select(column, comparer, value, list).FirstOrDefault();
        }

        public override void Dispose()
        {
            base.Dispose();
            Clear();
            queryViews.Clear();
            queryViews.TrimExcess();
        }

        public override DBItem NewItem(DBUpdateState state = DBUpdateState.Insert, bool def = true)
        {
            var item = (T)FormatterServices.GetUninitializedObject(typeof(T));
            item.Build(this, def, ItemTypeIndex);
            item.update = state;
            return item;
        }
    }

}
