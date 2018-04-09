using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataWF.Common;

namespace DataWF.Data
{
    public class DBTable<T> : DBTable, ICollection<T> where T : DBItem, new()
    {
        protected List<T> items = new List<T>();
        protected List<DBTableView<T>> queryViews = new List<DBTableView<T>>(1);


        public DBTable()
        {
            SetItemType(typeof(T));
        }

        public DBTable(string name) : base(name)
        {
            SetItemType(typeof(T));
        }

        [Browsable(false)]
        public int Capacity
        {
            get { return items.Capacity; }
            set { items.Capacity = value; }
        }

        [Browsable(false)]
        public override int Count
        {
            get { return items.Count; }
        }

        [Browsable(false)]
        public override IDBTableView DefaultItemsView
        {
            get { return DefaultView; }
        }

        [Browsable(false)]
        public DBTableView<T> DefaultView
        {
            get
            {
                if (queryViews.Count == 0)
                    queryViews.Add(CreateView());
                return queryViews.Count > 0 ? queryViews[0] : null;
            }
        }

        [Browsable(false)]
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

        public bool Contains(T item)
        {
            return item.Table == this && item.Attached;
        }

        public void Add(params object[] fields)
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
            foreach (var column in Columns)
            {
                column.Index?.Add(item);
            }
            item.OnAttached();

            OnItemChanged(item, null, ListChangedType.ItemAdded);

        }

        public override bool Remove(DBItem item)
        {
            return Remove((T)item);
        }

        public bool Remove(T item)
        {
            if (!item.Attached)
                return false;

            OnItemChanged(item, null, ListChangedType.ItemDeleted);
            foreach (var column in Columns)
            {
                if (column.Index != null)
                    column.Index.Remove(item);
            }
            items.Remove(item);
            item.OnDetached();
            return true;
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
                var temp = items.ToArray();
                items.Clear();
                if (!(this is IDBVirtualTable))
                {
                    foreach (var column in Columns)
                    {
                        column.Pull.Clear();
                        if (column.Index != null)
                            column.Index.Clear();
                    }
                }
                OnItemChanged(null, null, ListChangedType.Reset);

                foreach (DBItem row in temp)
                {
                    row.Dispose();
                }
                GC.Collect();
            }
            IsSynchronized = false;
        }

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ThreadSafeEnumerator<T>(items);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new ThreadSafeEnumerator<T>(items);
        }

        public override IEnumerator<DBItem> GetEnumerator()
        {
            return new ThreadSafeEnumerator<T>(items);
        }

        #endregion
        public override void RemoveView(IDBTableView view)
        {
            RemoveView((DBTableView<T>)view);
        }

        public void RemoveView(DBTableView<T> view)
        {
            queryViews.Remove(view);
        }

        public override void AddView(IDBTableView view)
        {
            AddView((DBTableView<T>)view);
        }

        public void AddView(DBTableView<T> view)
        {
            queryViews.Add(view);
        }

        public override void OnItemChanged(DBItem item, string property, ListChangedType type)
        {
            if (property == nameof(DBItem.Attached)
                || property == nameof(DBItem.UpdateState))
                return;

            foreach (var collection in virtualViews)
            {
                if (type == ListChangedType.Reset)
                {
                    collection.Refresh();
                }
                else
                {
                    collection.CheckItem(type, item, property);
                }
            }

            for (int i = 0; i < queryViews.Count; i++)
                queryViews[i].OnItemChanged(item, property, type);
        }

        public override IDBTableView CreateItemsView(string query, DBViewKeys mode, DBStatus filter)
        {
            return CreateView(query, mode, filter);
        }

        public DBTableView<T> CreateView(string query = "", DBViewKeys mode = DBViewKeys.None, DBStatus filter = DBStatus.Empty)
        {
            return new DBTableView<T>(this, query, mode, filter);
        }

        public override IEnumerable<DBItem> LoadItems(string whereText = null, DBLoadParam param = DBLoadParam.None, IEnumerable cols = null, IDBTableView synch = null)
        {
            return Load(whereText, param, cols, synch);
        }

        public List<T> Load(string whereText = null, DBLoadParam param = DBLoadParam.None, IEnumerable cols = null, IDBTableView synch = null)
        {
            using (var transaction = new DBTransaction(Schema.Connection) { View = synch, ReaderParam = param })
                return Load(transaction, whereText, cols);
        }

        public List<T> Load(DBTransaction transaction, string whereText = null, IEnumerable cols = null)
        {
            if (string.IsNullOrEmpty(whereText) || whereText.Trim().Equals("where", StringComparison.OrdinalIgnoreCase))
                whereText = string.Empty;
            else if (whereText.Length > 3
                     && whereText.IndexOf("where", StringComparison.OrdinalIgnoreCase) < 0
                     && !whereText.Trim().StartsWith("select", StringComparison.OrdinalIgnoreCase))
                whereText = "where " + whereText;

            return Load(transaction, transaction.AddCommand(DetectQuery(whereText, cols)));
        }

        public override IEnumerable<DBItem> LoadItems(QQuery query, DBLoadParam param = DBLoadParam.None, IDBTableView synch = null)
        {
            return Load(query, param, synch);
        }

        public List<T> Load(QQuery query, DBLoadParam param = DBLoadParam.None, IDBTableView synch = null)
        {
            using (var transaction = new DBTransaction(Schema.Connection) { View = synch, ReaderParam = param })
            {
                return Load(transaction, query);
            }
        }

        public override IEnumerable<DBItem> LoadItems(DBTransaction transaction, QQuery query)
        {
            return Load(transaction, query);
        }

        public List<T> Load(DBTransaction transaction, QQuery query)
        {
            if (Count == 0)
                transaction.ReaderParam &= ~DBLoadParam.CheckDeleted;

            var buf = Load(transaction, transaction.AddCommand(query.ToCommand(true)));

            if (buf != null && (transaction.ReaderParam & DBLoadParam.CheckDeleted) == DBLoadParam.CheckDeleted)
                CheckDelete(transaction, query, buf);

            if (query.Parameters.Count == 0)
                IsSynchronized = true;
            return buf;
        }

        public override IEnumerable<DBItem> LoadItems(DBTransaction transaction, IDbCommand command)
        {
            return Load(transaction, command);
        }

        public List<T> Load(DBTransaction transaction, IDbCommand command)
        {
            if (transaction.Canceled)
                return null;
            var whereInd = command.CommandText.IndexOf("where ", StringComparison.OrdinalIgnoreCase);
            var arg = new DBLoadProgressEventArgs(transaction.View, 0, 0, null);
            var creference = Columns.GetIsReference();
            List<T> buf = null;

            if (items.Count == 0)
                transaction.ReaderParam &= ~DBLoadParam.Synchronize;
            try
            {
                if ((transaction.ReaderParam & DBLoadParam.GetCount) == DBLoadParam.GetCount)
                {
                    string w = whereInd == -1 ? string.Empty : command.CommandText.Substring(whereInd);
                    var val = transaction.ExecuteQuery(transaction.AddCommand(DBCommand.CloneCommand(command, BuildQuery(w, null, "count(*)"))), DBExecuteType.Scalar);
                    arg.TotalCount = val is Exception ? -1 : int.Parse(val.ToString());

                    if (arg.TotalCount < 0)
                        return null;
                    if (arg.TotalCount == 0)
                        return new List<T>(0);
                    if (items.Capacity < arg.TotalCount)
                        items.Capacity = arg.TotalCount;
                    //arg.TotalCount = Rows._items.Capacity;
                }
                if (transaction.Canceled)
                {
                    return null;
                }
                if ((transaction.ReaderParam & DBLoadParam.ReferenceRow) == DBLoadParam.ReferenceRow)
                {
                    transaction.BeginSubTransaction();
                }
                if (transaction.Canceled)
                {
                    return null;
                }
                using (transaction.Reader = transaction.ExecuteQuery(command, DBExecuteType.Reader) as IDataReader)
                {
                    buf = arg.TotalCount > 0 ? new List<T>(arg.TotalCount) : new List<T>();
                    CheckColumns(transaction);
                    while (!transaction.Canceled && transaction.Reader.Read())
                    {
                        T row = null;
                        lock (Lock)
                        {
                            row = LoadFromReader(transaction);

                            if (transaction.SubTransaction != null)
                            {
                                foreach (var refer in creference)
                                {
                                    if (refer.ReferenceTable != this)
                                        row.GetReference(refer, DBLoadParam.Load, transaction.SubTransaction);
                                }
                            }
                            if (!row.Attached && (transaction.ReaderParam & DBLoadParam.NoAttach) != DBLoadParam.NoAttach)
                                Add(row);
                        }
                        if (arg.TotalCount > 0)
                        {
                            arg.Current = buf.Count;
                            arg.CurrentRow = row;
                            RaiseLoadProgress(arg);
                        }
                        buf.Add(row);
                        if (transaction.View != null && transaction.View.Table == this && transaction.View.IsStatic)
                            transaction.View.Add(row);
                    }
                    transaction.Reader.Close();
                }
                //Check IsSynchronized
                if (transaction.View != null && transaction.View.Table == this)
                    transaction.View.IsSynchronized = true;
                if (whereInd < 0)
                {
                    IsSynchronized = true;
                }
            }
            finally
            {
                base.RaiseLoadCompleate(new DBLoadCompleteEventArgs(transaction.View, buf));
            }

            return buf;
        }

        public Task<IEnumerable<T>> LoadAsync(QQuery query, DBLoadParam param, IDBTableView synch)
        {
            return Task.Run<IEnumerable<T>>(() =>
            {
                try
                {
                    return Load(query, param, synch);
                }
                catch (Exception e)
                {
                    Helper.OnException(e);
                    return null;
                }
            });
        }

        public Task<IEnumerable<T>> LoadAsync(DBTransaction transaction, IDbCommand command)
        {
            return Task.Run<IEnumerable<T>>(() =>
            {
                try
                {
                    return Load(transaction, command);
                }
                catch (Exception e)
                {
                    Helper.OnException(e);
                    return null;
                }
            });
        }

        public Task<IEnumerable<T>> LoadAsync(string query, DBLoadParam param, IEnumerable columns, IDBTableView synch)
        {
            return Task.Run<IEnumerable<T>>(() =>
            {
                try
                {
                    return Load(query, param, columns, synch);
                }
                catch (Exception e)
                {
                    Helper.OnException(e);
                    return null;
                }
            });
        }

        public override void ReloadItem(object id, DBTransaction transaction = null)
        {
            LoadItem(transaction, id);
        }

        public T LoadItem(DBTransaction transaction, object id, IEnumerable cols = null)
        {
            string idName = Schema.System.ParameterPrefix + PrimaryKey.Name;
            var command = transaction.AddCommand(DetectQuery(string.Format("where {0}={1}", PrimaryKey.Name, idName), cols));
            transaction.AddParameter(command, idName, id);
            var rows = Load(transaction, command);
            return rows == null || rows.Count == 0 ? null : rows[0];
        }

        public override DBItem LoadItemById(object id, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null, IEnumerable cols = null, IDBTableView synch = null)
        {
            return LoadById(id, param, transaction, cols, synch);
        }

        public T LoadById(object id, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null, IEnumerable cols = null, IDBTableView synch = null)
        {
            object val = DBService.ParseValue(PrimaryKey, id);

            if (val == null || PrimaryKey == null)
                return null;

            T row = SelectOne(PrimaryKey, val) as T;

            if (row == null && (param & DBLoadParam.Load) == DBLoadParam.Load)
            {
                var temp = transaction ?? new DBTransaction(Schema.Connection) { View = synch, ReaderParam = param };
                try
                {
                    row = LoadItem(temp, val, cols);
                }
                finally
                {
                    if (transaction == null)
                        temp.Dispose();
                }

            }
            return row;
        }

        public T LoadByCode(string code, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null)
        {
            if (CodeKey == null)
                return null;
            return LoadByCode(code, CodeKey, param, transaction);
        }

        public T LoadByCode(string code, string column, DBLoadParam param, DBTransaction transaction = null)
        {
            return LoadByCode(code, ParseColumn(column), param, transaction);
        }

        public override DBItem LoadItemByCode(string code, DBColumn column, DBLoadParam param, DBTransaction transaction = null)
        {
            return LoadItemByCode(code, column, param, transaction);
        }

        public T LoadByCode(string code, DBColumn column, DBLoadParam param, DBTransaction transaction = null)
        {
            var row = SelectOne(column, code);
            if (row == null && (param & DBLoadParam.Load) == DBLoadParam.Load)
            {
                var temp = transaction ?? new DBTransaction(Schema.Connection) { ReaderParam = param };
                try
                {
                    var command = temp.AddCommand(DetectQuery(string.Format("where {0}={1}{0}", column.Name, Schema.System.ParameterPrefix), Columns));
                    temp.AddParameter(command, Schema.System.ParameterPrefix + column.Name, code);
                    var list = Load(temp, command);
                    row = list.Count == 0 ? null : list[0];
                }
                finally
                {
                    if (transaction == null)
                        temp.Dispose();
                }
            }
            return row;
        }

        public void LoadByStamp(DBTransaction transaction, IDBTableView synch)
        {
            var synchCols = new List<DBColumn>(2);
            synchCols.Add(PrimaryKey);
            synchCols.Add(StampKey);
            string squery = string.Empty;
            int whereInd = transaction.CommandText.IndexOf("where ", StringComparison.InvariantCultureIgnoreCase);
            if (whereInd != -1)
                squery = transaction.CommandText.Substring(whereInd);
            squery = DetectQuery(squery, synchCols);

            int cur = 0;
            var vals = Schema.Connection.ExecuteQResult(squery);
            //var e = new DBLoadProgressEventArgs(synch, vals.Values.Count, cur, null);

            foreach (var val in vals.Values)
            {
                if (synch != null && synch.Command != transaction.Command)
                    break;
                cur++;
                var row = LoadById(val[0], DBLoadParam.None);

                if (row == null)
                    LoadItem(transaction, val[0]);
                else if (StampKey != null && ListHelper.Compare(row.Stamp, val[1], null, false) < 0)
                    LoadItem(transaction, row.PrimaryId);
            }
        }

        private void CheckDelete(DBTransaction transcation, QQuery filter, List<T> buf)
        {
            var list = Select(filter).ToList();
            if (list.Count > buf.Count)
            {
                foreach (var item in list)
                {
                    if ((item.UpdateState & DBUpdateState.Insert) != DBUpdateState.Insert && !buf.Contains(item))
                    {
                        if (transcation.View != null && transcation.View.IsStatic)
                            transcation.View.Remove(item);
                        Remove(item);
                    }

                    if (transcation.Canceled)
                        break;
                }
            }
        }

        public override DBItem LoadItemFromReader(DBTransaction transaction)
        {
            return LoadFromReader(transaction);
        }

        public virtual T LoadFromReader(DBTransaction transaction)
        {
            T srow = null;
            if (transaction.ReaderPrimaryKey >= 0)
            {
                srow = SelectOne(PrimaryKey, transaction.Reader.GetValue(transaction.ReaderPrimaryKey));
            }
            if (srow == null)
            {
                var typeIndex = 0;
                if (transaction.ReaderItemTypeKey >= 0)
                    typeIndex = transaction.Reader.GetInt32(transaction.ReaderItemTypeKey);
                srow = New(transaction.ReaderState, false, typeIndex);
            }
            else if (transaction.ReaderStampKey >= 0 && (transaction.ReaderParam & DBLoadParam.Synchronize) == DBLoadParam.Synchronize
                     && srow.Stamp.Value.CompareTo(transaction.Reader.GetDateTime(transaction.ReaderStampKey)) >= 0)
            {
                return srow;
            }
            for (int i = 0; i < transaction.ReaderColumns.Count; i++)
            {
                var column = transaction.ReaderColumns[i];
                var value = transaction.DbConnection.System.ReadValue(column, transaction.Reader.GetValue(i));

                if (!srow.Attached || srow.UpdateState == DBUpdateState.Default || !srow.GetOld(column, out object oldValue))
                {
                    srow.SetValue(value, column, false);
                }
            }
            return srow;
        }

        public override DBItem NewItem(DBUpdateState state = DBUpdateState.Insert, bool def = true)
        {
            return New(state, def);
        }

        public T New(DBUpdateState state = DBUpdateState.Insert, bool def = true, int typeIndex = 0)
        {
            var type = GetItemType(typeIndex);
            var item = (T)type.Constructor.Create();
            item.Build(this, state, def);
            return item;
        }

        public virtual DBItemType GetItemType(int typeIndex)
        {
            return typeIndex == 0 ? ItemType : ItemTypes[typeIndex];
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

        public override IEnumerable<DBItem> SelectItems(QQuery qQuery)
        {
            return Select(query);
        }

        public IEnumerable<T> Select(QQuery query, IEnumerable<T> list = null)
        {
            IEnumerable<T> buf = null;
            if (query.Parameters.Count == 0)
            {
                buf = list ?? items;
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
            IEnumerable<T> buf = Select(param.Parameters, list);
            if (buf == null)
            {
                if (param.ValueLeft is QColumn)
                {
                    buf = Select(param.Column, param.Value, param.Comparer, list);
                }
                else if (param.ValueLeft is QReflection)
                {
                    buf = Select(((QReflection)param.ValueLeft).Invoker, param.Value, param.Comparer, list);
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
            list = list ?? this;
            foreach (T row in list)
            {
                if (param.ValueLeft == null || param.ValueRight == null)
                { }
                if (CheckItem(row, param.ValueLeft.GetValue(row), param.ValueRight.GetValue(row), param.Comparer))
                    yield return row;
            }
        }

        public object Optimisation(DBColumn column, object val, CompareType comparer)
        {
            if (val == null)
                return val;
            if (val is QQuery)
            {
                if (column.IsPrimaryKey)
                {
                    var query = (QQuery)val;
                    var qcolumn = query.Columns[0] as QColumn;
                    if (qcolumn != null && !query.IsRefence)
                    {
                        var buf = new List<T>();
                        foreach (DBItem item in query.Select())
                        {
                            var value = item.GetReference<T>(qcolumn.Column, DBLoadParam.None);
                            if (value != null && value.Table != this)
                            {
                                throw new Exception(string.Format("Value {0} Table {1}", value.Table, this));
                            }
                            if (value != null)
                            {

                                var index = buf.BinarySearch(value);
                                if (index < 0)
                                    buf.Insert(-index - 1, value);
                            }
                        }
                        val = buf;
                    }
                }
                else
                    val = SelectQuery(null, (QQuery)val, comparer);
            }
            else if (val.GetType() == typeof(QEnum))
            {
                val = ((QEnum)val).Items;
            }
            else if (comparer.Type == CompareTypes.In && val is string)
            {
                val = val.ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
            else if (comparer.Type == CompareTypes.Like)
            {
                val = Helper.BuildLike(val == null ? string.Empty : val.ToString());
            }
            return val;
        }

        public IEnumerable<T> Select(IInvoker invoker, object val, CompareType comparer, IEnumerable<T> list = null)
        {
            list = list ?? this;
            if (invoker == null)
                yield break;

            val = Optimisation(null, val, comparer);

            foreach (T row in list)
            {
                if (CheckItem(row, invoker.Get(row), val, comparer))
                    yield return row;
            }
        }

        public T SelectOne(DBColumn column, object val)
        {
            var value = DBService.ParseValue(column, val);
            if (column.Index != null)
            {
                return column.Index.SelectOne<T>(value);
            }
            return Select(column, value, CompareType.Equal).FirstOrDefault();
        }

        public override IEnumerable<DBItem> SelectItems(DBColumn column, object val, CompareType comparer)
        {
            return Select(column, val, comparer);
        }

        public IEnumerable<T> Select(DBColumn column, object val, CompareType comparer, IEnumerable<T> list = null)
        {
            list = list ?? this;
            if (column == null)
                return list;

            val = Optimisation(column, val, comparer);
            if (val is IEnumerable<T>)
            {
                return (IEnumerable<T>)val;
            }

            if (column.Index != null)
            {
                return column.Index.Select<T>(val, comparer);
            }
            return Search(column, val, comparer, list);
        }

        public IEnumerable<T> Search(DBColumn column, object val, CompareType comparer, IEnumerable<T> list)
        {
            list = list ?? this;
            foreach (T row in list)
            {
                if (CheckItem(row, row[column], val, comparer))
                    yield return row;
            }
        }

        public T SelectRow(DBColumn column, object val, CompareType comparer, IEnumerable<T> list = null)
        {
            return Select(column, val, comparer, list).FirstOrDefault();
        }

        public override void Dispose()
        {
            base.Dispose();
            Clear();
            queryViews.Clear();
            queryViews.TrimExcess();
        }
    }

}
