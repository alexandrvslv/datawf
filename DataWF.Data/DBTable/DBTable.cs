/*
 DBTable.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using DataWF.Common;
using System.Data;
using System.Xml.Serialization;
using System.Reflection;
using System.Threading;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Globalization;
using System.Diagnostics;

namespace DataWF.Data
{

    public abstract class DBTable : DBSchemaItem, ICollection<DBItem>, IComparable, IAccessable, IDisposable
    {
        protected DBCommand dmlInsert;
        protected DBCommand dmlInsertSequence;
        protected DBCommand dmlDelete;
        protected DBLogTable logTable;
        protected DBTableGroup tableGroup;
        protected DBColumn accessKey = DBColumn.EmptyKey;
        protected DBColumn primaryKey = DBColumn.EmptyKey;
        protected DBColumn dateKey = DBColumn.EmptyKey;
        protected DBColumn stampKey = DBColumn.EmptyKey;
        protected DBColumn codeKey = DBColumn.EmptyKey;
        protected DBColumn typeKey = DBColumn.EmptyKey;
        protected DBColumn groupKey = DBColumn.EmptyKey;
        protected DBColumn stateKey = DBColumn.EmptyKey;
        protected DBColumn imageKey = DBColumn.EmptyKey;
        protected DBColumn itemTypeKey = DBColumn.EmptyKey;

        private DBSequence cacheSequence;
        public DBComparer DefaultComparer;
        public int Hash = -1;

        protected string query;
        protected string comInsert;
        protected string comUpdate;
        protected string comDelete;
        protected string groupName;
        protected string sequenceName;
        protected bool caching = false;
        protected DBTableType type = DBTableType.Table;
        private int block = 1000;
        internal object locker = new object();
        protected List<IDBVirtualTable> virtualViews = new List<IDBVirtualTable>(0);
        private DBItemType itemType;

        protected DBTable(string name = null) : base(name)
        {
            ColumnGroups = new DBColumnGroupList(this);
            Columns = new DBColumnList(this);
            Indexes = new DBIndexList(this);
            Constraints = new DBConstraintList<DBConstraint>(this);
            Foreigns = new DBForeignList(this);
        }

        [Browsable(false)]
        public string LogTableName { get; set; }

        [XmlIgnore]
        public DBLogTable LogTable
        {
            get { return logTable ?? (logTable = DBService.ParseTable(LogTableName) as DBLogTable); }
            set
            {
                logTable = value;
                LogTableName = value?.name;
            }
        }

        [Browsable(false)]
        public object Lock
        {
            get { return locker; }
        }

        //[Browsable(false), XmlIgnore]
        //public TableAttribute Info { get; protected set; }

        public DBColumn ParseProperty(string property)
        {
            return Columns.GetByProperty(property);
        }

        [Browsable(false)]
        public Dictionary<int, DBItemType> ItemTypes { get; set; } = new Dictionary<int, DBItemType>();

        [XmlIgnore]
        public DBItemType ItemType => itemType;

        protected void SetItemType(Type type)
        {
            itemType = ItemTypes[0] = new DBItemType { Type = type };
            // Info = DBService.GetTableAttribute(type);
        }

        public override string FullName
        {
            get { return string.Format("{0}.{1}", Schema != null ? Schema.Name : string.Empty, name); }
        }

        [Category("Database")]
        public string Query
        {
            get { return query; }
            set
            {
                if (query != value)
                {
                    query = value;
                    OnPropertyChanged(nameof(Query), true);
                }
            }
        }

        [Browsable(false)]
        public DBSystem System
        {
            get { return Schema?.System; }
        }

        public int BlockSize
        {
            get { return block; }
            set { block = value; }
        }

        public virtual string SqlName
        {
            get { return name; }
        }

        [Browsable(false)]
        public abstract bool IsEdited { get; }

        [Category("Database")]
        public string ComInsert
        {
            get { return comInsert; }
            set { comInsert = value; }
        }

        [Category("Database")]
        public string ComUpdate
        {
            get { return comUpdate; }
            set { comUpdate = value; }
        }

        [Category("Database")]
        public string ComDelete
        {
            get { return comDelete; }
            set { comDelete = value; }
        }

        [Browsable(false), Category("Group")]
        public string GroupName
        {
            get { return groupName; }
            set
            {
                if (groupName == value)
                    return;
                groupName = value;
                tableGroup = null;
                OnPropertyChanged(nameof(GroupName), false);
            }
        }

        [XmlIgnore, Category("Group")]
        public DBTableGroup Group
        {
            get
            {
                if (tableGroup == null && groupName != null)
                    tableGroup = Schema?.TableGroups[groupName];
                return tableGroup;
            }
            set
            {
                tableGroup = value;
                GroupName = value?.Name;
            }
        }

        [Browsable(false)]
        public string SequenceName
        {
            get { return sequenceName; }
            set
            {
                if (sequenceName != value)
                {
                    sequenceName = value;
                    OnPropertyChanged(nameof(SequenceName), false);
                }
            }
        }

        [XmlIgnore]
        public DBSequence Sequence
        {
            get { return cacheSequence ?? (cacheSequence = Schema?.Sequences[sequenceName]); }
            set
            {
                cacheSequence = value;
                SequenceName = value?.Name;
                if (value != null && !Schema.Sequences.Contains(SequenceName))
                {
                    Schema.Sequences.Add(value);
                }
            }
        }

        [Category("Keys")]
        public DBColumn AccessKey
        {
            get
            {
                if (accessKey == DBColumn.EmptyKey)
                {
                    accessKey = Columns.GetByKey(DBColumnKeys.Access);
                }
                return accessKey;
            }
        }

        [Category("Keys")]
        public DBColumn PrimaryKey
        {
            get
            {
                if (primaryKey == DBColumn.EmptyKey)
                {
                    primaryKey = Columns.GetByKey(DBColumnKeys.Primary);
                }
                return primaryKey;
            }
        }

        [Category("Keys")]
        public DBColumn StampKey
        {
            get
            {
                if (stampKey == DBColumn.EmptyKey)
                {
                    stampKey = Columns.GetByKey(DBColumnKeys.Stamp);
                }
                return stampKey;
            }
        }

        [Category("Keys")]
        public DBColumn DateKey
        {
            get
            {
                if (dateKey == DBColumn.EmptyKey)
                {
                    dateKey = Columns.GetByKey(DBColumnKeys.Date);
                }
                return dateKey;
            }
        }

        [Category("Keys")]
        public DBColumn GroupKey
        {
            get
            {
                if (groupKey == DBColumn.EmptyKey)
                {
                    groupKey = Columns.GetByKey(DBColumnKeys.Group);
                }
                return groupKey;
            }
        }

        [Category("Keys")]
        public DBColumn ElementTypeKey
        {
            get
            {
                if (typeKey == DBColumn.EmptyKey)
                {
                    typeKey = Columns.GetByKey(DBColumnKeys.ElementType);
                }
                return typeKey;
            }
        }

        [Category("Keys")]
        public DBColumn StatusKey
        {
            get
            {
                if (stateKey == DBColumn.EmptyKey)
                {
                    stateKey = Columns.GetByKey(DBColumnKeys.State);
                }
                return stateKey;
            }
        }

        [Category("Keys")]
        public DBColumn ItemTypeKey
        {
            get
            {
                if (itemTypeKey == DBColumn.EmptyKey)
                {
                    itemTypeKey = Columns.GetByKey(DBColumnKeys.ItemType);
                }
                return itemTypeKey;
            }
        }

        [Category("Keys")]
        public DBColumn CodeKey
        {
            get
            {
                if (codeKey == DBColumn.EmptyKey)
                {
                    codeKey = Columns.GetByKey(DBColumnKeys.Code);
                }
                return codeKey;
            }
        }

        [Category("Keys")]
        public DBColumn ImageKey
        {
            get
            {
                if (imageKey == DBColumn.EmptyKey)
                {
                    imageKey = Columns.GetByKey(DBColumnKeys.Image);
                }
                return imageKey;
            }
        }

        public DBTableType Type
        {
            get { return type; }
            set
            {
                if (type == value)
                    return;
                type = value;
                OnPropertyChanged(nameof(Type), true);
            }
        }

        [XmlIgnore, Category("Database")]
        public virtual bool IsLoging
        {
            get { return LogTable != null; }
            set
            {
                if (value)
                {
                    GenerateLogTable();
                }
                else
                {
                    LogTable = null;
                }
            }
        }

        [Category("Database")]
        public bool IsCaching
        {
            get { return caching; }
            set
            {
                caching = value;
                OnPropertyChanged(nameof(IsCaching), false);
            }
        }

        [Category("Column")]
        public DBColumnList Columns { get; set; }

        [Category("Column")]
        public DBColumnGroupList ColumnGroups { get; set; }

        [Category("Performance")]
        public DBConstraintList<DBConstraint> Constraints { get; set; }

        [Category("Performance")]
        public DBIndexList Indexes { get; set; }

        [Category("Performance")]
        public DBForeignList Foreigns { get; set; }

        public abstract int Count { get; }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public abstract bool Contains(DBItem item);

        public abstract bool Remove(DBItem item);

        public abstract IEnumerator<DBItem> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public abstract void CopyTo(DBItem[] array, int arrayIndex);

        public abstract void OnItemChanged(DBItem item, string property, ListChangedType type);

        public IEnumerable<DBTable> GetChilds()
        {
            foreach (var item in virtualViews)
            {
                yield return (DBTable)item;
            }
        }

        public void FillReferenceBlock(DBTransaction transaction)
        {
            var command = transaction.Command;
            foreach (var column in Columns.GetIsReference())
            {
                if ((column.Keys & DBColumnKeys.Group) != DBColumnKeys.Group && column.ReferenceTable != this && !column.ReferenceTable.IsSynchronized)
                {
                    var sub = transaction.AddCommand(DBCommand.CloneCommand(command, column.ReferenceTable.BuildQuery(string.Format("where {0} in (select {1} {2})",
                                  column.ReferenceTable.PrimaryKey.Name,
                                  column.Name,
                                  command.CommandText.Substring(command.CommandText.IndexOf(" from ", StringComparison.OrdinalIgnoreCase))), null)));
                    column.ReferenceTable.LoadItems(transaction, sub);
                }
            }
            transaction.AddCommand(command);
        }

        public event EventHandler<DBLoadProgressEventArgs> LoadProgress;

        protected void RaiseLoadProgress(DBLoadProgressEventArgs arg)
        {
            LoadProgress?.Invoke(this, arg);
        }

        public event EventHandler<DBLoadCompleteEventArgs> LoadComplete;

        protected void RaiseLoadCompleate(DBLoadCompleteEventArgs args)
        {
            LoadComplete?.Invoke(this, args);
        }

        public event EventHandler<DBLoadColumnsEventArgs> LoadColumns;

        protected void RaiseLoadColumns(DBLoadColumnsEventArgs arg)
        {
            LoadColumns?.Invoke(this, arg);
        }

        public void CheckColumns(DBTransaction transaction)
        {
            bool newcol = false;
            transaction.ReaderColumns = new List<DBColumn>(transaction.Reader.FieldCount);
            for (int i = 0; i < transaction.ReaderColumns.Capacity; i++)
            {
                string fieldName = transaction.Reader.GetName(i);
                if (fieldName.Length == 0)
                    fieldName = i.ToString();
                var column = CheckColumn(fieldName, transaction.Reader.GetFieldType(i), ref newcol);
                if (column.IsPrimaryKey)
                {
                    transaction.ReaderPrimaryKey = i;
                }
                if ((column.Keys & DBColumnKeys.Stamp) == DBColumnKeys.Stamp)
                {
                    transaction.ReaderStampKey = i;
                }
                if ((column.Keys & DBColumnKeys.ItemType) == DBColumnKeys.ItemType)
                {
                    transaction.ReaderItemTypeKey = i;
                }
                transaction.ReaderColumns.Add(column);
            }
            if (newcol)
            {
                RaiseLoadColumns(new DBLoadColumnsEventArgs(transaction.View));
            }
        }

        public virtual DBColumn CheckColumn(string name, Type type, ref bool newCol)
        {
            var column = Columns[name];
            if (column == null)
            {
                column = new DBColumn(name);
                column.DataType = type;
                Columns.Add(column);
                newCol = true;
            }
            column.ReaderDataType = type;
            return column;
        }
        public abstract DBItem this[int index] { get; }

        public abstract void Add(DBItem item);

        public abstract DBItem LoadItemFromReader(DBTransaction transaction);

        public abstract IEnumerable LoadItems(QQuery query, DBLoadParam param = DBLoadParam.None, IDBTableView synch = null);

        public abstract IEnumerable LoadItems(string whereText = null, DBLoadParam param = DBLoadParam.None, IEnumerable cols = null, IDBTableView synch = null);

        public abstract IEnumerable LoadItems(DBTransaction transaction, QQuery query);

        public abstract IEnumerable LoadItems(DBTransaction transaction, IDbCommand command);

        public abstract DBItem LoadItemByCode(string code, DBColumn column, DBLoadParam param, DBTransaction transaction = null);

        public abstract DBItem LoadItemById(object id, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null, IEnumerable cols = null, IDBTableView synch = null);

        public abstract void ReloadItem(object id, DBTransaction transaction = null);

        public abstract void AddView(IDBTableView view);

        public abstract void RemoveView(IDBTableView view);

        //public List<DBItem> FillRows(string whereCommand)
        //{
        //    var rows = new List<DBItem>();
        //    var connection = DBService.GetConnection(Schema.Connection);
        //    try
        //    {
        //        using (var command = connection.CreateCommand())
        //        {
        //            command.CommandTimeout = connection.ConnectionTimeout;
        //            command.CommandText = DBService.ParceSelect(this, whereCommand);
        //            using (var reader = (IDataReader)DBService.ExecuteQuery(command, DBExecuteType.Reader))
        //            {
        //                var rcolumn = CheckColumns(reader);
        //                while (reader.Read())
        //                {
        //                    var row = LoadRowFromReader(rcolumn, reader, DBLoadParam.None, DBRowState.Default);
        //                    rows.Add(row);
        //                }
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        connection.Close();
        //    }
        //    return rows;
        //}

        public event EventHandler<DBItemEventArgs> RowUpdating;

        public bool OnUpdating(DBItemEventArgs e)
        {
            DBService.OnUpdating(e);
            RowUpdating?.Invoke(this, e);
            return !e.Cancel;
        }

        public event EventHandler<DBItemEventArgs> RowUpdated;

        public void OnUpdated(DBItemEventArgs e)
        {
            DBService.OnUpdated(e);
            RowUpdated?.Invoke(this, e);
        }

        public void DeleteById(object id)
        {
            DBItem row = LoadItemById(id);
            if (row != null)
                row.Delete();
        }

        public abstract IEnumerable<DBItem> GetChangedItems();

        public virtual bool SaveItem(DBItem item, DBTransaction transaction)
        {
            if (item.UpdateState == DBUpdateState.Default || (item.UpdateState & DBUpdateState.Commit) == DBUpdateState.Commit)
            {
                if (!item.Attached)
                    Add(item);
                return false;
            }

            if (item.UpdateState == DBUpdateState.Insert)
            {
                if (StampKey != null)
                    item.Stamp = DateTime.Now;
                if (DateKey != null)
                    item.Date = DateTime.Now;
                if (IsLoging && StatusKey != null && item.GetType().Name != "DocumentLog")
                    item.Status = DBStatus.New;
            }
            else if ((item.UpdateState & DBUpdateState.Update) == DBUpdateState.Update)
            {
                if (StampKey != null)
                    item.Stamp = DateTime.Now;
                if (IsLoging && StatusKey != null && item.Status == DBStatus.Actual && !item.Changed(StatusKey) && !item.Changed(AccessKey) && item.GetType().Name != "DocumentLog")
                    item.Status = DBStatus.Edit;
            }

            if (!item.Attached)
                Add(item);

            if (transaction.Reference && (item.UpdateState & DBUpdateState.Delete) != DBUpdateState.Delete)
            {
                foreach (var column in Columns.GetIsReference())
                {
                    if (column.ColumnType == DBColumnTypes.Default)
                    {
                        var refItem = item.GetCache(column) as DBItem;
                        if (refItem == null && item.GetValue(column) != null)
                        {
                            refItem = item.GetReference(column) as DBItem;
                        }
                        if (refItem != null && refItem != item)
                        {
                            if (refItem.IsChanged)
                                refItem.Save(transaction.GetSubTransaction(refItem.Table.Schema.Connection));
                            if (item.GetValue(column) == null)
                                item.SetValue(refItem.PrimaryId, column);
                        }
                    }
                }
            }

            transaction.Rows.Add(item);
            var args = new DBItemEventArgs(item) { Transaction = transaction };
            args.Columns = item.GetChangeKeys().ToList();

            if (item.OnUpdating(args))
            {
                DBCommand dmlCommand = null;

                if (item.UpdateState == DBUpdateState.Insert)
                {
                    if (PrimaryKey != null && item.PrimaryId == null && Schema.System != DBSystem.SQLite)
                    {
                        if (dmlInsertSequence == null)
                            dmlInsertSequence = DBCommand.Build(this, comInsert, DBCommandTypes.InsertSequence, Columns);
                        dmlCommand = dmlInsertSequence;
                    }
                    else
                    {
                        item.GenerateId(transaction);
                        if (dmlInsert == null)
                            dmlInsert = DBCommand.Build(this, comInsert, DBCommandTypes.Insert, Columns);
                        dmlCommand = dmlInsert;
                    }
                }
                else if ((item.UpdateState & DBUpdateState.Delete) == DBUpdateState.Delete)
                {
                    if (dmlDelete == null)
                        dmlDelete = DBCommand.Build(this, comDelete, DBCommandTypes.Delete);
                    dmlCommand = dmlDelete;
                }
                else if ((item.UpdateState & DBUpdateState.Update) == DBUpdateState.Update)
                {
                    //if (dmlUpdate == null)
                    dmlCommand = DBCommand.Build(this, comUpdate, DBCommandTypes.Update, args.Columns);
                    if (dmlCommand.Text.Length == 0)
                    {
                        item.Accept();
                        return true;
                    }
                }
                var command = transaction.AddCommand(dmlCommand.Text, dmlCommand.Type);
                dmlCommand.FillCommand(command, item);

                var result = transaction.ExecuteQuery(command, dmlCommand == dmlInsertSequence ? DBExecuteType.Scalar : DBExecuteType.NoReader);
                if (!(result is Exception))
                {
                    transaction.DbConnection.System.UploadCommand(item, command);
                    if (PrimaryKey != null && item.PrimaryId == null)
                        item[PrimaryKey] = result;
                    if (LogTable != null)
                    {
                        var subTransaction = transaction.GetSubTransaction(LogTable.Schema.Connection);
                        args.LogItem = new DBLogItem(item);
                        args.LogItem.Save(subTransaction);
                    }
                    item.OnUpdated(args);
                    item.UpdateState |= DBUpdateState.Commit;
                    return true;
                }
            }
            return false;
        }

        public void Save(IList rows = null, DBTransaction transaction = null)
        {
            if (rows == null)
                rows = GetChangedItems().ToList();
            if (rows.Count == 0)
                return;

            ListHelper.QuickSort(rows, new InvokerComparer(typeof(DBItem), nameof(DBItem.UpdateState)));

            var temp = transaction ?? new DBTransaction(Schema.Connection);
            try
            {
                foreach (DBItem row in rows)
                    SaveItem(row, temp);
                if (transaction == null)
                    temp.Commit();
            }
            finally
            {
                if (transaction == null)
                    temp.Dispose();
            }
        }

        public int GetRowCount(DBTransaction transaction, string @where)
        {
            var command = transaction.AddCommand(BuildQuery(@where, null, "count(*)"));
            object val = transaction.ExecuteQuery(command, DBExecuteType.Scalar);
            return val is Exception ? -1 : int.Parse(val.ToString());
        }
        #region IComparable Members

        int IComparable.CompareTo(object obj)
        {
            if (obj is DBTable)
            {
                DBTable ts = obj as DBTable;
                return string.Compare(this.Name, ts.Name);
            }
            return 1;
        }

        #endregion

        public DBColumn ParseColumn(string name)
        {
            DBTable table = this;
            int s = 0, i = name.IndexOf('.');
            while (i > 0)
            {
                DBColumn column = table.Columns[name.Substring(s, i - s)];
                if (column == null)
                    break;
                if (column.IsReference)
                    table = column.ReferenceTable;
                s = i + 1;
                i = name.IndexOf('.', s);
            }
            return table.Columns[name.Substring(s)];
        }

        public abstract void Clear();

        public void RejectChanges()
        {
            var rows = GetChangedItems().ToList();
            for (int i = 0; i < rows.Count; i++)
                rows[i].Reject();
        }

        public void AcceptChanges()
        {
            var rows = GetChangedItems().ToList();
            for (int i = 0; i < rows.Count; i++)
                rows[i].Accept();
        }

        [Browsable(false)]
        public abstract IDBTableView DefaultItemsView { get; }

        public abstract IDBTableView CreateItemsView(string query = "", DBViewKeys mode = DBViewKeys.None, DBStatus filter = DBStatus.Empty);

        public abstract DBItem NewItem(DBUpdateState state = DBUpdateState.Insert, bool def = true);

        public IEnumerable<DBColumn> ParseColumns(ICollection<string> columns)
        {
            foreach (string column in columns)
            {
                var dbColumn = ParseColumn(column);
                if (dbColumn != null)
                    yield return dbColumn;
            }
        }

        #region Use Index

        public IEnumerable<object> SelectQuery(DBItem item, QQuery query, CompareType compare)
        {
            if (query.Columns.Count == 0)
            {
                query.Columns.Add(new QColumn(query.Table.PrimaryKey));
            }
            if (query.IsRefence && item != null)
            {
                foreach (QParam param in query.AllParameters)
                {
                    if (param.ValueRight is QColumn)
                    {
                        DBColumn column = ((QColumn)param.ValueRight).Column;
                        if (column != null && column.Table == this)
                            ((QColumn)param.ValueRight).Temp = item[column];
                    }
                }
            }
            var objects = new List<object>();
            foreach (DBItem row in query.Select())
            {
                object value = query.Columns[0].GetValue(row);
                int index = ListHelper.BinarySearch<object>(objects, value, null);
                if (index < 0)
                {
                    objects.Insert(-index - 1, value);
                    yield return value;
                }
            }
        }
        public abstract IEnumerable SelectItems(DBColumn column, object val, CompareType comparer);

        public abstract IEnumerable SelectItems(string qQuery);

        public abstract IEnumerable SelectItems(QQuery qQuery);

        public bool CheckItem(DBItem item, QItemList<QParam> parameters)
        {
            bool result = true;
            for (int i = 0; i < parameters.Count; i++)
            {
                var param = parameters[i];
                if (i == parameters.Count - 1 && !result && param.Logic.Type == LogicTypes.And)
                    break;
                bool check = CheckItem(item, param);

                if (i == 0)
                    result = check;
                else if (param.Logic.Type == LogicTypes.Or)
                    result = param.Logic.Not ? result | !check : result | check;
                else if (param.Logic.Type == LogicTypes.And)
                    result = param.Logic.Not ? result & !check : result & check;
            }
            return result;
        }

        public bool CheckItem(DBItem item, QQuery query)
        {
            return CheckItem(item, query.Parameters);
        }

        public bool CheckItem(DBItem item, QParam param)
        {
            bool result = false;
            if (param.Parameters.Count == 0)
            {
                if (param.ValueLeft == null || param.ValueRight == null)
                {
                    result = true;
                }
                else
                {
                    result = CheckItem(item, param.ValueLeft.GetValue(item), param.ValueRight.GetValue(item), param.Comparer);
                }
            }
            else
            {
                result = CheckItem(item, param.Parameters);
            }
            return result;
        }

        public bool CheckItem(DBItem item, string column, object val, CompareType comparer)
        {
            object val1 = null;
            DBColumn dbColumn = ParseColumn(column);
            if (dbColumn == null)
                val1 = EmitInvoker.GetValue(typeof(DBItem), column, item);
            else
                val1 = item[dbColumn];
            return CheckItem(item, val1, dbColumn.ParseValue(val), comparer);
        }

        public bool CheckItem(DBItem item, object val1, object val2, CompareType comparer)
        {
            if (item == null)
                return false;
            if (val1 == null)
                return comparer.Type == CompareTypes.Is ? !comparer.Not : val2 == null;
            else if (val2 == null)
                return comparer.Type == CompareTypes.Is ? comparer.Not : false;
            if (val1 is QQuery)
                val1 = SelectQuery(item, (QQuery)val1, comparer);
            if (val2 is QQuery)
                val2 = SelectQuery(item, (QQuery)val2, comparer);
            if (val1 is Enum)
                val1 = (int)val1;
            if (val2 is Enum)
                val2 = (int)val1;
            switch (comparer.Type)
            {
                //case CompareTypes.Is:
                //    return val1.Equals(DBNull.Value) ? !comparer.Not : comparer.Not;
                case CompareTypes.Equal:
                    return ListHelper.Equal(val1, val2, false) ? !comparer.Not : comparer.Not;
                case CompareTypes.Like:
                    var r = val2 is Regex ? (Regex)val2 : Helper.BuildLike(val2.ToString());
                    return r.IsMatch(val1.ToString()) ? !comparer.Not : comparer.Not;
                case CompareTypes.In:
                    if (val2 is string)
                        val2 = val2.ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var list = val2 as IEnumerable;
                    if (list != null)
                    {
                        foreach (object s in list)
                        {
                            object comp = s;
                            if (comp is QItem)
                                comp = ((QItem)comp).GetValue(item);
                            if (comp is string)
                                comp = ((string)comp).Trim(' ', '\'');
                            if (comp.Equals(val1) && !comparer.Not)
                                return true;
                        }
                    }
                    return comparer.Not;
                case CompareTypes.Between:
                    var between = val2 as QBetween;
                    if (between == null)
                        throw new Exception("Expect QBetween but Get " + val2 == null ? "null" : val2.GetType().FullName);
                    return ListHelper.Compare(val1, between.Min.GetValue(item), null, false) >= 0
                                     && ListHelper.Compare(val1, between.Max.GetValue(item), null, false) <= 0;
                default:
                    bool f = false;
                    int rez = ListHelper.Compare(val1, val2, null, false);
                    switch (comparer.Type)
                    {
                        case CompareTypes.Greater:
                            f = rez > 0;
                            break;
                        case CompareTypes.GreaterOrEqual:
                            f = rez >= 0;
                            break;
                        case CompareTypes.Less:
                            f = rez < 0;
                            break;
                        case CompareTypes.LessOrEqual:
                            f = rez <= 0;
                            break;
                        default:
                            break;
                    }
                    return f;
            }
        }

        #endregion

        public void GetAllChildTables(List<DBTable> parents)
        {
            foreach (var table in GetChildTables())
            {
                if (table != this && !parents.Contains(table))
                {
                    parents.Add(table);
                    table.GetAllChildTables(parents);
                }
            }
        }

        public IEnumerable<DBTable> GetChildTables()
        {
            foreach (DBForeignKey rel in GetChildRelations())
            {
                yield return rel.Table;

                if (rel.Table is IDBVirtualTable)
                    yield return ((IDBVirtualTable)rel.Table).BaseTable;
            }
        }

        public void RemoveVirtual(IDBVirtualTable view)
        {
            virtualViews.Remove(view);
        }

        public void AddVirtual(IDBVirtualTable view)
        {
            virtualViews.Add(view);
        }

        [Browsable(false)]
        public IEnumerable<DBForeignKey> GetChildRelations()
        {
            if (Schema == null)
                yield break;
            foreach (var table in Schema.Tables)
            {
                foreach (var foreign in table.Foreigns.GetByReference(this))
                    yield return foreign;
            }
        }

        public void GetAllParentTables(List<DBTable> parents)
        {
            foreach (DBTable table in GetParentTables())
            {
                if (table != this && !parents.Contains(table))
                {
                    parents.Add(table);
                    table.GetAllParentTables(parents);
                }
            }
            //return l;
        }

        public IEnumerable<DBTable> GetParentTables()
        {
            foreach (var item in Foreigns)
            {
                yield return item.ReferenceTable;

                if (item.ReferenceTable is IDBVirtualTable)
                    yield return ((IDBVirtualTable)item.ReferenceTable).BaseTable;
            }
        }

        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Builds the query.
        /// </summary>
        /// <returns>
        /// The query.
        /// </returns>
        /// <param name='whereFilter'>
        /// Where filter.
        /// </param>
        /// <param name='cols'>
        /// Cols.
        /// </param>
        public string BuildQuery(string whereFilter, IEnumerable cols, string function = null)
        {
            var select = new StringBuilder("select ");
            if (!string.IsNullOrEmpty(function))
            {
                select.Append(function);
                select.Append(" ");
            }
            else
            {
                if (cols == null)
                    cols = Columns;// query += "*";// cols = this.columns as IEnumerable;

                bool f = false;
                foreach (DBColumn cs in cols)
                {
                    string temp = BuildQueryColumn(cs, "");
                    if (!string.IsNullOrEmpty(temp))
                    {
                        if (f)
                            select.Append(", ");
                        else
                            f = true;
                        select.Append(temp);
                    }
                }

                if (select.ToString() == "select ")
                    select.Append(" * ");
            }
            string vquery = Query;
            if (!string.IsNullOrEmpty(vquery))
            {
                if (whereFilter.IndexOf(vquery, StringComparison.OrdinalIgnoreCase) >= 0)
                    vquery = string.Empty;
                else
                    vquery = (whereFilter.Length != 0 ? " and (" : " where ") + vquery + (whereFilter.Length != 0 ? ")" : string.Empty);
            }
            select.Append(" from ");
            if (!string.IsNullOrEmpty(Schema?.Connection?.Schema))
                select.Append(Schema.Connection.Schema + ".");
            select.Append(SqlName);
            select.Append(" ");
            select.Append(whereFilter);
            select.Append(vquery);
            return select.ToString();
        }

        public string BuildQueryColumn(DBColumn cs, string seporator)
        {
            if (cs.ColumnType == DBColumnTypes.Internal || cs.ColumnType == DBColumnTypes.Expression)
                return string.Empty;
            else if (cs.ColumnType == DBColumnTypes.Query && cs.Table.Type != DBTableType.View)
                return string.Format("({0}) as {1} {2}", cs.Query, cs.Name, seporator);
            else
                return cs.Name + seporator;
        }

        public string DetectQuery(string whereText, IEnumerable cols = null)
        {
            string rez;
            if (string.IsNullOrEmpty(whereText) || whereText.Trim().StartsWith("where ", StringComparison.OrdinalIgnoreCase))
                rez = BuildQuery(whereText, cols);
            else
                rez = whereText;

            return rez;
        }

        public override string FormatSql(DDLType ddlType)
        {
            var ddl = new StringBuilder();
            Schema?.Connection?.System.Format(ddl, this, ddlType);
            return ddl.ToString();
        }

        public void SaveFile()
        {
            string fileName = Path.Combine("schems", Schema.Name, Name + ".rws");
            SaveFile(fileName);
        }

        public void SaveFile(string fileName)
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
                {
                    var map = DBItemBinarySerialize.WriteColumns(writer, this);
                    foreach (DBItem row in this)
                    {
                        DBItemBinarySerialize.Write(writer, row, map);
                    }

                    DBItemBinarySerialize.WriteSeparator(writer, DBRowBinarySeparator.End);
                }
            }
        }

        public void LoadFile()
        {
            string fileName = Path.Combine("schems", Schema.Name, Name + ".rws");
            LoadFile(fileName);
        }

        public void LoadFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName))
                return;
            using (var file = File.Open(fileName, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new BinaryReader(file))
                {
                    var map = DBItemBinarySerialize.ReadColumns(reader, this);
                    while (true)
                    {
                        DBRowBinarySeparator sep = DBItemBinarySerialize.PeekSeparator(reader);
                        if (sep == DBRowBinarySeparator.End)
                            break;
                        DBItem row = NewItem(DBUpdateState.Default, false);
                        DBItemBinarySerialize.Read(reader, row, map);
                        Add(row);
                        row.Accept();
                    }
                }
            }
        }

        public override object Clone()
        {
            var table = (DBTable)EmitInvoker.CreateObject(GetType(), true);
            table.name = name;
            //bc.bname = this.bname;
            table.query = query;
            table.caching = caching;
            table.type = type;
            table.groupName = groupName;
            table.sequenceName = sequenceName;
            table.schema = schema;
            foreach (var @group in ColumnGroups)
            {
                var newCol = (DBColumnGroup)@group.Clone();
                table.ColumnGroups.Add(newCol);
            }
            foreach (var column in Columns)
            {
                var newCol = (DBColumn)column.Clone();
                table.Columns.Add(newCol);
                if (column.LocaleInfo.Count > 0)
                {
                    newCol.LocaleInfo.Add(column.LocaleInfo[0].Value, column.LocaleInfo[0].Culture);
                }
            }
            return table;
        }

        internal void ClearCache()
        {
            dmlInsert = null;
            dmlInsertSequence = null;
            dmlDelete = null;
            accessKey = DBColumn.EmptyKey;
            primaryKey = DBColumn.EmptyKey;
            dateKey = DBColumn.EmptyKey;
            stampKey = DBColumn.EmptyKey;
            codeKey = DBColumn.EmptyKey;
            typeKey = DBColumn.EmptyKey;
            groupKey = DBColumn.EmptyKey;
            stateKey = DBColumn.EmptyKey;
            imageKey = DBColumn.EmptyKey;
        }

        public DBTable GenerateLogTable()
        {
            if (LogTable == null)
            {
                LogTable = new DBLogTable { BaseTable = this };
                Schema.Tables.Add(logTable);
            }
            else
            {
                LogTable.BaseTable = this;
            }
            return logTable;
        }

        private DBForeignKey GenerateRelation(DBColumn column, DBColumn reference)
        {
            var relation = Foreigns.GetByColumns(column, reference);
            if (relation == null)
            {
                relation = new DBForeignKey() { Column = column, Reference = reference };
                relation.GenerateName();
                Foreigns.Add(relation);
            }
            return relation;
            //List<DBTable> views = reference.Table.GetChilds();
            //foreach (DBTable view in views)
            //    GenerateRelation(stable, scolumn, view.PrimaryKey);
        }

        public DBSequence GenerateSequence()
        {
            var sname = $"seq_{Name}";
            var sequence = Schema.Sequences[sname];
            if (sequence == null)
            {
                sequence = new DBSequence(sname);
            }
            return sequence;
        }

        public void GenerateColumns(DBTableInfo tableInfo)
        {
            foreach (var columnInfo in tableInfo.Columns)
            {
                string name = columnInfo.Name;
                DBColumn column = this.InitColumn(columnInfo.Name);
                //if (col.Order == 1)
                //    col.IsPrimaryKey = true;
                if (name.Equals(Name, StringComparison.OrdinalIgnoreCase))
                    column.Keys |= DBColumnKeys.Primary;
                if (name.Equals("code", StringComparison.OrdinalIgnoreCase))
                    column.Keys |= DBColumnKeys.Code;
                if (name.Equals("goupid", StringComparison.OrdinalIgnoreCase))
                    column.Keys |= DBColumnKeys.Group;
                if (name.StartsWith("name", StringComparison.OrdinalIgnoreCase))
                    column.Keys |= DBColumnKeys.View;
                if (columnInfo.NotNull)
                    column.Keys |= DBColumnKeys.Notnull;
                column.DefaultValue = columnInfo.Default;

                string data = columnInfo.DataType.ToUpper();
                if (data.Equals("BLOB", StringComparison.OrdinalIgnoreCase) ||
                    data.Equals("RAW", StringComparison.OrdinalIgnoreCase) ||
                    data.Equals("VARBINARY", StringComparison.OrdinalIgnoreCase))
                {
                    column.DataType = typeof(byte[]);
                    if (!string.IsNullOrEmpty(columnInfo.Length))
                        column.Size = int.Parse(columnInfo.Length);
                }
                else if (data.IndexOf("DATE", StringComparison.OrdinalIgnoreCase) != -1 || data.IndexOf("TIMESTAMP", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    column.DataType = typeof(DateTime);
                }
                else if (data == "NUMBER" || data == "DECIMAL" || data == "NUMERIC")
                {
                    column.DataType = typeof(decimal);
                    if (!string.IsNullOrEmpty(columnInfo.Precision))
                        column.Size = int.Parse(columnInfo.Precision);
                    if (!string.IsNullOrEmpty(columnInfo.Scale))
                        column.Scale = int.Parse(columnInfo.Scale);
                }
                else if (data == "DOUBLE")
                    column.DataType = typeof(double);
                else if (data == "FLOAT")
                    column.DataType = typeof(double);
                else if (data == "INT" || data == "INTEGER")
                    column.DataType = typeof(int);
                else if (data == "BIT")
                    column.DataType = typeof(bool);
                else
                {
                    column.DataType = typeof(string);
                    //col.DBDataType = DBDataType.Clob;
                    if (!string.IsNullOrEmpty(columnInfo.Length))
                    {
                        column.Size = int.Parse(columnInfo.Length);
                        column.DBDataType = DBDataType.String;
                    }
                }
            }
        }

        public string GetRowText(object id)
        {
            return GetRowText(id, Columns.GetIsView());
        }

        public string GetRowText(object id, IEnumerable<DBColumn> parameters)
        {
            return GetRowText(id, parameters, false, " - ");
        }

        public string GetRowText(object id, IEnumerable<DBColumn> parametrs, bool showColumn, string separator)
        {
            return LoadItemById(id)?.GetRowText(parametrs, showColumn, separator) ?? "<null>";
        }

        public string GetRowText(object id, bool allColumns, bool showColumn, string separator)
        {
            return LoadItemById(id)?.GetRowText((allColumns ? (IEnumerable<DBColumn>)Columns : Columns.GetIsView()), showColumn, separator);
        }

        public QEnum GetStatusEnum(DBStatus status)
        {
            var qlist = new QEnum();
            if ((status & DBStatus.Actual) == DBStatus.Actual)
                qlist.Items.Add(new QValue((int)DBStatus.Actual));
            if ((status & DBStatus.New) == DBStatus.New)
                qlist.Items.Add(new QValue((int)DBStatus.New));
            if ((status & DBStatus.Edit) == DBStatus.Edit)
                qlist.Items.Add(new QValue((int)DBStatus.Edit));
            if ((status & DBStatus.Delete) == DBStatus.Delete)
                qlist.Items.Add(new QValue((int)DBStatus.Delete));
            if ((status & DBStatus.Archive) == DBStatus.Archive)
                qlist.Items.Add(new QValue((int)DBStatus.Archive));
            if ((status & DBStatus.Error) == DBStatus.Error)
                qlist.Items.Add(new QValue((int)DBStatus.Error));

            return qlist;
        }

        public QParam GetStatusParam(DBStatus status)
        {
            if (StatusKey != null && status != 0 && status != DBStatus.Empty)
            {
                return new QParam()
                {
                    ValueLeft = new QColumn(StatusKey),
                    Comparer = CompareType.In,
                    ValueRight = GetStatusEnum(status)
                };
            }
            return null;
        }

        public int GetTypeIndex(Type type)
        {
            foreach (var entry in ItemTypes)
            {
                if (entry.Value.Type == type)
                    return entry.Key;
            }
            return -1;
        }

        public QParam GetTypeParam(Type type)
        {
            var typeIndex = GetTypeIndex(type);
            if (ItemTypeKey != null && typeIndex > 0)
            {
                return new QParam()
                {
                    ValueLeft = new QColumn(ItemTypeKey),
                    Comparer = CompareType.Equal,
                    ValueRight = new QValue(typeIndex, ItemTypeKey)
                };
            }
            return null;
        }

        public DBColumnGroup InitColumnGroup(string code)
        {
            DBColumnGroup cs = null;
            cs = ColumnGroups[code];
            if (cs == null)
            {
                cs = new DBColumnGroup(code);
                ColumnGroups.Add(cs);
            }
            return cs;
        }

        public DBColumn InitColumn(string code)
        {
            DBColumn cs = null;
            cs = Columns[code];
            if (cs == null)
            {
                cs = new DBColumn(code);
                Columns.Add(cs);
            }
            return cs;
        }
    }
}
