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
using Newtonsoft.Json;
using System.Collections.Specialized;

namespace DataWF.Data
{

    public abstract class DBTable : DBSchemaItem, ICollection<DBItem>, IComparable, IAccessable, IDisposable
    {

        private static Dictionary<Type, TableAttribute> cacheTables = new Dictionary<Type, TableAttribute>();
        private static Dictionary<Type, ItemTypeAttribute> cacheItemTypes = new Dictionary<Type, ItemTypeAttribute>();

        public static void ClearChache()
        {
            cacheTables.Clear();
            cacheItemTypes.Clear();
        }

        public static TableAttribute GetTableAttributeInherit(Type type)
        {
            var tableAttribute = GetTableAttribute(type);
            while (tableAttribute == null && type != null)
            {
                type = type.BaseType;
                tableAttribute = type == null ? null : GetTableAttribute(type);
            }
            return tableAttribute;
        }

        public static TableAttribute GetTableAttribute<T>()
        {
            return GetTableAttribute(typeof(T));
        }

        public static TableAttribute GetTableAttribute(Type type)
        {
            if (!cacheTables.TryGetValue(type, out TableAttribute table))
            {
                table = type.GetCustomAttribute<TableAttribute>(false);
                table?.Initialize(type);
                cacheTables[type] = table;
            }
            if (table == null)
            {
                var itemType = GetItemTypeAttribute(type);
                table = itemType?.TableAttribute;
            }
            return table;
        }

        public static ItemTypeAttribute GetItemTypeAttribute(Type type)
        {
            if (!cacheItemTypes.TryGetValue(type, out ItemTypeAttribute itemType))
            {
                itemType = type.GetCustomAttribute<ItemTypeAttribute>(false);
                if (itemType != null)
                {
                    itemType.Initialize(type);
                }
                cacheItemTypes[type] = itemType;
            }
            return itemType;
        }

        public static DBTable<T> GetTable<T>(DBSchema schema = null, bool generate = false) where T : DBItem, new()
        {
            return (DBTable<T>)GetTable(typeof(T), schema, generate);
        }

        public static DBTable GetTable(Type type, DBSchema schema = null, bool generate = false)
        {
            var tableAttribute = GetTableAttribute(type);
            if (tableAttribute != null)
            {
                if (tableAttribute.Table == null && generate)
                    tableAttribute.Generate(schema);
                var itemAttribute = GetItemTypeAttribute(type);
                if (itemAttribute != null)
                    return itemAttribute.Table;
                return tableAttribute.Table;
            }
            return null;
        }

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
        private int block = 500;
        internal object locker = new object();
        protected List<IDBVirtualTable> virtualTables = new List<IDBVirtualTable>(0);
        private DBItemType itemType;

        protected DBTable(string name = null) : base(name)
        {
            ColumnGroups = new DBColumnGroupList(this);
            Columns = new DBColumnList<DBColumn>(this);
            Indexes = new DBIndexList(this);
            Constraints = new DBConstraintList<DBConstraint>(this);
            Foreigns = new DBForeignList(this);
        }

        [Browsable(false)]
        public string LogTableName { get; set; }

        [XmlIgnore, JsonIgnore]
        public virtual DBLogTable LogTable
        {
            get
            {
                return logTable
                               ?? (logTable = (DBLogTable)Schema?.LogSchema?.Tables[LogTableName]
                               ?? (DBLogTable)Schema?.Tables[LogTableName]);
            }
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

        //[Browsable(false), XmlIgnore, JsonIgnore]
        //public TableAttribute Info { get; protected set; }

        public DBColumn ParseProperty(string property)
        {
            return Columns.GetByProperty(property) ?? Foreigns.GetByProperty(property)?.Column;
        }

        [Browsable(false)]
        public Dictionary<int, DBItemType> ItemTypes { get; set; } = new Dictionary<int, DBItemType>();

        [Browsable(false), XmlIgnore, JsonIgnore]
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
                    OnPropertyChanged(nameof(Query), DDLType.Alter);
                }
            }
        }

        [Browsable(false)]
        public DBSystem System
        {
            get { return Schema?.System ?? DBSystem.Default; }
        }

        [Browsable(false)]
        public virtual int BlockSize
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

        [Browsable(false), Category("Database")]
        public string ComInsert
        {
            get { return comInsert; }
            set { comInsert = value; }
        }

        [Browsable(false), Category("Database")]
        public string ComUpdate
        {
            get { return comUpdate; }
            set { comUpdate = value; }
        }

        [Browsable(false), Category("Database")]
        public string ComDelete
        {
            get { return comDelete; }
            set { comDelete = value; }
        }

        [Browsable(false), Category("Group")]
        public virtual string GroupName
        {
            get { return groupName; }
            set
            {
                if (groupName == value)
                    return;
                groupName = value;
                tableGroup = null;
                OnPropertyChanged(nameof(GroupName));
            }
        }

        [XmlIgnore, JsonIgnore, Category("Group")]
        public DBTableGroup Group
        {
            get
            {
                if (tableGroup == null && GroupName != null)
                    tableGroup = Schema?.TableGroups[GroupName];
                return tableGroup;
            }
            set
            {
                tableGroup = value;
                GroupName = value?.Name;
            }
        }

        [Browsable(false)]
        public virtual string SequenceName
        {
            get { return sequenceName; }
            set
            {
                if (sequenceName != value)
                {
                    sequenceName = value;
                    OnPropertyChanged(nameof(SequenceName));
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public DBSequence Sequence
        {
            get { return cacheSequence ?? (cacheSequence = Schema?.Sequences[SequenceName]); }
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

        [Browsable(false), Category("Keys")]
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

        [Browsable(false), Category("Keys")]
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

        [Browsable(false), Category("Keys")]
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

        [Browsable(false), Category("Keys")]
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

        [Browsable(false), Category("Keys")]
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

        [Browsable(false), Category("Keys")]
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

        [Browsable(false), Category("Keys")]
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

        [Browsable(false), Category("Keys")]
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

        [Browsable(false), Category("Keys")]
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
                OnPropertyChanged(nameof(Type), DDLType.Alter);
            }
        }

        [XmlIgnore, JsonIgnore, Category("Database")]
        public virtual bool IsLoging
        {
            get { return !string.IsNullOrEmpty(LogTableName); }
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
                OnPropertyChanged(nameof(IsCaching));
            }
        }

        [Category("Column")]
        public virtual DBColumnList<DBColumn> Columns { get; set; }

        [Category("Column")]
        public virtual DBColumnGroupList ColumnGroups { get; set; }

        [Category("Performance")]
        public virtual DBConstraintList<DBConstraint> Constraints { get; set; }

        [Category("Performance")]
        public virtual DBIndexList Indexes { get; set; }

        [Category("Performance")]
        public virtual DBForeignList Foreigns { get; set; }

        public abstract int Count { get; }

        [Browsable(false)]
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

        public abstract void OnItemChanged(DBItem item, string proeprty, DBColumn column, object value);

        public abstract void OnItemChanging(DBItem item, string proeprty, DBColumn column, object value);

        public abstract void Trunc();

        public IEnumerable<DBTable> GetChilds()
        {
            foreach (var item in virtualTables)
            {
                yield return (DBTable)item;
            }
        }

        public void FillReferenceBlock(IDbCommand command)
        {
            foreach (var column in Columns.GetIsReference())
            {
                if ((column.Keys & DBColumnKeys.Group) != DBColumnKeys.Group && column.ReferenceTable != this && !column.ReferenceTable.IsSynchronized)
                {
                    var sub = DBCommand.CloneCommand(command, column.ReferenceTable.BuildQuery(string.Format("where {0} in (select {1} {2})",
                                  column.ReferenceTable.PrimaryKey.Name,
                                  column.Name,
                                  command.CommandText.Substring(command.CommandText.IndexOf(" from ", StringComparison.OrdinalIgnoreCase))), null));
                    column.ReferenceTable.LoadItems(sub);
                }
            }
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

        public abstract IEnumerable LoadItems(IDbCommand command, DBLoadParam param = DBLoadParam.None, IDBTableView synch = null);

        public abstract DBItem LoadItemByCode(string code, DBColumn column, DBLoadParam param);

        public abstract DBItem LoadItemById(object id, DBLoadParam param = DBLoadParam.Load, IEnumerable cols = null);

        public abstract void ReloadItem(object id);

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

        public virtual bool SaveItem(DBItem item)
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
                    item.DateCreate = DateTime.Now;
                if (IsLoging && StatusKey != null && !item.Changed(StatusKey))
                    item.Status = DBStatus.New;
            }
            else if ((item.UpdateState & DBUpdateState.Update) == DBUpdateState.Update)
            {
                if (StampKey != null)
                    item.Stamp = DateTime.Now;
                if (IsLoging && StatusKey != null && item.Status == DBStatus.Actual && !item.Changed(StatusKey) && !item.Changed(AccessKey))
                    item.Status = DBStatus.Edit;
            }

            if (!item.Attached)
                Add(item);

            var transaction = DBTransaction.GetTransaction(item, Schema.Connection);

            try
            {
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
                                    refItem.Save();
                                if (item.GetValue(column) == null)
                                    item.SetValue(refItem.PrimaryId, column);
                            }
                        }
                    }
                }

                transaction.Rows.Add(item);
                var args = new DBItemEventArgs(item) { Transaction = transaction };

                if (!item.OnUpdating(args))
                    return false;
                args.Columns = item.GetChangeKeys().ToList();
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
                        item.GenerateId();
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
                transaction.DbConnection.System.UploadCommand(item, command);
                if (PrimaryKey != null && item.PrimaryId == null)
                {
                    item[PrimaryKey] = result;
                    Sequence.SetCurrent(result);
                }

                if (LogTable != null)
                {
                    args.LogItem = new DBLogItem(item);
                    DBService.OnLogItem(args);
                    args.LogItem.Save();
                }
                item.OnUpdated(args);
                item.UpdateState |= DBUpdateState.Commit;
                if (transaction.Owner == item)
                    transaction.Commit();
                return true;
            }
            finally
            {
                if (transaction.Owner == item)
                    transaction.Dispose();
            }
        }

        public virtual int NextHash()
        {
            return Interlocked.Increment(ref Hash);
        }

        public void Save(IList rows = null)
        {
            if (rows == null)
                rows = GetChangedItems().ToList();

            if (rows.Count > 0)
            {
                ListHelper.QuickSort(rows, new InvokerComparer(typeof(DBItem), nameof(DBItem.UpdateState)));

                var transaction = DBTransaction.GetTransaction(this, Schema.Connection);
                try
                {
                    foreach (DBItem row in rows)
                        row.Save();

                    if (transaction.Owner == this)
                        transaction.Commit();
                }
                finally
                {
                    if (transaction.Owner == this)
                        transaction.Dispose();
                }
            }

            Sequence?.Save();
        }

        public int GetRowCount(DBTransaction transaction, string @where)
        {
            object val = transaction.ExecuteQuery(transaction.AddCommand(BuildQuery(@where, null, "count(*)")), DBExecuteType.Scalar);
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

        public static IDBTableView CreateView(Type type)
        {
            return (IDBTableView)EmitInvoker.Initialize(typeof(DBTableView<>).MakeGenericType(type), new Type[] { }, true).Create();
        }

        [Browsable(false)]
        public abstract IDBTableView DefaultItemsView { get; }

        public abstract IDBTableView CreateItemsView(string query = "", DBViewKeys mode = DBViewKeys.None, DBStatus filter = DBStatus.Empty);

        public virtual DBItem NewItem(DBUpdateState state = DBUpdateState.Insert, bool def = true, int typeIndex = 0)
        {
            var type = GetItemType(typeIndex);
            var item = (DBItem)type.Constructor.Create();
            if (item.Table == null)
            {
                item.Build(this, def);
            }
            item.update = state;
            return item;
        }

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
        public abstract IEnumerable SelectItems(DBColumn column, CompareType comparer, object val);

        public abstract IEnumerable SelectItems(string qQuery);

        public abstract IEnumerable SelectItems(QQuery qQuery);

        public bool CheckItem(DBItem item, QItemList<QParam> parameters)
        {
            bool first = true;
            bool result = true;
            foreach (var param in parameters)
            {
                if (!first && !result && param.Logic.Type == LogicTypes.And)
                    break;
                bool check = CheckItem(item, param);

                if (first)
                {
                    result = check;
                    first = false;
                }
                else if (param.Logic.Type == LogicTypes.Or)
                {
                    result |= param.Logic.Not ? !check : check;
                }
                else if (param.Logic.Type == LogicTypes.And)
                {
                    result &= param.Logic.Not ? !check : check;
                }
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
            virtualTables.Remove(view);
        }

        public void AddVirtual(IDBVirtualTable view)
        {
            virtualTables.Add(view);
        }

        public virtual IEnumerable<DBForeignKey> GetChildRelations()
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
                foreach (DBColumn column in cols)
                {
                    string temp = FormatQColumn(column);
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
            select.Append(FormatQTable());
            select.Append(" ");
            select.Append(whereFilter);
            select.Append(vquery);
            return select.ToString();
        }

        public string FormatQColumn(DBColumn column)
        {
            return System?.FormatQColumn(column);
        }

        public string FormatQTable()
        {
            return System?.FormatQTable(this);
        }

        public string CreateQuery(string whereText, IEnumerable cols = null)
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
            table.name = Name;
            //bc.bname = this.bname;
            table.query = Query;
            table.caching = caching;
            table.type = Type;
            table.groupName = GroupName;
            table.sequenceName = SequenceName;
            table.schema = Schema;
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

        public DBLogTable GenerateLogTable()
        {
            if (LogTable == null)
            {
                LogTable = new DBLogTable { BaseTable = this };
                LogTable.Schema.Tables.Add(LogTable);
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

        public void Generate(DBTableInfo tableInfo)
        {
            foreach (var columnInfo in tableInfo.Columns)
            {
                string name = columnInfo.Name;
                var column = InitColumn(columnInfo.Name);
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
                if (column.Container == null)
                    Columns.Add(column);
            }

            foreach (var constraintInfo in tableInfo.Constraints)
            {
                string name = constraintInfo.Name;
                var constraint = InitConstraint(name);
                //if (col.Order == 1)
                //    col.IsPrimaryKey = true;
                var column = Columns[constraintInfo.Column];
                if (column == null)
                    continue;
                constraint.Column = column;
                if (constraintInfo.Type.Equals("PRIMARY KEY", StringComparison.OrdinalIgnoreCase))
                {
                    constraint.Type = DBConstraintType.Primary;
                    column.Keys |= DBColumnKeys.Primary;
                }

                if (constraint.Container == null)
                    Constraints.Add(constraint);
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

        public virtual DBItemType GetItemType(int typeIndex)
        {
            return typeIndex == 0 ? ItemType : ItemTypes[typeIndex];
        }

        public virtual int GetTypeIndex(Type type)
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
            return Columns[code] ?? new DBColumn(code) { Table = this };
        }

        private DBConstraint InitConstraint(string name)
        {
            return Constraints[name] ?? new DBConstraint() { Table = this, Name = name };
        }

        public void GenerateDefaultColumns()
        {
            Columns.AddRange(new[]{
                new DBColumn { Name = "unid", Keys = DBColumnKeys.Primary, DBDataType = DBDataType.Int },
                new DBColumn { Name = "datec", Keys = DBColumnKeys.Date, DBDataType = DBDataType.DateTime },
                new DBColumn { Name = "dateu", Keys = DBColumnKeys.Stamp, DBDataType = DBDataType.DateTime },
                new DBColumn { Name = "stateid", Keys = DBColumnKeys.State, DBDataType = DBDataType.Decimal, Size = 28 },
                new DBColumn { Name = "access", Keys = DBColumnKeys.Access, DBDataType = DBDataType.Blob, Size = 2000 }
            });
        }
    }
}
