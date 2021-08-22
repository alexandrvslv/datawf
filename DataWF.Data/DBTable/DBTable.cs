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
using DataWF.Data;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [InvokerGenerator(Instance = true)]
    public abstract partial class DBTable : DBSchemaItem, IComparable, IDBTable, IAccessable
    {
        private static int tableIndex;

        protected DBCommand dmlInsert;
        protected DBCommand dmlDelete;
        protected IDBTableLog logTable;
        protected DBTableGroup tableGroup;
        protected DBSequence cacheSequence;
        protected readonly List<DBTable> virtualTables = new List<DBTable>(0);
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
        public int Hash = -1;
        protected internal int index = ++tableIndex;
        protected internal ConcurrentQueue<PullHandler> FreeHandlers = new ConcurrentQueue<PullHandler>();
        protected readonly ConcurrentDictionary<Type, List<DBColumn>> mapTypeColumn = new ConcurrentDictionary<Type, List<DBColumn>>();
        protected readonly ConcurrentDictionary<Type, List<DBReferencing>> mapTypeRefing = new ConcurrentDictionary<Type, List<DBReferencing>>();
        protected string query;
        protected string comInsert;
        protected string comUpdate;
        protected string comDelete;
        protected string groupName;
        protected string sequenceName;
        protected string logTableName;
        private IDBTable parentTable;
        protected string parentTableName;
        protected DBTableKeys keys = DBTableKeys.None;
        protected DBTableType type = DBTableType.Table;
        protected int blockSize = 256;
        internal object locker = new object();
        private DBItemType itemType;
        private int itemTypeIndex = 0;
        private DBColumn[] refInvoker;

        protected DBTable(string name = null) : base(name)
        {
            DefaultComparer = DBItemDefaultComparer<DBItem>.Instance;

            ColumnGroups = new DBColumnGroupList(this);
            Columns = new DBColumnList<DBColumn>(this);
            Indexes = new DBIndexList(this);
            Constraints = new DBConstraintList<DBConstraint>(this);
            Foreigns = new DBForeignList(this);
            Referencings = new DBTableItemList<DBReferencing>(this);
        }

        [XmlIgnore, JsonIgnore]
        public IComparer DefaultComparer { get; set; }

        [XmlIgnore, JsonIgnore]
        public TableGenerator Generator { get; internal set; }

        [XmlAttribute, Browsable(false), Category("Database")]
        public string ParentTableName
        {
            get => parentTableName;
            set
            {
                if (!string.Equals(parentTableName, value, StringComparison.Ordinal))
                {
                    parentTableName = value;
                    parentTable = null;
                    OnPropertyChanged(DDLType.Alter);
                }
            }
        }

        [XmlIgnore, JsonIgnore, Category("Database")]
        public IDBTable ParentTable
        {
            get => parentTable ?? (parentTable = Schema?.Tables[parentTableName]);
            set
            {
                if (ParentTable != value)
                {
                    ParentTableName = value?.Name;
                    parentTable = (DBTable)value;
                    if (parentTable != null)
                    {
                        BlockSize = parentTable.BlockSize;
                    }
                }
            }
        }

        [XmlIgnore, JsonIgnore, Category("Database")]
        public abstract QQuery FilterQuery { get; set; }

        [Browsable(false)]
        public string LogTableName
        {
            get => logTableName;
            set
            {
                if (logTableName != value)
                {
                    logTable = null;
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
        public IDBTableLog LogTable
        {
            get => logTable ??= ((IDBTableLog)Schema?.LogSchema?.Tables[LogTableName] ?? (IDBTableLog)Schema?.Tables[LogTableName]);
            set
            {
                LogTableName = value?.Name;
                logTable = value;
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

        [XmlIgnore, JsonIgnore]
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
                    OnPropertyChanged(DDLType.Alter);
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
                    foreach (var table in virtualTables)
                    {
                        table.BlockSize = value;
                    }
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public virtual string SqlName => IsVirtual ? ParentTableName : name;

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
                GroupName = value?.Name;
                tableGroup = value;
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
                    cacheSequence = null;
                    sequenceName = value;
                    OnPropertyChanged(nameof(SequenceName));
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public DBSequence Sequence
        {
            get => cacheSequence ??= Schema?.Sequences[SequenceName];
            set
            {
                SequenceName = value?.Name;
                cacheSequence = value;
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
        public DBColumn<long?> FileOIDKey => fileBLOBKey == DBColumn<long?>.EmptyKey ? (fileBLOBKey = (DBColumn<long?>)Columns.GetByKey(DBColumnKeys.FileOID)) : fileBLOBKey;

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
        public DBColumn<DateTime> DateCreateKey => dateKey == DBColumn<DateTime>.EmptyKey ? (dateKey = (DBColumn<DateTime>)Columns.GetByKey(DBColumnKeys.Date)) : dateKey;

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
                OnPropertyChanged(DDLType.Alter);
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

        [XmlIgnore, JsonIgnore, Category("Database")]
        public virtual bool IsVirtual
        {
            get => (Keys & DBTableKeys.Virtual) != 0;
            set
            {
                if (!value)
                {
                    Keys &= ~DBTableKeys.Virtual;
                }
                else
                {
                    Keys |= DBTableKeys.Virtual;
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

        [XmlIgnore, JsonIgnore]
        public DBTableItemList<DBReferencing> Referencings { get; set; }

        [XmlIgnore, JsonIgnore]
        public abstract int Count { get; }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public abstract IDBTableView DefaultItemsView { get; }

        [XmlIgnore, JsonIgnore]
        public List<DBForeignKey> ChildRelations { get; } = new List<DBForeignKey>();

        public abstract bool Contains(DBItem item);

        public abstract bool Remove(DBItem item);

        public abstract IEnumerator<DBItem> GetItemEnumerator();

        IEnumerator<DBItem> IEnumerable<DBItem>.GetEnumerator() => GetItemEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetItemEnumerator();

        public abstract void CopyTo(DBItem[] array, int arrayIndex);

        protected internal abstract void OnItemChanging<V>(DBItem item, string property, DBColumn<V> column, V value);
        protected internal abstract void OnItemChanged<V>(DBItem item, string proeprty, DBColumn<V> column, V value);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DBColumn<T> ParseColumn<T>(string property, ref DBColumn<T> cache)
        {
            return cache ??= (DBColumn<T>)Columns[property];
        }

        public DBColumn<T> ParseColumn<T>(string property)
        {
            return (DBColumn<T>)Columns[property];
        }

        public DBColumn ParseColumnProperty(string property)
        {
            return Columns[property]
                ?? ParseProperty(property);
        }

        public virtual DBColumn ParseColumn(string name)
        {
            DBTable table = this;
            int s = 0, i = name.IndexOf('.');
            while (i > 0)
            {
                var column = table.Columns[name.Substring(s, i - s)];
                if (column == null)
                    break;
                if (column.IsReference)
                    table = (DBTable)column.ReferenceTable;
                s = i + 1;
                i = name.IndexOf('.', s);
            }
            return table.Columns[name.Substring(s)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DBColumn ParseProperty(string property, ref DBColumn cache)
        {
            return cache ??= ParseProperty(property);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DBColumn<T> ParseProperty<T>(string property, ref DBColumn<T> cache)
        {
            return cache ??= ParseProperty<T>(property);
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
            itemType = ItemTypes[0] = new DBItemType { Type = type };
            OnPropertyChanged(nameof(ItemType));
            OnPropertyChanged(nameof(ItemTypeName));
            // Info = DBService.GetTableAttribute(type);
        }

        public IEnumerable<IDBTable> GetVirtualTables()
        {
            foreach (var item in virtualTables)
            {
                yield return item;
            }
        }

        public IDBTable GetVirtualTable(int itemType)
        {
            if (itemType == 0)
                return this;
            return virtualTables.FirstOrDefault(p => p.ItemTypeIndex == itemType);
        }

        public DBTable<T> GetVirtualTable<T>() where T : DBItem
        {
            return (DBTable<T>)GetVirtualTable(typeof(T));
        }

        public IDBTable GetVirtualTable(Type type)
        {
            if (type == ItemType.Type)
                return this;
            if (IsVirtual)
                return ParentTable.GetVirtualTable(type);
            return virtualTables.FirstOrDefault(p => p.ItemType.Type == type) ?? this;
        }

        public void RefreshSequence(bool truncate = false)
        {
            using (var transaction = new DBTransaction(this))
            {
                RefreshSequence(transaction, truncate);
                transaction.Commit();
            }
        }

        public void RefreshSequence(DBTransaction transaction, bool truncate = false)
        {
            var result = transaction.ExecuteQuery($"select max({PrimaryKey.SqlName}) from {SqlName}");
            var maximum = result == DBNull.Value ? 0L : Convert.ToInt64(result);
            if (!truncate)
            {
                var current = Sequence.GetCurrent(transaction);
                if (current >= maximum)
                {
                    return;
                }
            }
            Sequence.Current = maximum;
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
            foreach (var reference in Referencings)
            {
                var referenceTable = reference.ReferenceTable;
                var referenceColumn = reference.ReferenceColumn;

                if (!transaction.ReferencingStack.Contains(referenceColumn)
                    && (referenceColumn.Keys & DBColumnKeys.Group) != DBColumnKeys.Group
                    && referenceTable != this
                    && !referenceTable.IsSynchronized
                    && !(referenceTable.IsVirtual))
                {
                    transaction.ReferencingStack.Add(referenceColumn);
                    var subCommand = DBCommand.CloneCommand(command, referenceTable.BuildQuery($@"
    left join {SqlName} {oldAlias} on {oldAlias}.{PrimaryKey.SqlName} = {newAlias}.{referenceColumn.SqlName} 
    {where}", newAlias, null));
                    //Debug.WriteLine($"Load Referencing: {subCommand.CommandText}");
                    var loadParam = reference.ForceLoadReference ? DBLoadParam.Reference | DBLoadParam.Referencing : DBLoadParam.Referencing;
                    referenceTable.LoadItems(subCommand, loadParam, transaction);
                    transaction.ReferencingStack.Remove(referenceColumn);
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

        public DBColumn<string> GetCultureColumn(string group)
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

        public long GenerateId(DBTransaction transaction = null)
        {
            var id = transaction != null ? Sequence.GetNext(transaction) : Sequence.GetNext();
            var pkSize = (PrimaryKey.SizeOfDataType - 1) * 8;
            var identy = (long)Connection.DataBaseId << pkSize;
            return id | identy;
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
            if (IsVirtual)
            {
                var baseColumn = ParentTable.CheckColumn(name, type, ref newCol);
                if (newCol)
                    Columns.Add(DBColumnFactory.CreateVirtual(baseColumn, this));
                return baseColumn;
            }
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

        public abstract DBItem LoadItemById<K>(K? id, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null) where K : struct;

        public abstract DBItem LoadItemByKey(object key, DBColumn column, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null);

        public abstract DBItem LoadItemByKey<K>(K key, DBColumn<K> column, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null);

        public abstract void ReloadItem(object id, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null);

        public abstract void AddView(IDBTableView view);

        public abstract void RemoveView(IDBTableView view);

        public List<DBItem> LoadItemsById(List<string> ids, DBTransaction transaction)
        {
            var items = new List<DBItem>();
            var primaryKey = PrimaryKey;
            foreach (var id in ids)
            {
                var item = primaryKey.LoadByKey(id, DBLoadParam.Load | DBLoadParam.Referencing, null, transaction);
                if (item != null)
                {
                    items.Add(item);
                }
            }

            return items;
        }
        public abstract void RefreshVirtualTable(DBTable value);
        public abstract void OnBaseTableChanged(DBItem item, NotifyCollectionChangedAction type);

        public event EventHandler<DBItemEventArgs> RowUpdating;

        protected internal virtual bool OnUpdating(DBItemEventArgs e)
        {
            DBService.OnUpdating(e);
            RowUpdating?.Invoke(this, e);
            return !e.Cancel;
        }

        protected internal virtual void OnUpdated(DBItemEventArgs e)
        {
            DBService.OnUpdated(e);
            RowUpdated?.Invoke(this, e);
        }

        protected internal PullHandler GetNextHandler()
        {
            if (IsVirtual)
                return ((DBTable)ParentTable).GetNextHandler();
            if (FreeHandlers.TryDequeue(out var handler))
            {
                return handler;
            }
            return PullHandler.FromSeqence(NextHash(), BlockSize);
        }

        protected internal int NextHash()
        {
            if (IsVirtual)
            {
                return ((DBTable)ParentTable).NextHash();
            }
            return Interlocked.Increment(ref Hash);
        }

        public event EventHandler<DBItemEventArgs> RowUpdated;

        public void DeleteById(object id)
        {
            DBItem row = PrimaryKey.LoadByKey(id);
            row?.Delete();
        }

        public abstract IEnumerable<DBItem> GetChangedItems();

        public virtual async Task<bool> SaveItem(DBItem item, DBTransaction transaction)
        {
            if (IsVirtual)
            {
                return await ParentTable.SaveItem(item, transaction);
            }
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
                    if (DateCreateKey != null)
                        item.DateCreate = DateTime.UtcNow;
                    if (IsLoging && StatusKey != null && !item.IsChangedKey(StatusKey))
                        item.Status = DBStatus.New;
                }
                else if ((item.UpdateState & DBUpdateState.Update) == DBUpdateState.Update)
                {
                    if (StampKey != null)
                        item.Stamp = DateTime.UtcNow;
                    if (IsLoging
                        && StatusKey != null
                        && item.Status == DBStatus.Actual
                        && !item.IsChangedKey(StatusKey)
                        && !item.IsChangedKey(AccessKey))
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
                if (PrimaryKey != null && PrimaryKey.IsEmpty(item))
                {
                    item.GenerateId(transaction);
                }
                dmlCommand = dmlInsert ??= DBCommand.Build(this, comInsert, DBCommandTypes.Insert, Columns);
            }
            else if ((item.UpdateState & DBUpdateState.Delete) == DBUpdateState.Delete)
            {
                dmlCommand = dmlDelete ??= DBCommand.Build(this, comDelete, DBCommandTypes.Delete);
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

            var result = await transaction.ExecuteQueryAsync(command, DBExecuteType.NoReader);
            transaction.DbConnection.System.UploadCommand(item, command);

            if (!transaction.Replication
                && !transaction.NoLogs
                && LogTable != null)
            {
                args.LogItem = (DBItemLog)LogTable.NewItem(DBUpdateState.Insert, false, item.ItemType);
                args.LogItem.BaseItem = item;
                DBService.OnLogItem(args);
                await args.LogItem.Save(transaction.GetSubTransaction(LogTable.Connection));
            }

            item.OnUpdated(args);
            item.UpdateState |= DBUpdateState.Commit;

            return true;
        }

        public abstract void Accept(DBItem item);

        public async Task Save(IEnumerable<DBItem> rows = null)
        {
            using (var transaction = new DBTransaction(this))
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
                    throw;
                }
            }
        }

        public async Task Save(DBTransaction transaction, IEnumerable<DBItem> rows = null)
        {
            if (rows == null)
                rows = GetChangedItems();
            var items = rows.ToList();
            if (items.Count > 0)
            {
                ListHelper.QuickSort<DBItem>(items, new InvokerComparer<DBItem, DBUpdateState>(nameof(DBItem.UpdateState)));
                foreach (DBItem row in items)
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

        public T NewItem<T>(DBUpdateState state = DBUpdateState.Insert, bool def = true) where T : DBItem
        {
            return (T)NewItem(state, def, typeof(T));
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

        public IEnumerable SelectValues(DBItem item, QQuery query, CompareType compare)
        {
            if (query.Columns.Count == 0)
            {
                query.Columns.Add(new QColumn(query.Table.PrimaryKey));
            }
            if (query.IsRefence && item != null)
            {
                foreach (QParam param in query.GetAllParameters())
                {
                    if (param.RightColumn is var column)
                    {
                        if (column.Table == this)
                        {
                            param.RightQColumn.Temp = item.GetValue(column);
                        }
                    }
                }
            }
            if (query.Columns[0] is QColumn qColumn)
            {
                return qColumn.Column.Distinct(query.Select());
            }
            var objects = new List<object>();
            foreach (DBItem row in query.Select())
            {
                object value = query.Columns[0].GetValue(row);
                int index = ListHelper.BinarySearch<object>(objects, value, null);
                if (index < 0)
                {
                    objects.Insert(-index - 1, value);
                }
            }
            return objects;
        }
        public abstract IEnumerable<DBItem> SelectItems(DBColumn column, CompareType comparer, object val);

        public abstract IEnumerable<DBItem> SelectItems(string qQuery);

        public abstract IEnumerable<DBItem> SelectItems(QQuery qQuery);


        #endregion

        public void GetAllChildTables(List<IDBTable> parents)
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

        public IEnumerable<IDBTable> GetChildTables()
        {
            foreach (DBForeignKey rel in GetChildRelations())
            {
                yield return rel.Table;

                if (rel.Table.IsVirtual)
                    yield return rel.Table.ParentTable;
            }
        }

        public void RemoveVirtual(IDBTable view)
        {
            virtualTables.Remove((DBTable)view);
        }

        public void AddVirtual(IDBTable view)
        {
            if (!virtualTables.Contains(view))
            {
                ((DBTable)view).ParentTable = this;
                virtualTables.Add((DBTable)view);
            }
        }

        public virtual IEnumerable<DBForeignKey> GetChildRelations()
        {
            foreach (var item in Schema?.GetAllForeignConstraints(this) ?? Enumerable.Empty<DBForeignKey>())
                yield return item;

            if (IsVirtual)
            {
                foreach (var item in ParentTable.GetChildRelations())
                    yield return item;
            }

        }

        public IEnumerable<DBReferencing> GetPropertyReferencing(Type type)
        {
            foreach (var referencing in Referencings)
            {
                if (referencing.PropertyInvoker.TargetType.IsAssignableFrom(type))
                {
                    yield return referencing;
                }
            }
        }

        public void GetAllParentTables(List<IDBTable> parents)
        {
            foreach (var table in GetParentTables())
            {
                if (table != this && !parents.Contains(table))
                {
                    parents.Add(table);
                    table.GetAllParentTables(parents);
                }
            }
            //return l;
        }

        public IEnumerable<IDBTable> GetParentTables()
        {
            foreach (var item in Foreigns)
            {
                yield return item.ReferenceTable;

                if (item.ReferenceTable.IsVirtual)
                    yield return item.ReferenceTable.ParentTable;
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
            if (IsVirtual)
            {
                var ddlView = new StringBuilder();
                Schema.System.FormatView(ddlView, this, ddlType);
                return ddlView.ToString();
            }

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
                    var itemSerializer = new DBItemSerializer(this);
                    var map = itemSerializer.WriteMap(invokerWriter, ItemType.Type);
                    foreach (DBItem item in this)
                    {
                        invokerWriter.WriteArrayEntry();
                        itemSerializer.Write(invokerWriter, item, null, map);
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
                    var itemSerializer = new DBItemSerializer(this);
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
                    var type = ItemType.Type;
                    var map = (Dictionary<ushort, IPropertySerializeInfo>)null;
                    if (invokerReader.CurrentToken == BinaryToken.SchemaBegin)
                    {
                        map = itemSerializer.ReadMap(invokerReader, out type);
                        invokerReader.ReadToken();
                    }

                    while (invokerReader.CurrentToken == BinaryToken.ArrayEntry)
                    {
                        DBItem item = NewItem(DBUpdateState.Default, false, type);
                        itemSerializer.Read(invokerReader, item, null, map);
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
            table.Schema = Schema;
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

        public IEnumerable<DBReferencing> GetReferencing<T>()
        {
            return GetReferencing(typeof(T));
        }

        public IEnumerable<DBReferencing> GetReferencing(Type t)
        {
            return mapTypeRefing.GetOrAdd(t, CreateInvokers);
            List<DBReferencing> CreateInvokers(Type type)
            {
                var refingInvokers = new List<DBReferencing>(Referencings.Count);
                foreach (var refing in Referencings)
                {
                    if (!refing.PropertyInvoker.TargetType.IsAssignableFrom(type)
                        || !refing.IsSerializable)
                        continue;
                    refingInvokers.Add(refing);
                }

                return refingInvokers;
            }
        }

        public DBReferencing ParseReferencing(string property)
        {
            return Referencings[property];
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

            var refing = Referencings[property];
            if (refing != null)
            {
                return refing.PropertyInvoker;
            }

            return null;
        }

        public IDBTableLog GenerateLogTable()
        {
            if (LogTable == null)
            {
                var genericType = TypeHelper.ParseType(ItemType.Type.Name + "Log");
                var itemType = genericType ?? typeof(DBItemLog);
                LogTable = (IDBTableLog)(Schema.LogSchema ?? Schema).GetTable(itemType, true);
                if (LogTable == null)
                {
                    var tableGenerator = new LogTableGenerator()
                    {
                        Attribute = new LogTableAttribute(ItemType.Type, Name + "_log") { SequenceName = SequenceName + "_log" },
                        BaseTableGenerator = Generator
                    };
                    tableGenerator.Initialize(itemType);
                    LogTable = (IDBTableLog)tableGenerator.Generate(Schema.LogSchema ?? Schema);
                }
                LogTable.TargetTable = this;
                if (!LogTable.Schema.Tables.Contains(LogTable))
                {
                    LogTable.Schema.Tables.Add((DBTable)LogTable);
                }
            }
            else
            {
                LogTable.TargetTable = this;
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
            return primaryKey.LoadByKey(id)?.GetRowText(parametrs, showColumn, separator) ?? "<null>";
        }

        public string GetRowText(object id, bool allColumns, bool showColumn, string separator)
        {
            return primaryKey.LoadByKey(id)?.GetRowText((allColumns ? (IEnumerable<DBColumn>)Columns : Columns.GetIsView()), showColumn, separator);
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
            if (IsVirtual)
            {
                return ParentTable.GetItemType(typeIndex);
            }
            return typeIndex == 0 ? ItemType : ItemTypes[typeIndex];
        }

        public virtual int GetTypeIndex(Type type)
        {
            if (IsVirtual)
            {
                return ParentTable.GetTypeIndex(type);
            }
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
    }
}
