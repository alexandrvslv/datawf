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
using DataWF.Data;
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

[assembly: Invoker(typeof(DBTable), nameof(DBTable.GroupName), typeof(DBTable.GroupNameInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.LogTableName), typeof(DBTable.LogTableNameInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.LogTable), typeof(DBTable.LogTableInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.SequenceName), typeof(DBTable.SequenceNameInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.Sequence), typeof(DBTable.SequenceInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.BlockSize), typeof(DBTable.BlockSizeInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.Columns), typeof(DBTable.ColumnsInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.ItemType), typeof(DBTable.ItemTypeInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.ItemTypeName), typeof(DBTable.ItemTypeNameInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.ColumnGroups), typeof(DBTable.ColumnGroupsInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.Indexes), typeof(DBTable.IndexesInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.Foreigns), typeof(DBTable.ForeignsInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.Constraints), typeof(DBTable.ConstraintsInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.ItemTypes), typeof(DBTable.ItemTypesInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.Query), typeof(DBTable.QueryInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.Type), typeof(DBTable.TypeInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.Keys), typeof(DBTable.KeysInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.IsCaching), typeof(DBTable.IsCachingInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.IsReadOnly), typeof(DBTable.IsReadOnlyInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.IsPrivate), typeof(DBTable.IsPrivateInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.ComDelete), typeof(DBTable.ComDeleteInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.ComInsert), typeof(DBTable.ComInsertInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.ComUpdate), typeof(DBTable.ComUpdateInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.SqlName), typeof(DBTable.SqlNameInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.Count), typeof(DBTable.CountInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.DefaultItemsView), typeof(DBTable.DefaultItemsViewInvoker))]
[assembly: Invoker(typeof(DBTable), nameof(DBTable.ChildRelations), typeof(DBTable.ChildRelationsInvoker))]
namespace DataWF.Data
{
    public abstract class DBTable : DBSchemaItem, IComparable, IDBTable
    {
        private static readonly Dictionary<Type, DBTable> cacheTables = new Dictionary<Type, DBTable>();
        private static readonly Dictionary<Type, TableGenerator> cacheTableGenerators = new Dictionary<Type, TableGenerator>();
        private static readonly Dictionary<Type, ItemTypeGenerator> cacheItemTypeGenerator = new Dictionary<Type, ItemTypeGenerator>();
        private static int tableIndex;

        public static void ClearGeneratorCache()
        {
            cacheTables.Clear();
            cacheTableGenerators.Clear();
            cacheItemTypeGenerator.Clear();
        }

        public static TableGenerator GetTableGeneratorInherit(Type type)
        {
            var tableGenerator = GetTableGenerator(type);
            while (tableGenerator == null && type != null)
            {
                type = type.BaseType;
                tableGenerator = type == null ? null : GetTableGenerator(type);
            }
            return tableGenerator;
        }

        public static TableGenerator GetTableGenerator<T>()
        {
            return GetTableGenerator(typeof(T));
        }

        public static TableGenerator GetTableGenerator(Type type)
        {
            if (!cacheTableGenerators.TryGetValue(type, out var tableGenerator))
            {
                var tableAttribute = type.GetCustomAttribute<TableAttribute>(false);
                if (tableAttribute is LogTableAttribute)
                {
                    tableGenerator = new LogTableGenerator() { Attribute = tableAttribute };
                    tableGenerator.Initialize(type);
                }
                else if (tableAttribute is TableAttribute)
                {
                    tableGenerator = new TableGenerator() { Attribute = tableAttribute };
                    tableGenerator.Initialize(type);
                }
                cacheTableGenerators[type] = tableGenerator;
            }
            if (tableGenerator == null)
            {
                var itemType = GetItemTypeGenerator(type);
                tableGenerator = itemType?.TableGenerator;
            }
            return tableGenerator;
        }

        public static ItemTypeGenerator GetItemTypeGenerator(Type type)
        {
            if (!cacheItemTypeGenerator.TryGetValue(type, out var itemTypeGenerator))
            {
                var itemTypeAttribute = type.GetCustomAttribute<ItemTypeAttribute>(false);
                if (itemTypeAttribute is LogItemTypeAttribute)
                {
                    itemTypeGenerator = new LogItemTypeGenerator { Attribute = itemTypeAttribute };
                    itemTypeGenerator.Initialize(type);
                }
                else if (itemTypeAttribute is ItemTypeAttribute)
                {
                    itemTypeGenerator = new ItemTypeGenerator { Attribute = itemTypeAttribute };
                    itemTypeGenerator.Initialize(type);
                }
                cacheItemTypeGenerator[type] = itemTypeGenerator;
            }
            return itemTypeGenerator;
        }

        public static DBTable<T> GetTable<T>(DBSchema schema = null, bool generate = false) where T : DBItem, new()
        {
            return (DBTable<T>)GetTable(typeof(T), schema, generate);
        }

        public static DBTable GetTable(Type type, DBSchema schema = null, bool generate = false)
        {
            if (type == null)
                return null;
            if (!cacheTables.TryGetValue(type, out var table))
            {
                var itemGenerator = GetItemTypeGenerator(type);
                if (itemGenerator != null)
                {
                    if (!itemGenerator.Generated && generate)
                        itemGenerator.Generate(schema);
                    if (itemGenerator.Table != null)
                        return cacheTables[type] = itemGenerator.Table;
                }
                else
                {
                    var tableGenerator = GetTableGenerator(type);
                    if (tableGenerator != null && tableGenerator.ItemType == type)
                    {
                        if (!tableGenerator.Generated && generate)
                            tableGenerator.Generate(schema);
                        if (tableGenerator.Table != null)
                            return cacheTables[type] = tableGenerator.Table;
                    }
                    else
                    {
                        cacheTables[type] = null;
                    }
                }
            }
            return table;
        }

        protected DBCommand dmlInsert;
        protected DBCommand dmlInsertSequence;
        protected DBCommand dmlDelete;
        protected IDBLogTable logTable;
        protected DBTableGroup tableGroup;
        protected DBSequence cacheSequence;
        protected readonly List<IDBVirtualTable> virtualTables = new List<IDBVirtualTable>(0);
        protected readonly ConcurrentDictionary<string, QQuery> queryChache = new ConcurrentDictionary<string, QQuery>();
        protected DBColumn<string> nameKey = DBColumn<string>.EmptyKey;
        protected DBColumn<byte[]> accessKey = DBColumn<byte[]>.EmptyKey;
        protected DBColumn primaryKey = DBColumn.EmptyKey;
        protected DBColumn<byte[]> fileKey = DBColumn<byte[]>.EmptyKey;
        protected DBColumn<string> fileNameKey = DBColumn<string>.EmptyKey;
        protected DBColumn<long?> fileBLOBKey = DBColumn<long?>.EmptyKey;
        protected DBColumn<DateTime?> fileLastWriteKey = DBColumn<DateTime?>.EmptyKey;
        protected DBColumn<DateTime> dateKey = DBColumn<DateTime>.EmptyKey;
        protected DBColumn<DateTime> stampKey = DBColumn<DateTime>.EmptyKey;
        protected DBColumn<DateTime?> replicateStampKey = DBColumn<DateTime?>.EmptyKey;
        protected DBColumn<string> codeKey = DBColumn<string>.EmptyKey;
        protected DBColumn typeKey = DBColumn.EmptyKey;
        protected DBColumn groupKey = DBColumn.EmptyKey;
        protected DBColumn<DBStatus> stateKey = DBColumn<DBStatus>.EmptyKey;
        protected DBColumn<byte[]> imageKey = DBColumn<byte[]>.EmptyKey;
        protected DBColumn<int> itemTypeKey = DBColumn<int>.EmptyKey;

        public IComparer DefaultComparer;
        public int Hash = -1;
        protected internal readonly int index = ++tableIndex;
        protected internal ConcurrentQueue<int> FreeHandlers = new ConcurrentQueue<int>();
        protected readonly ConcurrentDictionary<Type, List<DBColumn>> mapTypeColumn = new ConcurrentDictionary<Type, List<DBColumn>>();
        protected readonly ConcurrentDictionary<Type, List<IInvoker>> refingInvokers = new ConcurrentDictionary<Type, List<IInvoker>>();
        protected string query;
        protected string comInsert;
        protected string comUpdate;
        protected string comDelete;
        protected string groupName;
        protected string sequenceName;
        protected string logTableName;
        protected DBTableKeys keys = DBTableKeys.None;
        protected DBTableType type = DBTableType.Table;
        protected int blockSize = 256;
        internal object locker = new object();
        private DBItemType itemType;
        private int itemTypeIndex = 0;
        private DBColumn[] refInvoker;

        protected DBTable(string name = null) : base(name)
        {
            ColumnGroups = new DBColumnGroupList(this);
            Columns = new DBColumnList<DBColumn>(this);
            Indexes = new DBIndexList(this);
            Constraints = new DBConstraintList<DBConstraint>(this);
            Foreigns = new DBForeignList(this);
        }

        [Browsable(false)]
        public string LogTableName
        {
            get => logTableName;
            set
            {
                if (logTableName != value)
                {
                    logTableName = value;
                    if (!string.IsNullOrEmpty(logTableName))
                    {
                        Keys &= ~DBTableKeys.NoLogs;
                    }
                    OnPropertyChanged();
                }
            }
        }

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
            set
            {
                if (itemTypeIndex != value)
                {
                    itemTypeIndex = value;
                    OnPropertyChanged();
                }
            }
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

        [XmlIgnore, JsonIgnore, Browsable(false)]
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
        public DBColumn<byte[]> AccessKey => accessKey == DBColumn<byte[]>.EmptyKey ? (accessKey = (DBColumn<byte[]>)Columns.GetByKey(DBColumnKeys.Access)) : accessKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn PrimaryKey => primaryKey == DBColumn.EmptyKey ? (primaryKey = Columns.GetByKey(DBColumnKeys.Primary)) : primaryKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn<long?> FileBLOBKey => fileBLOBKey == DBColumn<long?>.EmptyKey ? (fileBLOBKey = (DBColumn<long?>)Columns.GetByKey(DBColumnKeys.FileLOB)) : fileBLOBKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn<byte[]> FileKey => fileKey == DBColumn<byte[]>.EmptyKey ? (fileKey = (DBColumn<byte[]>)Columns.GetByKey(DBColumnKeys.File)) : fileKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn<string> FileNameKey => fileNameKey == DBColumn<string>.EmptyKey ? (fileNameKey = (DBColumn<string>)Columns.GetByKey(DBColumnKeys.FileName)) : fileNameKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn<DateTime?> FileLastWriteKey => fileLastWriteKey == DBColumn<DateTime?>.EmptyKey ? (fileLastWriteKey = (DBColumn<DateTime?>)Columns.GetByKey(DBColumnKeys.FileLastWrite)) : fileLastWriteKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn<DateTime?> ReplicateStampKey => replicateStampKey == DBColumn<DateTime?>.EmptyKey ? (replicateStampKey = (DBColumn<DateTime?>)Columns.GetByKey(DBColumnKeys.ReplicateStamp)) : replicateStampKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn<DateTime> StampKey => stampKey == DBColumn<DateTime>.EmptyKey ? (stampKey = (DBColumn<DateTime>)Columns.GetByKey(DBColumnKeys.Stamp)) : stampKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn<DateTime> DateKey => dateKey == DBColumn<DateTime>.EmptyKey ? (dateKey = (DBColumn<DateTime>)Columns.GetByKey(DBColumnKeys.Date)) : dateKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn GroupKey => groupKey == DBColumn.EmptyKey ? (groupKey = Columns.GetByKey(DBColumnKeys.Group)) : groupKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn ElementTypeKey => typeKey == DBColumn.EmptyKey ? (typeKey = Columns.GetByKey(DBColumnKeys.ElementType)) : typeKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn<DBStatus> StatusKey => stateKey == DBColumn<DBStatus>.EmptyKey ? (stateKey = (DBColumn<DBStatus>)Columns.GetByKey(DBColumnKeys.State)) : stateKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn<int> ItemTypeKey => itemTypeKey == DBColumn<int>.EmptyKey ? (itemTypeKey = (DBColumn<int>)Columns.GetByKey(DBColumnKeys.ItemType)) : itemTypeKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn<string> CodeKey => codeKey == DBColumn<string>.EmptyKey ? (codeKey = (DBColumn<string>)Columns.GetByKey(DBColumnKeys.Code)) : codeKey;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBColumn<byte[]> ImageKey => imageKey == DBColumn<byte[]>.EmptyKey ? (imageKey = (DBColumn<byte[]>)Columns.GetByKey(DBColumnKeys.Image)) : imageKey;

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

        [Category("Database")]
        public DBTableKeys Keys
        {
            get => keys;
            set
            {
                if (keys != value)
                {
                    keys = value;
                    OnPropertyChanged();
                }
            }
        }

        [XmlIgnore, JsonIgnore, Category("Database")]
        public bool IsCaching
        {
            get => (Keys & DBTableKeys.Caching) != 0;
            set
            {
                if (IsCaching != value)
                {
                    if (value)
                    {
                        Keys |= DBTableKeys.Caching;
                    }
                    else
                    {
                        Keys &= ~DBTableKeys.Caching;
                    }
                    OnPropertyChanged();
                }
            }
        }

        [XmlIgnore, JsonIgnore, Category("Database")]
        public bool IsReadOnly
        {
            get => (Keys & DBTableKeys.ReadOnly) != 0;
            set
            {
                if (IsReadOnly != value)
                {
                    if (value)
                    {
                        Keys |= DBTableKeys.ReadOnly;
                    }
                    else
                    {
                        Keys &= ~DBTableKeys.ReadOnly;
                    }
                    OnPropertyChanged();
                }
            }
        }

        [XmlIgnore, JsonIgnore, Category("Database")]
        public bool IsPrivate
        {
            get => (Keys & DBTableKeys.Private) != 0;
            set
            {
                if (IsPrivate != value)
                {
                    if (value)
                    {
                        Keys |= DBTableKeys.Private;
                    }
                    else
                    {
                        Keys &= ~DBTableKeys.Private;
                    }
                    OnPropertyChanged();
                }
            }
        }

        [XmlIgnore, JsonIgnore, Category("Database")]
        public virtual bool IsLoging
        {
            get => (Keys & DBTableKeys.NoLogs) == 0;
            set
            {
                if (value)
                {
                    Keys &= ~DBTableKeys.NoLogs;
                }
                else
                {
                    Keys |= DBTableKeys.NoLogs;
                    LogTable = null;
                }
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
        public abstract IDBTableView DefaultItemsView { get; }

        public List<DBForeignKey> ChildRelations { get; } = new List<DBForeignKey>();

        public abstract bool Contains(DBItem item);

        public abstract bool Remove(DBItem item);

        public abstract IEnumerator<DBItem> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public abstract void CopyTo(DBItem[] array, int arrayIndex);

        public abstract void OnItemChanging<V>(DBItem item, string property, DBColumn<V> column, V value);
        public abstract void OnItemChanged<V>(DBItem item, string proeprty, DBColumn<V> column, V value);
        public abstract void Trunc();

        public bool IsSerializeableColumn(DBColumn column, Type type)
        {
            return column.PropertyName != null
                && column.PropertyInvoker != null && column.PropertyInvoker != column
                && column.PropertyInvoker.TargetType.IsAssignableFrom(type)
                && !TypeHelper.IsNonSerialize(column.PropertyInfo)
                //&& (column.Attribute.Keys & DBColumnKeys.Access) != DBColumnKeys.Access
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

        public DBColumn<T> ParseProperty<T>(string property)
        {
            return (DBColumn<T>)ParseProperty(property);
        }

        protected internal void SetItemType(Type type)
        {
            DefaultComparer = DBItemDefaultComparer.Instance;
            itemType = ItemTypes[0] = new DBItemType { Type = type };
            OnPropertyChanged(nameof(ItemType));
            OnPropertyChanged(nameof(ItemTypeName));
            // Info = DBService.GetTableAttribute(type);
        }

        public IEnumerable<DBTable> GetVirtualTables()
        {
            foreach (var item in virtualTables)
            {
                yield return (DBTable)item;
            }
        }

        public DBTable GetVirtualTable(int itemType)
        {
            if (itemType == 0)
                return this;
            return virtualTables.FirstOrDefault(p => p.ItemTypeIndex == itemType) as DBTable;
        }

        public void RefreshSequence(bool truncate = false)
        {
            using (var transaction = new DBTransaction(Connection))
            {
                RefreshSequence(transaction, truncate);
                transaction.Commit();
            }
        }

        public void RefreshSequence(DBTransaction transaction, bool truncate = false)
        {
            var maximum = DBSequence.Convert(transaction.ExecuteQuery($"select max({PrimaryKey.SqlName}) from {SqlName}"));
            if (!truncate)
            {
                var current = Sequence.GetCurrent(transaction);
                if (current >= maximum)
                {
                    return;
                }
            }
            Sequence.SetCurrent(maximum);
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

        public DBColumn<string> GetNameKey(string group)
        {
            if (group.Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                if (nameKey == DBColumn<string>.EmptyKey)
                {
                    return nameKey = GetCultureColumn(group, Locale.Instance.Culture);
                }
                return nameKey;
            }
            return GetCultureColumn(group, Locale.Instance.Culture);
        }

        public DBColumn<string> GetCultureColumn(string group, CultureInfo culture)
        {
            return (DBColumn<string>)Columns.GetByGroup(@group)
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
                column = DBColumnFactory.Create(typeof(Nullable<>).MakeGenericType(type), name: name, size: -1, table: this);
                Columns.Add(column);
                newCol = true;
            }
            return column;
        }

        public abstract DBItem this[int index] { get; }

        public abstract void Add(DBItem item);

        public abstract DBItem LoadItemFromReader(DBTransaction transaction);

        public abstract IEnumerable<DBItem> LoadItemsCache(string filter, DBLoadParam loadParam = DBLoadParam.Referencing, DBTransaction transaction = null);

        public abstract IEnumerable<DBItem> LoadItems(QQuery query, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null);

        public abstract IEnumerable<DBItem> LoadItems(string whereText = null, DBLoadParam param = DBLoadParam.None, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null);

        public abstract IEnumerable<DBItem> LoadItems(IDbCommand command, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null);

        public abstract Task<IEnumerable<DBItem>> LoadItemsAsync(IDbCommand command, DBLoadParam param = DBLoadParam.None, DBTransaction transaction = null);

        public abstract DBItem LoadItemByCode(string code, DBColumn<string> column, DBLoadParam param, DBTransaction transaction = null);

        public abstract DBItem LoadItemById(object id, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null);

        public abstract DBItem LoadItemById<K>(K id, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null);

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
            if (FreeHandlers.TryDequeue(out var handler))
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
            var args = new DBItemEventArgs(item, transaction);

            if (!transaction.Replication)
            {
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
                if (!item.OnUpdating(args))
                    return false;
            }
            else
            {
                item.ReplicateStamp = item.Stamp;
            }

            transaction.AddItem(item);

            var columns = item.GetChangeKeys().ToList();
            args.Columns = columns;
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
            //transaction.PrepareStatements(command);

            var result = await transaction.ExecuteQueryAsync(command, dmlCommand == dmlInsertSequence ? DBExecuteType.Scalar : DBExecuteType.NoReader);
            transaction.DbConnection.System.UploadCommand(item, command);
            if (dmlCommand == dmlInsertSequence)
            {
                item[PrimaryKey] = result;
                Sequence.SetCurrent(result);
            }

            if (!transaction.Replication
                && !transaction.NoLogs
                && LogTable != null)
            {
                args.LogItem = (DBLogItem)LogTable.NewItem(DBUpdateState.Insert, false, item.ItemType);
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
                foreach (QParam param in query.GetAllParameters())
                {
                    if (param.RightIsColumn)
                    {
                        DBColumn column = param.RightColumn;
                        if (column.Table == this)
                        {
                            param.RightQColumn.Temp = item.GetValue(column);
                        }
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

        public string BuildQuery(string whereFilter, string alias, IEnumerable<DBColumn> cols = null, string function = null)
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
                column.PullIndex?.Clear();
                if (pool)
                {
                    column.Clear();
                }
            }
        }

        public IDbCommand CreateKeyCommmand(object key, DBColumn column, IEnumerable<DBColumn> cols = null)
        {
            string idName = System.ParameterPrefix + column.SqlName;
            var command = System.CreateCommand(Schema.Connection, BuildQuery($"where a.{column.SqlName}={idName}", "a", cols));
            System.CreateParameter(command, idName, key, column);
            return command;
        }

        public IDbCommand CreateKeyCommmand<K>(K key, DBColumn<K> column, IEnumerable<DBColumn> cols = null)
        {
            string idName = System.ParameterPrefix + column.SqlName;
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

        public override string FormatSql(DDLType ddlType, bool dependency = false)
        {
            var ddl = new StringBuilder();
            Schema?.Connection?.System.Format(ddl, this, ddlType, dependency, false);
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
                using (var invokerWriter = new BinaryInvokerWriter(writer))
                {
                    invokerWriter.WriteArrayBegin();
                    invokerWriter.WriteArrayLength(Count);
                    var map = DBItemSerializer.Instance.WriteMap(invokerWriter, ItemType.Type, this);
                    foreach (DBItem item in this)
                    {
                        invokerWriter.WriteArrayEntry();
                        DBItemSerializer.Instance.Write(invokerWriter, item, null, map);
                    }
                    invokerWriter.WriteArrayEnd();
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
                using (var invokerReader = new BinaryInvokerReader(reader))
                {
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
                    var map = (Dictionary<ushort, IPropertySerializeInfo>)null;
                    if (invokerReader.CurrentToken == BinaryToken.SchemaBegin)
                    {
                        map = DBItemSerializer.Instance.ReadMap(invokerReader, out _, out _);
                        invokerReader.ReadToken();
                    }

                    while (invokerReader.CurrentToken == BinaryToken.ArrayEntry)
                    {
                        DBItem item = NewItem(DBUpdateState.Default, false);
                        DBItemSerializer.Instance.Read(invokerReader, item, null, map);
                        Add(item);
                        item.Accept((IUserIdentity)null);
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
            table.keys = Keys;
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
            accessKey = DBColumn<byte[]>.EmptyKey;
            primaryKey = DBColumn.EmptyKey;
            dateKey = DBColumn<DateTime>.EmptyKey;
            stampKey = DBColumn<DateTime>.EmptyKey;
            replicateStampKey = DBColumn<DateTime?>.EmptyKey;
            fileLastWriteKey = DBColumn<DateTime?>.EmptyKey;
            fileBLOBKey = DBColumn<long?>.EmptyKey;
            fileNameKey = DBColumn<string>.EmptyKey;
            codeKey = DBColumn<string>.EmptyKey;
            itemTypeKey = DBColumn<int>.EmptyKey;
            typeKey = DBColumn.EmptyKey;
            groupKey = DBColumn.EmptyKey;
            stateKey = DBColumn<DBStatus>.EmptyKey;
            imageKey = DBColumn<byte[]>.EmptyKey;
        }

        public IEnumerable<DBColumn> GetRefColumns()
        {
            return refInvoker ?? (refInvoker = new DBColumn[] { ItemTypeKey, PrimaryKey });
        }

        public IEnumerable<IInvoker> GetRefingInvokers<T>()
        {
            return GetRefingInvokers(typeof(T));
        }

        public IEnumerable<IInvoker> GetRefingInvokers(Type t)
        {
            return refingInvokers.GetOrAdd(t, CreateInvokers);
            List<IInvoker> CreateInvokers(Type type)
            {
                var refingInvokers = new List<IInvoker>(Generator?.Referencings.Count() ?? 0);
                if (Generator != null)
                {
                    foreach (var refing in Generator.Referencings)
                    {
                        if (!refing.PropertyInvoker.TargetType.IsAssignableFrom(type)
                            || TypeHelper.IsNonSerialize(refing.PropertyInfo))
                            continue;
                        refingInvokers.Add((IInvoker)refing.PropertyInvoker);
                    }
                }
                return refingInvokers;
            }
        }

        public ReferencingGenerator ParseReferencing(string property)
        {
            if (Generator != null)
            {
                return Generator.GetReferencingByProperty(property);
            }
            return null;
        }

        public IEnumerable<DBColumn> GetTypeColumns<T>()
        {
            return GetTypeColumns(typeof(T));
        }

        public IEnumerable<DBColumn> GetTypeColumns(Type t)
        {
            return mapTypeColumn.GetOrAdd(t, CreateInvokers);
            List<DBColumn> CreateInvokers(Type type)
            {
                var columns = new List<DBColumn>(Columns.Count);
                foreach (var column in Columns)
                {
                    if (!IsSerializeableColumn(column, type))
                        continue;

                    columns.Add(column);
                }
                return columns;
            }
        }

        public IInvoker GetInvoker(string property)
        {
            var column = Columns.GetByProperty(property);
            if (column != null)
            {
                return column.PropertyInvoker;
            }

            var reference = Columns.GetByReferenceProperty(property);
            if (reference != null)
            {
                return reference.ReferencePropertyInvoker;
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
                LogTable = (IDBLogTable)GetTable(itemType, Schema.LogSchema ?? Schema, true);
                if (LogTable == null)
                {
                    var tableGenerator = new LogTableGenerator()
                    {
                        Attribute = new LogTableAttribute(ItemType.Type, Name + "_log") { SequenceName = SequenceName + "_log" },
                        Schema = Schema.LogSchema ?? Schema,
                        BaseTableGenerator = Generator
                    };
                    tableGenerator.Initialize(itemType);
                    LogTable = (IDBLogTable)tableGenerator.Generate();
                }
                LogTable.BaseTable = this;
                if (!LogTable.Schema.Tables.Contains(LogTable))
                {
                    LogTable.Schema.Tables.Add((DBTable)LogTable);
                }
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

        public DBSequence GenerateSequence(string sequenceName = null)
        {
            var sname = sequenceName ?? $"seq_{Name}";
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
                (Type type, int size, int scale) = columnInfo.GetDataType();
                var column = InitColumn(type, columnInfo.Name);
                column.Size = size;
                column.Scale = scale;
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
                    LeftItem = new QColumn(StatusKey),
                    Comparer = CompareType.In,
                    RightItem = GetStatusEnum(status)
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
                    LeftItem = new QColumn(ItemTypeKey),
                    Comparer = CompareType.Equal,
                    RightItem = new QValue(typeIndex, ItemTypeKey)
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

        public DBColumn InitColumn(Type type, string name)
        {
            return Columns[name] ?? DBColumnFactory.Create(type, name: name, table: this);
        }

        private DBConstraint InitConstraint(string name)
        {
            return Constraints[name] ?? new DBConstraint() { Table = this, Name = name };
        }

        public void GenerateDefaultColumns()
        {
            Columns.AddRange(new[]
            {
                DBColumnFactory.Create(typeof(int), name: "type_id", keys: DBColumnKeys.ItemType, table: this ),
                DBColumnFactory.Create(typeof(int?), name: "unid", keys: DBColumnKeys.Primary, table: this),
                DBColumnFactory.Create(typeof(DateTime?), name: "datec", keys: DBColumnKeys.Date| DBColumnKeys.UtcDate, table:this),
                DBColumnFactory.Create(typeof(DateTime?), name: "dateu", keys: DBColumnKeys.Stamp| DBColumnKeys.UtcDate, table:this),
                DBColumnFactory.Create(typeof(DBItemState?), name: "stateid", keys: DBColumnKeys.State,table:this),
                DBColumnFactory.Create(typeof(byte[]), name: "access", keys: DBColumnKeys.Access, table:this)
            });
        }

        public virtual void RemoveDeletedColumns()
        {
            for (int i = 0; i < Columns.Count;)
            {
                var column = Columns[i];
                if (column.PropertyName != null && column.PropertyInfo == null)
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

        public bool ParseQuery(string filter, out QQuery query)
        {
            query = null;
            if (!queryChache.TryGetValue(filter, out query))
            {
                query = new QQuery(filter, this);
                queryChache.TryAdd(filter, query);
                return false;
            }
            return true;
        }

        public DateTime? GetReplicateMaxStamp()
        {
            using (var query = new QQuery(this))
            {
                query.Columns.Add(new QFunc(QFunctionType.max, new[] { StampKey }));

                var param = query.Add();
                param.Parameters.Add(query.CreateParam(ReplicateStampKey, CompareType.Is, null));
                param.Parameters.Add(query.CreateParam(LogicType.Or, StampKey, CompareType.Greater, ReplicateStampKey));

                var max = Connection.ExecuteQuery(query.ToCommand(false), true, DBExecuteType.Scalar);
                return max == null || max == DBNull.Value
                    ? (DateTime?)null
                    : DateTime.SpecifyKind((DateTime)max, DateTimeKind.Utc);
            }
        }

        public IEnumerable<DBItem> GetReplicateItems(DateTime? stamp)
        {
            using (var query = new QQuery(this))
            {
                if (stamp != null)
                {
                    query.BuildParam(StampKey, CompareType.GreaterOrEqual, stamp);
                }
                var param = query.Add();
                param.Parameters.Add(query.CreateParam(ReplicateStampKey, CompareType.Is, null));
                param.Parameters.Add(query.CreateParam(LogicType.Or, StampKey, CompareType.Greater, ReplicateStampKey));

                return LoadItems(query, DBLoadParam.Referencing);
            }
        }

        public class GroupNameInvoker : Invoker<DBTable, string>
        {
            public static readonly GroupNameInvoker Instance = new GroupNameInvoker();
            public override string Name => nameof(DBTable.GroupName);

            public override bool CanWrite => true;

            public override string GetValue(DBTable target) => target.GroupName;

            public override void SetValue(DBTable target, string value) => target.GroupName = value;
        }

        public class LogTableNameInvoker : Invoker<DBTable, string>
        {
            public static readonly LogTableNameInvoker Instance = new LogTableNameInvoker();
            public override string Name => nameof(DBTable.LogTableName);

            public override bool CanWrite => true;

            public override string GetValue(DBTable target) => target.LogTableName;

            public override void SetValue(DBTable target, string value) => target.LogTableName = value;
        }

        public class LogTableInvoker : Invoker<DBTable, IDBLogTable>
        {
            public static readonly LogTableInvoker Instance = new LogTableInvoker();
            public override string Name => nameof(DBTable.LogTable);

            public override bool CanWrite => true;

            public override IDBLogTable GetValue(DBTable target) => target.LogTable;

            public override void SetValue(DBTable target, IDBLogTable value) => target.LogTable = value;
        }

        public class SequenceNameInvoker : Invoker<DBTable, string>
        {
            public static readonly SequenceNameInvoker Instance = new SequenceNameInvoker();
            public override string Name => nameof(DBTable.SequenceName);

            public override bool CanWrite => true;

            public override string GetValue(DBTable target) => target.SequenceName;

            public override void SetValue(DBTable target, string value) => target.SequenceName = value;
        }

        public class SequenceInvoker : Invoker<DBTable, DBSequence>
        {
            public static readonly SequenceInvoker Instance = new SequenceInvoker();
            public override string Name => nameof(DBTable.Sequence);

            public override bool CanWrite => true;

            public override DBSequence GetValue(DBTable target) => target.Sequence;

            public override void SetValue(DBTable target, DBSequence value) => target.Sequence = value;
        }

        public class BlockSizeInvoker : Invoker<DBTable, int>
        {
            public static readonly BlockSizeInvoker Instance = new BlockSizeInvoker();
            public override string Name => nameof(DBTable.BlockSize);

            public override bool CanWrite => true;

            public override int GetValue(DBTable target) => target.BlockSize;

            public override void SetValue(DBTable target, int value) => target.BlockSize = value;
        }

        public class ColumnsInvoker : Invoker<DBTable, DBColumnList<DBColumn>>
        {
            public static readonly ColumnsInvoker Instance = new ColumnsInvoker();
            public override string Name => nameof(DBTable.Columns);

            public override bool CanWrite => true;

            public override DBColumnList<DBColumn> GetValue(DBTable target) => target.Columns;

            public override void SetValue(DBTable target, DBColumnList<DBColumn> value) => target.Columns = value;
        }

        public class ItemTypeInvoker : Invoker<DBTable, DBItemType>
        {
            public static readonly ItemTypeInvoker Instance = new ItemTypeInvoker();
            public override string Name => nameof(DBTable.ItemType);

            public override bool CanWrite => false;

            public override DBItemType GetValue(DBTable target) => target.ItemType;

            public override void SetValue(DBTable target, DBItemType value) { }
        }

        public class ItemTypeNameInvoker : Invoker<DBTable, string>
        {
            public static readonly ItemTypeNameInvoker Instance = new ItemTypeNameInvoker();
            public override string Name => nameof(DBTable.ItemTypeName);

            public override bool CanWrite => false;

            public override string GetValue(DBTable target) => target.ItemTypeName;

            public override void SetValue(DBTable target, string value) { }
        }

        public class ColumnGroupsInvoker : Invoker<DBTable, DBColumnGroupList>
        {
            public override string Name => nameof(DBTable.ColumnGroups);

            public override bool CanWrite => true;

            public override DBColumnGroupList GetValue(DBTable target) => target.ColumnGroups;

            public override void SetValue(DBTable target, DBColumnGroupList value) => target.ColumnGroups = value;
        }

        public class IndexesInvoker : Invoker<DBTable, DBIndexList>
        {
            public override string Name => nameof(DBTable.Indexes);

            public override bool CanWrite => true;

            public override DBIndexList GetValue(DBTable target) => target.Indexes;

            public override void SetValue(DBTable target, DBIndexList value) => target.Indexes = value;
        }

        public class ForeignsInvoker : Invoker<DBTable, DBForeignList>
        {
            public override string Name => nameof(DBTable.Foreigns);

            public override bool CanWrite => true;

            public override DBForeignList GetValue(DBTable target) => target.Foreigns;

            public override void SetValue(DBTable target, DBForeignList value) => target.Foreigns = value;
        }

        public class ConstraintsInvoker : Invoker<DBTable, DBConstraintList<DBConstraint>>
        {
            public override string Name => nameof(DBTable.Constraints);

            public override bool CanWrite => true;

            public override DBConstraintList<DBConstraint> GetValue(DBTable target) => target.Constraints;

            public override void SetValue(DBTable target, DBConstraintList<DBConstraint> value) => target.Constraints = value;
        }

        public class ItemTypesInvoker : Invoker<DBTable, Dictionary<int, DBItemType>>
        {
            public override string Name => nameof(DBTable.ItemTypes);

            public override bool CanWrite => true;

            public override Dictionary<int, DBItemType> GetValue(DBTable target) => target.ItemTypes;

            public override void SetValue(DBTable target, Dictionary<int, DBItemType> value) => target.ItemTypes = value;
        }

        public class QueryInvoker : Invoker<DBTable, string>
        {
            public override string Name => nameof(DBTable.Query);

            public override bool CanWrite => true;

            public override string GetValue(DBTable target) => target.Query;

            public override void SetValue(DBTable target, string value) => target.Query = value;
        }

        public class TypeInvoker : Invoker<DBTable, DBTableType>
        {
            public override string Name => nameof(DBTable.Type);

            public override bool CanWrite => true;

            public override DBTableType GetValue(DBTable target) => target.Type;

            public override void SetValue(DBTable target, DBTableType value) => target.Type = value;
        }

        public class IsCachingInvoker : Invoker<DBTable, bool>
        {
            public override string Name => nameof(DBTable.IsCaching);

            public override bool CanWrite => true;

            public override bool GetValue(DBTable target) => target.IsCaching;

            public override void SetValue(DBTable target, bool value) => target.IsCaching = value;
        }

        public class ComDeleteInvoker : Invoker<DBTable, string>
        {
            public override string Name => nameof(DBTable.ComDelete);

            public override bool CanWrite => true;

            public override string GetValue(DBTable target) => target.ComDelete;

            public override void SetValue(DBTable target, string value) => target.ComDelete = value;
        }

        public class ComInsertInvoker : Invoker<DBTable, string>
        {
            public override string Name => nameof(DBTable.ComInsert);

            public override bool CanWrite => true;

            public override string GetValue(DBTable target) => target.ComInsert;

            public override void SetValue(DBTable target, string value) => target.ComInsert = value;
        }

        public class ComUpdateInvoker : Invoker<DBTable, string>
        {
            public override string Name => nameof(DBTable.ComUpdate);

            public override bool CanWrite => true;

            public override string GetValue(DBTable target) => target.ComUpdate;

            public override void SetValue(DBTable target, string value) => target.ComUpdate = value;
        }

        public class SqlNameInvoker : Invoker<DBTable, string>
        {
            public override string Name => nameof(DBTable.SqlName);

            public override bool CanWrite => false;

            public override string GetValue(DBTable target) => target.SqlName;

            public override void SetValue(DBTable target, string value) { }
        }

        public class CountInvoker : Invoker<DBTable, int>
        {
            public override string Name => nameof(DBTable.Count);

            public override bool CanWrite => false;

            public override int GetValue(DBTable target) => target.Count;

            public override void SetValue(DBTable target, int value) { }
        }

        public class IsReadOnlyInvoker : Invoker<DBTable, bool>
        {
            public override string Name => nameof(DBTable.IsReadOnly);

            public override bool CanWrite => true;

            public override bool GetValue(DBTable target) => target.IsReadOnly;

            public override void SetValue(DBTable target, bool value) => target.IsReadOnly = value;
        }

        public class IsPrivateInvoker : Invoker<DBTable, bool>
        {
            public override string Name => nameof(DBTable.IsPrivate);

            public override bool CanWrite => true;

            public override bool GetValue(DBTable target) => target.IsPrivate;

            public override void SetValue(DBTable target, bool value) => target.IsPrivate = value;
        }

        public class DefaultItemsViewInvoker : Invoker<DBTable, IDBTableView>
        {
            public override string Name => nameof(DBTable.DefaultItemsView);

            public override bool CanWrite => false;

            public override IDBTableView GetValue(DBTable target) => target.DefaultItemsView;

            public override void SetValue(DBTable target, IDBTableView value) { }
        }

        public class ChildRelationsInvoker : Invoker<DBTable, List<DBForeignKey>>
        {
            public override string Name => nameof(DBTable.ChildRelations);

            public override bool CanWrite => false;

            public override List<DBForeignKey> GetValue(DBTable target) => target.ChildRelations;

            public override void SetValue(DBTable target, List<DBForeignKey> value) { }
        }

        public class KeysInvoker : Invoker<DBTable, DBTableKeys>
        {
            public override string Name => nameof(DBTable.Keys);

            public override bool CanWrite => true;

            public override DBTableKeys GetValue(DBTable target) => target.Keys;

            public override void SetValue(DBTable target, DBTableKeys value) => target.Keys = value;
        }
    }
}
