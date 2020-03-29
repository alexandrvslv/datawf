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
using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public abstract class DBTable : DBSchemaItem, IComparable, IDBTable
    {
        private static readonly Dictionary<Type, DBTable> cacheTables = new Dictionary<Type, DBTable>();
        private static readonly Dictionary<Type, TableGenerator> cacheTableAttributes = new Dictionary<Type, TableGenerator>();
        private static readonly Dictionary<Type, ItemTypeGenerator> cacheItemTypeAttributes = new Dictionary<Type, ItemTypeGenerator>();

        public static void ClearAttributeCache()
        {
            cacheTables.Clear();
            cacheTableAttributes.Clear();
            cacheItemTypeAttributes.Clear();
        }

        public static TableGenerator GetTableAttributeInherit(Type type)
        {
            var tableAttribute = GetTableAttribute(type);
            while (tableAttribute == null && type != null)
            {
                type = type.BaseType;
                tableAttribute = type == null ? null : GetTableAttribute(type);
            }
            return tableAttribute;
        }

        public static TableGenerator GetTableAttribute<T>()
        {
            return GetTableAttribute(typeof(T));
        }

        public static TableGenerator GetTableAttribute(Type type)
        {
            if (!cacheTableAttributes.TryGetValue(type, out var table))
            {
                var tableAttribute = type.GetCustomAttribute<TableAttribute>(false);
                if (tableAttribute is LogTableAttribute)
                {
                    table = new LogTableGenerator() { Attribute = tableAttribute };
                    table.Initialize(type);
                }
                else if (tableAttribute is TableAttribute)
                {
                    table = new TableGenerator() { Attribute = tableAttribute };
                    table.Initialize(type);
                }
                cacheTableAttributes[type] = table;
            }
            if (table == null)
            {
                var itemType = GetItemTypeAttribute(type);
                table = itemType?.TableAttribute;
            }
            return table;
        }

        public static ItemTypeGenerator GetItemTypeAttribute(Type type)
        {
            if (!cacheItemTypeAttributes.TryGetValue(type, out var itemType))
            {
                var itemTypeAttribute = type.GetCustomAttribute<ItemTypeAttribute>(false);
                if (itemTypeAttribute is LogItemTypeAttribute)
                {
                    itemType = new LogItemTypeGenerator { Attribute = itemTypeAttribute };
                    itemType.Initialize(type);
                }
                else if (itemTypeAttribute is ItemTypeAttribute)
                {
                    itemType = new ItemTypeGenerator { Attribute = itemTypeAttribute };
                    itemType.Initialize(type);
                }
                cacheItemTypeAttributes[type] = itemType;
            }
            return itemType;
        }

        public static DBTable<T> GetTable<T>(DBSchema schema = null, bool generate = false) where T : DBItem, new()
        {
            return (DBTable<T>)GetTable(typeof(T), schema, generate);
        }

        public static DBTable GetTable(Type type, DBSchema schema = null, bool generate = false)
        {
            if (!cacheTables.TryGetValue(type, out var table))
            {
                var tableAttribute = GetTableAttribute(type);
                if (tableAttribute != null)
                {
                    if (tableAttribute.Table == null && generate)
                        tableAttribute.Generate(schema);
                    var itemAttribute = GetItemTypeAttribute(type);
                    if (itemAttribute != null)
                    {
                        if (itemAttribute.Table == null && generate)
                            itemAttribute.Generate(schema);
                        return cacheTables[type] = itemAttribute.Table;
                    }
                    return cacheTables[type] = tableAttribute.Table;
                }
                else
                {
                    cacheTables[type] = null;
                }
            }
            return table;
        }

        protected DBCommand dmlInsert;
        protected DBCommand dmlInsertSequence;
        protected DBCommand dmlDelete;
        protected IDBLogTable logTable;
        protected DBTableGroup tableGroup;
        protected DBColumn nameKey = DBColumn.EmptyKey;
        protected DBColumn accessKey = DBColumn.EmptyKey;
        protected DBColumn primaryKey = DBColumn.EmptyKey;
        protected DBColumn fileKey = DBColumn.EmptyKey;
        protected DBColumn fileNameKey = DBColumn.EmptyKey;
        protected DBColumn fileLOBKey = DBColumn.EmptyKey;
        protected DBColumn fileLastWriteKey = DBColumn.EmptyKey;
        protected DBColumn dateKey = DBColumn.EmptyKey;
        protected DBColumn stampKey = DBColumn.EmptyKey;
        protected DBColumn codeKey = DBColumn.EmptyKey;
        protected DBColumn typeKey = DBColumn.EmptyKey;
        protected DBColumn groupKey = DBColumn.EmptyKey;
        protected DBColumn stateKey = DBColumn.EmptyKey;
        protected DBColumn imageKey = DBColumn.EmptyKey;
        protected DBColumn itemTypeKey = DBColumn.EmptyKey;

        private DBSequence cacheSequence;
        public IComparer DefaultComparer;
        public int Hash = -1;
        protected internal ConcurrentQueue<int> FreeHandlers = new ConcurrentQueue<int>();

        protected string query;
        protected string comInsert;
        protected string comUpdate;
        protected string comDelete;
        protected string groupName;
        protected string sequenceName;
        protected bool caching = false;
        protected DBTableType type = DBTableType.Table;
        protected int blockSize = 256;
        internal object locker = new object();
        protected List<IDBVirtualTable> virtualTables = new List<IDBVirtualTable>(0);
        private DBItemType itemType;
        private int itemTypeIndex = 0;


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
        public TableGenerator Generator { get; internal set; }

        [XmlIgnore, JsonIgnore]
        public virtual IDBLogTable LogTable
        {
            get => logTable
                    ?? (logTable = (IDBLogTable)Schema?.LogSchema?.Tables[LogTableName]
                    ?? (IDBLogTable)Schema?.Tables[LogTableName]);
            set
            {
                logTable = value;
                LogTableName = value?.Name;
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public object Lock => locker;

        //[Browsable(false), XmlIgnore, JsonIgnore]
        //public TableAttribute Info { get; protected set; }
        [Browsable(false)]
        public Dictionary<int, DBItemType> ItemTypes { get; set; } = new Dictionary<int, DBItemType>();

        [Browsable(false), XmlIgnore, JsonIgnore]
        public DBItemType ItemType => itemType;

        [Browsable(false), XmlIgnore, JsonIgnore]
        public string ItemTypeName => itemType?.Type.Name;

        [Browsable(false), XmlIgnore, JsonIgnore]
        public int ItemTypeIndex
        {
            get => itemTypeIndex;
            set => itemTypeIndex = value;
        }

        public override string FullName => string.Format("{0}.{1}", Schema != null ? Schema.Name : string.Empty, name);

        [Category("Database")]
        public string Query
        {
            get => query;
            set
            {
                if (query != value)
                {
                    query = value;
                    OnPropertyChanged(nameof(Query), DDLType.Alter);
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBSystem System => Schema?.System ?? DBSystem.Default;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBConnection Connection => Schema?.Connection;

        [Browsable(false)]
        public virtual int BlockSize
        {
            get => blockSize;
            set
            {
                var i = 1;
                var temp = value;
                do
                {
                    i++;
                    temp = temp / 2;
                }
                while (temp > 1);
                temp = (int)Math.Pow(2, i);
                if (temp != blockSize)
                {
                    blockSize = temp;
                    foreach (var column in Columns)
                    {
                        column.CheckPull();
                    }
                }
            }
        }

        public virtual string SqlName => name;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public abstract bool IsEdited { get; }

        [Browsable(false), Category("Database")]
        public string ComInsert
        {
            get => comInsert;
            set => comInsert = value;
        }

        [Browsable(false), Category("Database")]
        public string ComUpdate
        {
            get => comUpdate;
            set => comUpdate = value;
        }

        [Browsable(false), Category("Database")]
        public string ComDelete
        {
            get => comDelete;
            set => comDelete = value;
        }

        [Browsable(false), Category("Group")]
        public virtual string GroupName
        {
            get => groupName;
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
            get => sequenceName;
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
            get => cacheSequence ?? (cacheSequence = Schema?.Sequences[SequenceName]);
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

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn AccessKey => accessKey == DBColumn.EmptyKey ? (accessKey = Columns.GetByKey(DBColumnKeys.Access)) : accessKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn PrimaryKey => primaryKey == DBColumn.EmptyKey ? (primaryKey = Columns.GetByKey(DBColumnKeys.Primary)) : primaryKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn FileLOBKey => fileLOBKey == DBColumn.EmptyKey ? (fileLOBKey = Columns.GetByKey(DBColumnKeys.FileLOB)) : fileLOBKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn FileKey => fileKey == DBColumn.EmptyKey ? (fileKey = Columns.GetByKey(DBColumnKeys.File)) : fileKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn FileNameKey => fileNameKey == DBColumn.EmptyKey ? (fileNameKey = Columns.GetByKey(DBColumnKeys.FileName)) : fileNameKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn FileLastWriteKey => fileLastWriteKey == DBColumn.EmptyKey ? (fileLastWriteKey = Columns.GetByKey(DBColumnKeys.FileLastWrite)) : fileLastWriteKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn StampKey => stampKey == DBColumn.EmptyKey ? (stampKey = Columns.GetByKey(DBColumnKeys.Stamp)) : stampKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn DateKey => dateKey == DBColumn.EmptyKey ? (dateKey = Columns.GetByKey(DBColumnKeys.Date)) : dateKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn GroupKey => groupKey == DBColumn.EmptyKey ? (groupKey = Columns.GetByKey(DBColumnKeys.Group)) : groupKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn ElementTypeKey => typeKey == DBColumn.EmptyKey ? (typeKey = Columns.GetByKey(DBColumnKeys.ElementType)) : typeKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn StatusKey => stateKey == DBColumn.EmptyKey ? (stateKey = Columns.GetByKey(DBColumnKeys.State)) : stateKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn ItemTypeKey => itemTypeKey == DBColumn.EmptyKey ? (itemTypeKey = Columns.GetByKey(DBColumnKeys.ItemType)) : itemTypeKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn CodeKey => codeKey == DBColumn.EmptyKey ? (codeKey = Columns.GetByKey(DBColumnKeys.Code)) : codeKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn ImageKey => imageKey == DBColumn.EmptyKey ? (imageKey = Columns.GetByKey(DBColumnKeys.Image)) : imageKey;

        public DBTableType Type
        {
            get => type;
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
            get => !string.IsNullOrEmpty(LogTableName);
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
            get => caching;
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
        public bool IsReadOnly => false;

        public abstract bool Contains(DBItem item);

        public abstract bool Remove(DBItem item);

        public abstract IEnumerator<DBItem> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public abstract void CopyTo(DBItem[] array, int arrayIndex);

        public abstract void OnItemChanging<V>(DBItem item, string property, DBColumn column, V value);
        public abstract void OnItemChanging(DBItem item, string proeprty, DBColumn column, object value);
        public abstract void OnItemChanged<V>(DBItem item, string proeprty, DBColumn column, V value);
        public abstract void OnItemChanged(DBItem item, string proeprty, DBColumn column, object value);


        public abstract void Trunc();

        public bool IsSerializeableColumn(DBColumn column, Type type)
        {
            return column.Property != null
                && column.PropertyInvoker != null && column.PropertyInvoker != column
                && column.PropertyInvoker.TargetType.IsAssignableFrom(type)
                //&& (column.Attribute.Keys & DBColumnKeys.Access) != DBColumnKeys.Access
                && (column.Keys & DBColumnKeys.Stamp) != DBColumnKeys.Stamp
                && (column.Keys & DBColumnKeys.Password) != DBColumnKeys.Password
                && (column.Keys & DBColumnKeys.File) != DBColumnKeys.File;
        }

        public DBColumn ParseColumnProperty(string property)
        {
            return Columns[property]
                ?? ParseProperty(property);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DBColumn ParseProperty(string property, ref DBColumn cache)
        {
            return cache == DBColumn.EmptyKey ? (cache = ParseProperty(property)) : cache;
        }

        public DBColumn ParseProperty(string property)
        {
            return Columns.GetByProperty(property)
                ?? Columns.GetByReferenceProperty(property);
        }

        protected internal void SetItemType(Type type)
        {
            itemType = ItemTypes[0] = new DBItemType { Type = type };
            // Info = DBService.GetTableAttribute(type);
        }

        public IEnumerable<DBTable> GetChilds()
        {
            foreach (var item in virtualTables)
            {
                yield return (DBTable)item;
            }
        }

        public void RefreshSequence()
        {
            using (var transaction = new DBTransaction(Connection))
            {
                RefreshSequence(transaction);
                transaction.Commit();
            }
        }

        public void RefreshSequence(DBTransaction transaction)
        {
            var current = transaction.ExecuteQuery($"select max({PrimaryKey.SqlName}) from {SqlName}");
            Sequence.SetCurrent(current);
            Sequence.NextInternal();
            Sequence.Save(transaction);
        }

        public void LoadReferenceBlock(IDbCommand command, DBTransaction transaction)
        {
            var references = Columns.GetIsReference();
            if (!references.Any())
                return;
            var oldAlias = Helper.IntToChar(transaction.ReferencingRecursion).ToLowerInvariant();
            transaction.ReferencingRecursion++;
            var newAlias = Helper.IntToChar(transaction.ReferencingRecursion).ToLowerInvariant();
            string where = GetWhere(command.CommandText);
            foreach (var column in references)
            {
                if (column.ReferenceTable != null
                    && !transaction.ReferencingStack.Contains(column)
                    && column.ColumnType == DBColumnTypes.Default
                    && (column.Keys & DBColumnKeys.Group) != DBColumnKeys.Group
                    && column.ReferenceTable != this
                    && !column.ReferenceTable.IsSynchronized)
                {
                    transaction.ReferencingStack.Add(column);
                    var subCommand = DBCommand.CloneCommand(command, column.ReferenceTable.BuildQuery($@"
    left join {SqlName} {oldAlias} on {oldAlias}.{column.SqlName} = {newAlias}.{column.ReferenceTable.PrimaryKey.SqlName} 
    {where}", newAlias, null));
                    //Debug.WriteLine($"Load Reference: {subCommand.CommandText}");
                    column.ReferenceTable.LoadItems(subCommand, DBLoadParam.Referencing, transaction);
                    transaction.ReferencingStack.Remove(column);
                }
            }
            transaction.ReferencingRecursion--;
        }

        public void LoadReferencingBlock(IDbCommand command, DBTransaction transaction)
        {
            if (!Generator.Referencings.Any())
                return;
            var oldAlias = Helper.IntToChar(transaction.ReferencingRecursion).ToLowerInvariant();
            transaction.ReferencingRecursion++;
            var newAlias = Helper.IntToChar(transaction.ReferencingRecursion).ToLowerInvariant();
            string where = GetWhere(command.CommandText);
            foreach (var reference in Generator.Referencings)
            {
                var referenceColumn = reference.ReferenceColumn;
                if (!transaction.ReferencingStack.Contains(referenceColumn.Column)
                    && (referenceColumn.Attribute.Keys & DBColumnKeys.Group) != DBColumnKeys.Group
                    && reference.ReferenceTable.Table != this
                    && !reference.ReferenceTable.Table.IsSynchronized
                    && !(reference.ReferenceTable.Table is IDBVirtualTable))
                {
                    transaction.ReferencingStack.Add(referenceColumn.Column);
                    var subCommand = DBCommand.CloneCommand(command, reference.ReferenceTable.Table.BuildQuery($@"
    left join {SqlName} {oldAlias} on {oldAlias}.{PrimaryKey.SqlName} = {newAlias}.{referenceColumn.Column.SqlName} 
    {where}", newAlias, null));
                    //Debug.WriteLine($"Load Referencing: {subCommand.CommandText}");
                    var loadParam = reference.ReferenceTable.Attribute.ForceLoadReference ? DBLoadParam.Reference | DBLoadParam.Referencing : DBLoadParam.Referencing;
                    reference.ReferenceTable.Table.LoadItems(subCommand, loadParam, transaction);
                    transaction.ReferencingStack.Remove(referenceColumn.Column);
                }
            }
            transaction.ReferencingRecursion--;
        }

        private static string GetWhere(string commandText)
        {
            var whereIndex = commandText.IndexOf(" left join ", StringComparison.OrdinalIgnoreCase);
            if (whereIndex < 0)
            {
                whereIndex = commandText.IndexOf(" where ", StringComparison.OrdinalIgnoreCase);
            }
            var where = whereIndex < 0 ? string.Empty : commandText.Substring(whereIndex);
            return where;
        }

        public DBColumn GetNameKey(string group)
        {
            if (group.Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                if (nameKey == DBColumn.EmptyKey)
                {
                    return nameKey = GetCultureColumn(group, Locale.Instance.Culture);
                }
                return nameKey;
            }
            return GetCultureColumn(group, Locale.Instance.Culture);
        }

        public DBColumn GetCultureColumn(string group, CultureInfo culture)
        {
            return Columns.GetByGroup(@group)
                     .FirstOrDefault(column => column.Culture != null
                     && column.Culture.ThreeLetterISOLanguageName == culture.ThreeLetterISOLanguageName);
        }

        public event EventHandler<DBLoadProgressEventArgs> LoadProgress;

        protected void RaiseLoadProgress(DBLoadProgressEventArgs arg)
        {
            LoadProgress?.Invoke(this, arg);
        }

        public event EventHandler<DBLoadCompleteEventArgs> LoadComplete;

        protected void RaiseLoadCompleate(DBTransaction transaction)
        {
            LoadComplete?.Invoke(this, new DBLoadCompleteEventArgs(transaction.View, null));
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
                column = new DBColumn(name) { DataType = type };
                Columns.Add(column);
                newCol = true;
            }
            column.ReaderDataType = type;
            return column;
        }

        public abstract DBItem this[int index] { get; }

        public abstract void Add(DBItem item);

        public abstract DBItem LoadItemFromReader(DBTransaction transaction);

        public abstract IEnumerable<DBItem> LoadItems(QQuery query, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null);

        public abstract IEnumerable<DBItem> LoadItems(string whereText = null, DBLoadParam param = DBLoadParam.None, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null);

        public abstract IEnumerable<DBItem> LoadItems(IDbCommand command, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null);

        public abstract Task<IEnumerable<DBItem>> LoadItemsAsync(IDbCommand command, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null);

        public abstract DBItem LoadItemByCode(string code, DBColumn column, DBLoadParam param, DBTransaction transaction = null);

        public abstract DBItem LoadItemById(object id, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null);

        public abstract void ReloadItem(object id, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null);

        public abstract void AddView(IDBTableView view);

        public abstract void RemoveView(IDBTableView view);

        public List<DBItem> LoadItemsById(List<string> ids, DBTransaction transaction)
        {
            var items = new List<DBItem>();
            foreach (var id in ids)
            {
                var item = LoadItemById(id, DBLoadParam.Referencing, null, transaction);
                if (item != null)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        public event EventHandler<DBItemEventArgs> RowUpdating;

        public bool OnUpdating(DBItemEventArgs e)
        {
            DBService.OnUpdating(e);
            RowUpdating?.Invoke(this, e);
            return !e.Cancel;
        }

        protected internal int GetNextHandler(out short block, out short blockIndex)
        {
            if (FreeHandlers.Count > 0 && FreeHandlers.TryDequeue(out var handler))
            {
                Helper.OneToTwoPointer(handler, out block, out blockIndex);
                return handler;
            }
            return Pull.GetHIndex(NextHash(), BlockSize, out block, out blockIndex);
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

        public virtual async Task<bool> SaveItem(DBItem item, DBTransaction transaction)
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
                    item.Stamp = DateTime.UtcNow;
                if (DateKey != null)
                    item.DateCreate = DateTime.UtcNow;
                if (IsLoging && StatusKey != null && !item.Changed(StatusKey))
                    item.Status = DBStatus.New;
            }
            else if ((item.UpdateState & DBUpdateState.Update) == DBUpdateState.Update)
            {
                if (StampKey != null)
                    item.Stamp = DateTime.UtcNow;
                if (IsLoging && StatusKey != null && item.Status == DBStatus.Actual && !item.Changed(StatusKey) && !item.Changed(AccessKey))
                    item.Status = DBStatus.Edit;
            }

            //if (!item.Attached)
            //    Add(item);
            transaction.AddItem(item);
            var args = new DBItemEventArgs(item, transaction);

            //CheckRerencing();

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
                    item.Accept(transaction.Caller);
                    return true;
                }
            }
            var command = transaction.AddCommand(dmlCommand.Text, dmlCommand.Type);
            dmlCommand.FillCommand(command, item);

            var result = await transaction.ExecuteQueryAsync(command, dmlCommand == dmlInsertSequence ? DBExecuteType.Scalar : DBExecuteType.NoReader);
            transaction.DbConnection.System.UploadCommand(item, command);
            if (PrimaryKey != null && item.PrimaryId == null)
            {
                item[PrimaryKey] = result;
                Sequence.SetCurrent(result);
            }

            if (!transaction.NoLogs && LogTable != null)
            {
                args.LogItem = (DBLogItem)LogTable.NewItem(DBUpdateState.Insert, false, item.ItemType ?? 0);
                args.LogItem.BaseItem = item;
                DBService.OnLogItem(args);
                await args.LogItem.Save(transaction.GetSubTransaction(LogTable.Connection));
            }

            item.OnUpdated(args);
            item.UpdateState |= DBUpdateState.Commit;

            return true;
        }

        public abstract void Accept(DBItem item);

        public virtual int NextHash()
        {
            return Interlocked.Increment(ref Hash);
        }

        public async Task Save(IList rows = null)
        {
            using (var transaction = new DBTransaction(Connection))
            {
                try
                {
                    await Save(transaction, rows);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                    transaction.Rollback();
                }
            }
        }

        public async Task Save(DBTransaction transaction, IList rows = null)
        {
            if (rows == null)
                rows = GetChangedItems().ToList();

            if (rows.Count > 0)
            {
                ListHelper.QuickSort(rows, new InvokerComparer(typeof(DBItem), nameof(DBItem.UpdateState)));
                foreach (DBItem row in rows)
                {
                    row.Attach();
                    transaction.AddItem(row);
                }
                foreach (DBItem row in rows)
                {
                    await row.Save(transaction);
                }
            }

            Sequence?.Save(transaction);
        }

        public int GetRowCount(DBTransaction transaction, string @where)
        {
            object val = transaction.ExecuteQuery(transaction.AddCommand(BuildQuery(@where, "a", null, "count(*)")), DBExecuteType.Scalar);
            return val is Exception ? -1 : int.Parse(val.ToString());
        }

        #region IComparable Members

        int IComparable.CompareTo(object obj)
        {
            if (obj is DBTable table)
            {
                return DBTableComparer.Instance.Compare(this, table);
            }
            return -1;
        }

        #endregion

        public virtual DBColumn ParseColumn(string name)
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

        public void RejectChanges(IUserIdentity user)
        {
            RejectChanges(GetChangedItems().ToList(), user);
        }

        public void RejectChanges(IEnumerable<DBItem> items, IUserIdentity user)
        {
            foreach (var item in items)
            {
                item.Reject(user);
            }
        }

        public void AcceptChanges(IUserIdentity user)
        {
            foreach (var row in GetChangedItems().ToList())
            {
                row.Accept(user);
            }
        }

        public static IDBTableView CreateView(Type type)
        {
            return (IDBTableView)EmitInvoker.Initialize(typeof(DBTableView<>).MakeGenericType(type), new Type[] { }, true).Create();
        }

        [Browsable(false)]
        public abstract IDBTableView DefaultItemsView { get; }

        public List<DBForeignKey> ChildRelations { get; } = new List<DBForeignKey>();

        public abstract IDBTableView CreateItemsView(string query = "", DBViewKeys mode = DBViewKeys.None, DBStatus filter = DBStatus.Empty);

        public abstract DBItem NewItem(DBUpdateState state = DBUpdateState.Insert, bool def = true);

        public virtual DBItem NewItem(DBUpdateState state, bool def, int typeIndex)
        {
            var type = GetItemType(typeIndex);
            return type != null
                ? NewItem(state, def, typeIndex, type.Type)
                : NewItem(state, def);
        }

        public DBItem NewItem(DBUpdateState state, bool def, Type type)
        {
            var typeIndex = ItemTypes.First(p => p.Value.Type == type).Key;
            return NewItem(state, def, typeIndex, type);
        }

        public DBItem NewItem(DBUpdateState state, bool def, int typeIndex, Type type)
        {
            var item = (DBItem)FormatterServices.GetUninitializedObject(type);
            item.Build(this, def, typeIndex);
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
        public abstract IEnumerable<DBItem> SelectItems(DBColumn column, CompareType comparer, object val);

        public abstract IEnumerable<DBItem> SelectItems(string qQuery);

        public abstract IEnumerable<DBItem> SelectItems(QQuery qQuery);

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
                    return ListHelper.Equal(val1, val2) ? !comparer.Not : comparer.Not;
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
                    return ListHelper.Compare(val1, between.Min.GetValue(item), null) >= 0
                                     && ListHelper.Compare(val1, between.Max.GetValue(item), null) <= 0;
                default:
                    bool f = false;
                    int rez = ListHelper.Compare(val1, val2, null);
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
            return Schema?.GetChildRelations(this) ?? Enumerable.Empty<DBForeignKey>();
        }

        public IEnumerable<ReferencingGenerator> GetPropertyReferencing(Type type)
        {
            if (Generator == null)
            {
                yield break;
            }

            foreach (var referencing in Generator.Referencings)
            {
                if (referencing.PropertyInvoker.TargetType.IsAssignableFrom(type))
                {
                    yield return referencing;
                }
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

        public string BuildQuery(string whereFilter, string alias, IEnumerable<DBColumn> cols, string function = null)
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
                {
                    cols = Columns.Where(p => (p.Keys & DBColumnKeys.File) != DBColumnKeys.File);// query += "*";// cols = this.columns as IEnumerable;
                }

                bool f = false;
                foreach (DBColumn column in cols)
                {
                    string temp = FormatQColumn(column, alias);
                    if (!string.IsNullOrEmpty(temp))
                    {
                        if (f)
                            select.Append(", ");
                        else
                            f = true;
                        select.Append(temp);
                    }
                }

                if (select.ToString()?.Equals("select ", StringComparison.Ordinal) ?? false)
                    select.Append(" * ");
            }
            string vquery = Query;
            if (!string.IsNullOrEmpty(vquery))
            {
                var isWhere = whereFilter.IndexOf("where", StringComparison.OrdinalIgnoreCase) > -1;
                vquery = vquery.Replace("a.", $"{alias}.");
                if (whereFilter.IndexOf(vquery, StringComparison.OrdinalIgnoreCase) >= 0)
                    vquery = string.Empty;
                else
                    vquery = $"{(isWhere ? " and (" : " where ")}{vquery}{(isWhere ? ")" : string.Empty)}";
            }
            select.Append("\nfrom ");
            select.Append(FormatQTable(alias));
            select.Append(" ");
            select.Append(whereFilter);
            select.Append(vquery);
            return select.ToString();
        }

        public string FormatQColumn(DBColumn column, string tableAlias)
        {
            return System?.FormatQColumn(column, tableAlias);
        }

        public string FormatQTable(string alias)
        {
            return System?.FormatQTable(this, alias);
        }

        protected void ClearColumnsData(bool pool)
        {
            foreach (var column in Columns)
            {
                if (pool)
                {
                    column.Clear();
                }
                column.Index?.Clear();
            }
        }

        public IDbCommand CreateKeyCommmand(object key, DBColumn column, IEnumerable<DBColumn> cols = null)
        {
            string idName = System.ParameterPrefix + column.Name;
            var command = System.CreateCommand(Schema.Connection, BuildQuery($"where a.{column.SqlName}={idName}", "a", cols));
            System.CreateParameter(command, idName, key, column);
            return command;
        }

        public IDbCommand CreatePrimaryKeyCommmand(object id, IEnumerable<DBColumn> cols = null)
        {
            return CreateKeyCommmand(id, PrimaryKey, cols);
        }

        public string CreateQuery(string whereText, string alias, IEnumerable<DBColumn> cols = null)
        {
            string rez;
            if (string.IsNullOrEmpty(whereText) || whereText.Trim().StartsWith("where ", StringComparison.OrdinalIgnoreCase))
                rez = BuildQuery(whereText, alias, cols);
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
                        row.Accept((IUserIdentity)null);
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

        public IEnumerable<IInvoker> GetInvokers<T>()
        {
            return GetInvokers(typeof(T));
        }

        public IEnumerable<IInvoker> GetRefInvokers()
        {
            yield return ItemTypeKey.PropertyInvoker;
            yield return PrimaryKey.PropertyInvoker;
        }

        public IEnumerable<IInvoker> GetInvokers(Type type)
        {
            foreach (var itemType in ItemTypes.Values)
            {
                if (itemType.Type == type)
                    return itemType.Invokers;
            }
            return itemType.Invokers;
        }

        public IInvoker GetInvoker(string property)
        {
            var column = Columns.GetByProperty(property);
            if (column != null)
            {
                return column.PropertyInvoker;
            }

            var reference = Foreigns.GetByProperty(property);
            if (reference != null)
            {
                return reference.Invoker;
            }

            var refing = Generator?.GetReferencingByProperty(property);
            if (refing != null)
            {
                return refing.PropertyInvoker;
            }

            return null;
        }


        public IDBLogTable GenerateLogTable()
        {
            if (LogTable == null)
            {
                var genericType = TypeHelper.ParseType(ItemType.Type.Name + "Log");
                var itemType = genericType ?? typeof(DBLogItem);
                LogTable = (IDBLogTable)GetTable(itemType) ?? (IDBLogTable)EmitInvoker.CreateObject(typeof(DBLogTable<>).MakeGenericType(itemType));
                LogTable.BaseTable = this;
                LogTable.Schema.Tables.Add((DBTable)LogTable);
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
                var sizeIndex = data.IndexOf('(');
                if (data.IndexOf('(') > 0)
                {
                    if (columnInfo.Length == null && columnInfo.Precision == null)
                    {
                        var sizeData = data.Substring(sizeIndex).Trim('(', ')')
                            .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        columnInfo.Length =
                            columnInfo.Precision = sizeData[0];
                        if (sizeData.Length > 1)
                            columnInfo.Scale = sizeData[1];
                    }
                    data = data.Substring(0, sizeIndex);

                }
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
                if (!Columns.Contains(column))
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

                if (!Constraints.Contains(constraint))
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
            Columns.AddRange(new[]
            {
                new DBColumn { Name = "type_id", Keys = DBColumnKeys.ItemType, DBDataType = DBDataType.Int },
                new DBColumn { Name = "unid", Keys = DBColumnKeys.Primary, DBDataType = DBDataType.Int },
                new DBColumn { Name = "datec", Keys = DBColumnKeys.Date, DBDataType = DBDataType.DateTime },
                new DBColumn { Name = "dateu", Keys = DBColumnKeys.Stamp, DBDataType = DBDataType.DateTime },
                new DBColumn { Name = "stateid", Keys = DBColumnKeys.State, DBDataType = DBDataType.Decimal, Size = 28 },
                new DBColumn { Name = "access", Keys = DBColumnKeys.Access, DBDataType = DBDataType.Blob, Size = 2000 }
            });
        }

        public virtual void RemoveDeletedColumns()
        {
            for (int i = 0; i < Columns.Count;)
            {
                var column = Columns[i];
                if (column.Property != null && column.PropertyInfo == null)
                {
                    column.RemoveConstraints();
                    column.RemoveForeignKeys();
                    column.RemoveIndexes();

                    Columns.RemoveInternal(column, i);
                }
                else
                {
                    i++;
                }
            }
        }

        [Invoker(typeof(DBTable), nameof(DBTable.GroupName))]
        public class GroupNameInvoker : Invoker<DBTable, string>
        {
            public static readonly GroupNameInvoker Instance = new GroupNameInvoker();
            public override string Name => nameof(DBTable.GroupName);

            public override bool CanWrite => true;

            public override string GetValue(DBTable target) => target.GroupName;

            public override void SetValue(DBTable target, string value) => target.GroupName = value;
        }

        [Invoker(typeof(DBTable), nameof(DBTable.LogTableName))]
        public class LogTableNameInvoker : Invoker<DBTable, string>
        {
            public static readonly LogTableNameInvoker Instance = new LogTableNameInvoker();
            public override string Name => nameof(DBTable.LogTableName);

            public override bool CanWrite => true;

            public override string GetValue(DBTable target) => target.LogTableName;

            public override void SetValue(DBTable target, string value) => target.LogTableName = value;
        }

        [Invoker(typeof(DBTable), nameof(DBTable.LogTable))]
        public class LogTableInvoker : Invoker<DBTable, IDBLogTable>
        {
            public static readonly LogTableInvoker Instance = new LogTableInvoker();
            public override string Name => nameof(DBTable.LogTable);

            public override bool CanWrite => true;

            public override IDBLogTable GetValue(DBTable target) => target.LogTable;

            public override void SetValue(DBTable target, IDBLogTable value) => target.LogTable = value;
        }

        [Invoker(typeof(DBTable), nameof(DBTable.SequenceName))]
        public class SequenceNameInvoker : Invoker<DBTable, string>
        {
            public static readonly SequenceNameInvoker Instance = new SequenceNameInvoker();
            public override string Name => nameof(DBTable.SequenceName);

            public override bool CanWrite => true;

            public override string GetValue(DBTable target) => target.SequenceName;

            public override void SetValue(DBTable target, string value) => target.SequenceName = value;
        }

        [Invoker(typeof(DBTable), nameof(DBTable.Sequence))]
        public class SequenceInvoker : Invoker<DBTable, DBSequence>
        {
            public static readonly SequenceInvoker Instance = new SequenceInvoker();
            public override string Name => nameof(DBTable.Sequence);

            public override bool CanWrite => true;

            public override DBSequence GetValue(DBTable target) => target.Sequence;

            public override void SetValue(DBTable target, DBSequence value) => target.Sequence = value;
        }

        [Invoker(typeof(DBTable), nameof(DBTable.BlockSize))]
        public class BlockSizeInvoker : Invoker<DBTable, int>
        {
            public static readonly BlockSizeInvoker Instance = new BlockSizeInvoker();
            public override string Name => nameof(DBTable.BlockSize);

            public override bool CanWrite => true;

            public override int GetValue(DBTable target) => target.BlockSize;

            public override void SetValue(DBTable target, int value) => target.BlockSize = value;
        }

        [Invoker(typeof(DBTable), nameof(DBTable.Columns))]
        public class ColumnsInvoker : Invoker<DBTable, DBColumnList<DBColumn>>
        {
            public static readonly ColumnsInvoker Instance = new ColumnsInvoker();
            public override string Name => nameof(DBTable.Columns);

            public override bool CanWrite => true;

            public override DBColumnList<DBColumn> GetValue(DBTable target) => target.Columns;

            public override void SetValue(DBTable target, DBColumnList<DBColumn> value) => target.Columns = value;
        }

        [Invoker(typeof(DBTable), nameof(DBTable.ItemType))]
        public class ItemTypeInvoker : Invoker<DBTable, DBItemType>
        {
            public static readonly ItemTypeInvoker Instance = new ItemTypeInvoker();
            public override string Name => nameof(DBTable.ItemType);

            public override bool CanWrite => false;

            public override DBItemType GetValue(DBTable target) => target.ItemType;

            public override void SetValue(DBTable target, DBItemType value) { }
        }

        [Invoker(typeof(DBTable), nameof(DBTable.ItemTypeName))]
        public class ItemTypeNameInvoker : Invoker<DBTable, string>
        {
            public static readonly ItemTypeNameInvoker Instance = new ItemTypeNameInvoker();
            public override string Name => nameof(DBTable.ItemTypeName);

            public override bool CanWrite => false;

            public override string GetValue(DBTable target) => target.ItemTypeName;

            public override void SetValue(DBTable target, string value) { }
        }

        [Invoker(typeof(DBTable), nameof(DBTable.ColumnGroups))]
        public class ColumnGroupsInvoker : Invoker<DBTable, DBColumnGroupList>
        {
            public override string Name => nameof(DBTable.ColumnGroups);

            public override bool CanWrite => true;

            public override DBColumnGroupList GetValue(DBTable target) => target.ColumnGroups;

            public override void SetValue(DBTable target, DBColumnGroupList value) => target.ColumnGroups = value;
        }

        [Invoker(typeof(DBTable), nameof(DBTable.Indexes))]
        public class IndexesInvoker : Invoker<DBTable, DBIndexList>
        {
            public override string Name => nameof(DBTable.Indexes);

            public override bool CanWrite => true;

            public override DBIndexList GetValue(DBTable target) => target.Indexes;

            public override void SetValue(DBTable target, DBIndexList value) => target.Indexes = value;
        }

        [Invoker(typeof(DBTable), nameof(DBTable.Foreigns))]
        public class ForeignsInvoker : Invoker<DBTable, DBForeignList>
        {
            public override string Name => nameof(DBTable.Foreigns);

            public override bool CanWrite => true;

            public override DBForeignList GetValue(DBTable target) => target.Foreigns;

            public override void SetValue(DBTable target, DBForeignList value) => target.Foreigns = value;
        }

        [Invoker(typeof(DBTable), nameof(DBTable.Constraints))]
        public class ConstraintsInvoker : Invoker<DBTable, DBConstraintList<DBConstraint>>
        {
            public override string Name => nameof(DBTable.Constraints);

            public override bool CanWrite => true;

            public override DBConstraintList<DBConstraint> GetValue(DBTable target) => target.Constraints;

            public override void SetValue(DBTable target, DBConstraintList<DBConstraint> value) => target.Constraints = value;
        }

        [Invoker(typeof(DBTable), nameof(DBTable.ItemTypes))]
        public class ItemTypesInvoker : Invoker<DBTable, Dictionary<int, DBItemType>>
        {
            public override string Name => nameof(DBTable.ItemTypes);

            public override bool CanWrite => true;

            public override Dictionary<int, DBItemType> GetValue(DBTable target) => target.ItemTypes;

            public override void SetValue(DBTable target, Dictionary<int, DBItemType> value) => target.ItemTypes = value;
        }

        [Invoker(typeof(DBTable), nameof(DBTable.Query))]
        public class QueryInvoker : Invoker<DBTable, string>
        {
            public override string Name => nameof(DBTable.Query);

            public override bool CanWrite => true;

            public override string GetValue(DBTable target) => target.Query;

            public override void SetValue(DBTable target, string value) => target.Query = value;
        }

        [Invoker(typeof(DBTable), nameof(DBTable.Type))]
        public class TypeInvoker : Invoker<DBTable, DBTableType>
        {
            public override string Name => nameof(DBTable.Type);

            public override bool CanWrite => true;

            public override DBTableType GetValue(DBTable target) => target.Type;

            public override void SetValue(DBTable target, DBTableType value) => target.Type = value;
        }

        [Invoker(typeof(DBTable), nameof(DBTable.IsCaching))]
        public class IsCachingInvoker : Invoker<DBTable, bool>
        {
            public override string Name => nameof(DBTable.IsCaching);

            public override bool CanWrite => true;

            public override bool GetValue(DBTable target) => target.IsCaching;

            public override void SetValue(DBTable target, bool value) => target.IsCaching = value;
        }

        [Invoker(typeof(DBTable), nameof(DBTable.ComDelete))]
        public class ComDeleteInvoker : Invoker<DBTable, string>
        {
            public override string Name => nameof(DBTable.ComDelete);

            public override bool CanWrite => true;

            public override string GetValue(DBTable target) => target.ComDelete;

            public override void SetValue(DBTable target, string value) => target.ComDelete = value;
        }

        [Invoker(typeof(DBTable), nameof(DBTable.ComInsert))]
        public class ComInsertInvoker : Invoker<DBTable, string>
        {
            public override string Name => nameof(DBTable.ComInsert);

            public override bool CanWrite => true;

            public override string GetValue(DBTable target) => target.ComInsert;

            public override void SetValue(DBTable target, string value) => target.ComInsert = value;
        }

        [Invoker(typeof(DBTable), nameof(DBTable.ComUpdate))]
        public class ComUpdateInvoker : Invoker<DBTable, string>
        {
            public override string Name => nameof(DBTable.ComUpdate);

            public override bool CanWrite => true;

            public override string GetValue(DBTable target) => target.ComUpdate;

            public override void SetValue(DBTable target, string value) => target.ComUpdate = value;
        }

        [Invoker(typeof(DBTable), nameof(DBTable.SqlName))]
        public class SqlNameInvoker : Invoker<DBTable, string>
        {
            public override string Name => nameof(DBTable.SqlName);

            public override bool CanWrite => false;

            public override string GetValue(DBTable target) => target.SqlName;

            public override void SetValue(DBTable target, string value) { }
        }

        [Invoker(typeof(DBTable), nameof(DBTable.Count))]
        public class CountInvoker : Invoker<DBTable, int>
        {
            public override string Name => nameof(DBTable.Count);

            public override bool CanWrite => false;

            public override int GetValue(DBTable target) => target.Count;

            public override void SetValue(DBTable target, int value) { }
        }

        [Invoker(typeof(DBTable), nameof(DBTable.IsReadOnly))]
        public class IsReadOnlyInvoker : Invoker<DBTable, bool>
        {
            public override string Name => nameof(DBTable.IsReadOnly);

            public override bool CanWrite => false;

            public override bool GetValue(DBTable target) => target.IsReadOnly;

            public override void SetValue(DBTable target, bool value) { }
        }

        [Invoker(typeof(DBTable), nameof(DBTable.DefaultItemsView))]
        public class DefaultItemsViewInvoker : Invoker<DBTable, IDBTableView>
        {
            public override string Name => nameof(DBTable.DefaultItemsView);

            public override bool CanWrite => false;

            public override IDBTableView GetValue(DBTable target) => target.DefaultItemsView;

            public override void SetValue(DBTable target, IDBTableView value) { }
        }

        [Invoker(typeof(DBTable), nameof(DBTable.ChildRelations))]
        public class ChildRelationsInvoker : Invoker<DBTable, List<DBForeignKey>>
        {
            public override string Name => nameof(DBTable.ChildRelations);

            public override bool CanWrite => false;

            public override List<DBForeignKey> GetValue(DBTable target) => target.ChildRelations;

            public override void SetValue(DBTable target, List<DBForeignKey> value) { }
        }
    }
}
