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
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [InvokerGenerator(Instance = true)]
    public abstract partial class DBColumn : DBTableItem, IComparable, IComparable<DBColumn>, ICloneable, IInvoker, IPropertySerializeInfo
    {
        public static readonly DBColumn EmptyKey = new DBColumn<object>();

        public static string GetLogName(DBColumn column)
        {
            return column.Name + "_log";
        }

        #region Variable
        private QExpression expression;
        protected IDBTable cacheReferenceTable;
        CultureInfo culture;
        DBColumnGroup cacheGroup;
        protected DBColumnKeys keys = DBColumnKeys.None;
        protected int size;
        protected int scale;
        private Type dataType = null;
        protected DBDataType type = DBDataType.String;
        protected DBColumnTypes ctype = DBColumnTypes.Default;
        protected string gname;
        protected string property;
        protected string referenceProperty;
        protected string format;
        protected int order = -1;
        protected string cdefault;
        protected string query;
        protected string subList;
        //private Dictionary<int, object> tags;
        private DBColumn logColumn;
        private DBColumn parentColumn;
        private DBColumn targetColumn;
        private PropertyInfo propertyInfo;
        private PropertyInfo referencePropertyInfo;
        private JsonEncodedText? jsonName;
        private JsonEncodedText? jsonReferenceName;

        #endregion

        public DBColumn()
        {
        }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public JsonEncodedText JsonName { get => jsonName ??= JsonEncodedText.Encode(PropertyName ?? Name, JavaScriptEncoder.UnsafeRelaxedJsonEscaping); }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public JsonEncodedText JsonReferenceName { get => jsonReferenceName ??= JsonEncodedText.Encode(ReferencePropertyName, JavaScriptEncoder.UnsafeRelaxedJsonEscaping); }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public DBSystem DBSystem { get => Schema?.Connection.System ?? DBSystem.Default; }

        [Browsable(false)]
        public string TargetName { get; set; }

        [XmlIgnore, JsonIgnore]
        public DBColumn TargetColumn
        {
            get => targetColumn ??= Table is IDBTableLog logTable
                ? logTable.TargetTable?.Columns[TargetName]
                : null;// Table.ParentTable.Columns[TargetName];
            set
            {
                targetColumn = value;
                TargetName = value?.Name;
            }
        }

        [XmlIgnore, JsonIgnore]
        public DBColumn ParentColumn
        {
            get => parentColumn ??= Table.ParentTable?.Columns[Name];
            set
            {
                parentColumn = value;
            }
        }

        [JsonIgnore, XmlIgnore]
        public DBColumn LogColumn => logColumn ??= Table?.LogTable?.GetLogColumn(this);

        [Browsable(false), XmlIgnore, JsonIgnore]
        public Dictionary<Type, string> DefaultValues { get; set; }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public virtual PropertyInfo PropertyInfo
        {
            get => propertyInfo;
            set
            {
                if (propertyInfo != value)
                {
                    propertyInfo = value;
                    if (propertyInfo != null)
                    {
                        if (PropertyName == null)
                            PropertyName = propertyInfo.Name;
                        if (PropertyInvoker == this && string.Equals(PropertyName, propertyInfo.Name, StringComparison.Ordinal))
                        {
                            PropertyInvoker = EmitInvoker.Initialize(propertyInfo, true);
                        }
                        else
                        { }
                    }
                    OnPropertyChanged();
                }
            }
        }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public abstract IInvoker PropertyInvoker { get; set; }

        [Browsable(false)]
        public virtual string PropertyName
        {
            get => property;
            set
            {
                if (property != value)
                {
                    property = value;
                    OnPropertyChanged();
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public virtual PropertyInfo ReferencePropertyInfo
        {
            get => referencePropertyInfo;
            set
            {
                if (referencePropertyInfo != value)
                {
                    referencePropertyInfo = value;
                    ReferencePropertyName = value?.Name;
                    if (referencePropertyInfo != null && ReferencePropertyInvoker == null)
                    {
                        ReferencePropertyInvoker = EmitInvoker.Initialize(referencePropertyInfo, true);
                    }
                    OnPropertyChanged(nameof(ReferencePropertyInfo));
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public virtual IInvoker ReferencePropertyInvoker { get; set; }

        [Browsable(false)]
        public virtual string ReferencePropertyName
        {
            get => referenceProperty;
            set
            {
                if (referenceProperty != value)
                {
                    referenceProperty = value;
                    OnPropertyChanged(nameof(ReferencePropertyName));
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public bool CanWrite => true;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public abstract IElementSerializer Serializer { get; }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public abstract Pull Pull { get; internal set; }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public abstract IPullIndex PullIndex { get; internal set; }


        [Category("Name")]
        public override string FullName => string.Format("{0}.{1}.{2}", Schema?.Name, Table?.Name, name);


        [XmlText, DefaultValue((string)null)]
        public string SubList
        {
            get => subList;
            set => subList = value;
        }

        [Category("Add")]
        public CultureInfo Culture
        {
            get => culture;
            set
            {
                if (value == culture)
                    return;
                culture = value;
                if (culture != null)
                    keys |= DBColumnKeys.View;
                OnPropertyChanged();
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false), Category("Localizing")]
        public bool IsCulture => Culture != null;

        [Browsable(false), Category("Add")]
        public virtual string GroupName
        {
            get => gname;
            set
            {
                if (string.Equals(gname, value, StringComparison.Ordinal))
                    return;
                gname = value;
                cacheGroup = null;
                OnPropertyChanged();
            }
        }

        [XmlIgnore, JsonIgnore, Category("Add")]
        public DBColumnGroup Group
        {
            get
            {
                if (Table == null)
                    return null;
                if (cacheGroup == null && gname != null && gname.Length != 0)
                    cacheGroup = Table.ColumnGroups[gname];
                return cacheGroup;
            }
            set
            {
                if (value == Group)
                    return;
                GroupName = value?.Name;
                cacheGroup = value;
            }
        }

        [Category("Database"), DefaultValue(DBColumnKeys.None)]
        public virtual DBColumnKeys Keys
        {
            get => keys;
            set
            {
                if (Keys != value)
                {
                    var isNotnull1 = (keys & DBColumnKeys.Notnull) == DBColumnKeys.Notnull;
                    keys = value;
                    var isNotnull2 = (keys & DBColumnKeys.Notnull) == DBColumnKeys.Notnull;

                    OnPropertyChanged(isNotnull1 != isNotnull2 ? DDLType.Alter : DDLType.None);
                }
                CheckPullIndex();
            }
        }

        [Category("Database")]
        public string Format
        {
            get => format;
            set
            {
                format = value;
                OnPropertyChanged(nameof(Format));
            }
        }

        [Browsable(false), Category("Database"), XmlIgnore, JsonIgnore]
        public int Order
        {
            get => order;
            set
            {
                if (order == value)
                    return;
                order = value;
                OnPropertyChanged(nameof(Order));
            }
        }

        [XmlIgnore, JsonIgnore, Category("Database")]
        public string ReferenceTableName => Table?.Foreigns.GetByColumn(this)?.FirstOrDefault()?.ReferenceTableName;

        [XmlIgnore, JsonIgnore, Category("Database")]
        public IDBTable ReferenceTable
        {
            get
            {
                if (IsReference && cacheReferenceTable == null)
                    cacheReferenceTable = Table?.Foreigns.GetByColumn(this)?.FirstOrDefault()?.ReferenceTable ?? Schema.GetTable(ReferencePropertyInvoker?.DataType);
                return cacheReferenceTable;
            }
            set
            {
                IsReference = value != null;
                cacheReferenceTable = value;
                if (value != null && value.PrimaryKey != null)
                {
                    Size = value.PrimaryKey.Size;
                    Scale = value.PrimaryKey.Scale;
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false), Category("Database")]
        public bool IsView => (Keys & DBColumnKeys.View) == DBColumnKeys.View;

        [XmlIgnore, JsonIgnore, Browsable(false), Category("Database")]
        public bool IsReference
        {
            get => (Keys & DBColumnKeys.Reference) == DBColumnKeys.Reference;
            set
            {
                if (value != IsReference)
                {
                    if (value)
                        Keys |= DBColumnKeys.Reference;
                    else
                        Keys &= ~DBColumnKeys.Reference;
                    OnPropertyChanged(nameof(IsReference));
                }
            }
        }

        [Category("Database")]
        public virtual DBDataType DBDataType
        {
            get => type;
            set
            {
                if (type == value)
                    return;
                type = value;
                if (dataType == null)
                {
                    switch (type)
                    {
                        case DBDataType.ByteArray:
                        case DBDataType.Blob: dataType = typeof(byte[]); break;
                        case DBDataType.Bool: dataType = typeof(bool?); break;
                        case DBDataType.String:
                        case DBDataType.Clob: dataType = typeof(string); break;
                        case DBDataType.Date:
                        case DBDataType.DateTime:
                        case DBDataType.TimeStamp: dataType = typeof(DateTime?); break;
                        case DBDataType.TimeSpan: dataType = typeof(TimeSpan?); break;
                        case DBDataType.Double: dataType = typeof(double?); break;
                        case DBDataType.Float: dataType = typeof(float?); break;
                        case DBDataType.TinyInt: dataType = typeof(byte?); break;
                        case DBDataType.Int: dataType = typeof(int?); break;
                        case DBDataType.BigInt: dataType = typeof(long?); break;
                        case DBDataType.Decimal: dataType = typeof(decimal?); break;
                        case DBDataType.ShortInt: dataType = typeof(short?); break;
                        default: dataType = typeof(object); break;
                    }
                }
                CheckPull();
                OnPropertyChanged(DDLType.Alter);
            }
        }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public Type TargetType => typeof(DBItem);

        [Browsable(false), Category("Database"), XmlIgnore, JsonIgnore]
        public virtual Type DataType
        {
            get => dataType;
            set
            {
                if (dataType == value)
                    return;
                dataType = value;
                if (value == null)
                {
                    DBDataType = DBDataType.String;
                    return;
                }
                value = TypeHelper.CheckNullable(value);
                if (value == typeof(byte[]))
                {
                    if (size <= 0 || size > 4000)
                        DBDataType = DBDataType.Blob;
                    else
                        DBDataType = DBDataType.ByteArray;
                }
                else if (value == typeof(byte)
                    || value == typeof(sbyte))
                    DBDataType = DBDataType.TinyInt;
                else if (value == typeof(bool))
                    DBDataType = DBDataType.Bool;
                else if (value == typeof(string))
                {
                    if (size <= 0 || size > 4000)
                        DBDataType = DBDataType.Clob;
                    else
                        DBDataType = DBDataType.String;
                }
                else if (value == typeof(decimal))
                    DBDataType = DBDataType.Decimal;
                else if (value == typeof(double))
                    DBDataType = DBDataType.Double;
                else if (value == typeof(float))
                    DBDataType = DBDataType.Float;
                else if (value == typeof(long)
                    || value == typeof(ulong))
                    DBDataType = DBDataType.BigInt;
                else if (value == typeof(int))
                    DBDataType = DBDataType.Int;
                else if (value == typeof(uint))
                    DBDataType = DBDataType.BigInt;
                else if (value == typeof(short))
                    DBDataType = DBDataType.ShortInt;
                else if (value == typeof(ushort))
                    DBDataType = DBDataType.Int;
                else if (value == typeof(string))
                    DBDataType = DBDataType.String;
                else if (value == typeof(DateTime))
                    DBDataType = DBDataType.DateTime;
                else if (value == typeof(TimeSpan))
                    DBDataType = DBDataType.TimeSpan;
                else if (value.IsEnum)
                {
                    var underlineType = Enum.GetUnderlyingType(value);
                    if (underlineType == typeof(int))
                        DBDataType = DBDataType.Int;
                    else if (underlineType == typeof(uint))
                        DBDataType = DBDataType.BigInt;
                    else if (underlineType == typeof(byte)
                        || underlineType == typeof(sbyte))
                        DBDataType = DBDataType.TinyInt;
                    else if (underlineType == typeof(short))
                        DBDataType = DBDataType.ShortInt;
                    else if (underlineType == typeof(ushort))
                        DBDataType = DBDataType.Int;
                    else if (underlineType == typeof(long)
                        || underlineType == typeof(ulong))
                        DBDataType = DBDataType.BigInt;
                }
                else if (TypeHelper.IsInterface(value, typeof(IBinarySerializable)))
                    DBDataType = DBDataType.ByteSerializable;
                else
                    DBDataType = DBDataType.Object;
            }
        }

        [Category("Database")]
        public string DefaultValue
        {
            get => cdefault;
            set
            {
                if (cdefault == value)
                    return;
                cdefault = value;
                OnPropertyChanged(DDLType.None);
            }
        }

        [Category("Database"), DefaultValue(DBColumnTypes.Default)]
        public virtual DBColumnTypes ColumnType
        {
            get => ctype;
            set
            {
                if (ctype == value)
                    return;
                ctype = value;
                OnPropertyChanged(value == DBColumnTypes.Default ? DDLType.Create : DDLType.None);
            }
        }

        [Category("Database")]
        public string Query
        {
            get => query;
            set
            {
                if (query == value)
                    return;
                query = value;
                OnPropertyChanged(DDLType.Alter);
            }
        }

        [Category("Database"), DefaultValue(0)]
        public virtual int Size
        {
            get => size;
            set
            {
                if (size != value)
                {
                    size = value;
                    if (DataType == typeof(string))
                    {
                        if (size <= 0 || size > 4000)
                            DBDataType = DBDataType.Clob;
                        else
                            DBDataType = DBDataType.String;
                    }
                    if (DataType == typeof(byte[]))
                    {
                        if (size <= 0 || size > 4000)
                            DBDataType = DBDataType.Blob;
                        else
                            DBDataType = DBDataType.ByteArray;
                    }
                    OnPropertyChanged(DDLType.Alter);
                }
            }
        }

        [Category("Database"), DefaultValue(0)]
        public virtual int Scale
        {
            get => scale;
            set
            {
                if (scale != value)
                {
                    scale = value;
                    OnPropertyChanged(DDLType.Alter);
                }
            }
        }

        [Browsable(false)]
        public virtual string SqlName => Name;

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public bool IsPrimaryKey => (Keys & DBColumnKeys.Primary) == DBColumnKeys.Primary;

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public bool IsTypeKey => (Keys & DBColumnKeys.ItemType) == DBColumnKeys.ItemType;

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public bool IsNotNull => (Keys & DBColumnKeys.Notnull) == DBColumnKeys.Notnull;

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public bool IsSystem => (Keys & DBColumnKeys.System) == DBColumnKeys.System;

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public bool IsFile => (Keys & DBColumnKeys.File) == DBColumnKeys.File;

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public bool IsFileName => (Keys & DBColumnKeys.FileName) == DBColumnKeys.FileName;

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public bool IsFileLOB => (Keys & DBColumnKeys.FileOID) == DBColumnKeys.FileOID;

        [JsonIgnore, XmlIgnore]
        public object Default => null;

        [JsonIgnore, XmlIgnore]
        public bool IsAttribute => true;

        [JsonIgnore, XmlIgnore]
        public bool IsChangeSensitive => true;

        [JsonIgnore, XmlIgnore]
        public bool IsReadOnly => false;

        [JsonIgnore, XmlIgnore]
        public bool IsRequired => IsNotNull;

        [JsonIgnore, XmlIgnore]
        public bool IsText => false;

        [JsonIgnore, XmlIgnore]
        public bool IsWriteable => true;

        [JsonIgnore, XmlIgnore]
        public abstract int SizeOfDataType { get; }

        internal protected abstract void CheckPull();

        internal protected abstract void CheckPullIndex();

        public void RefreshLogColumn(DBColumn baseColumn)
        {
            TargetColumn = baseColumn;
            Name = GetLogName(baseColumn);
            DisplayName = baseColumn.DisplayName + " Log";
            DBDataType = baseColumn.DBDataType;
            ReferenceTable = baseColumn.ReferenceTable;
            Size = baseColumn.Size;
            Scale = baseColumn.Scale;
            if (baseColumn.IsFile)
            {
                Keys |= DBColumnKeys.File;
            }
            if (baseColumn.IsFileName)
            {
                Keys |= DBColumnKeys.FileName;
            }
            if (baseColumn.IsFileLOB)
            {
                Keys |= DBColumnKeys.FileOID;
            }
            if (baseColumn.IsTypeKey)
            {
                Keys |= DBColumnKeys.ItemType;
            }
            if ((baseColumn.Keys & DBColumnKeys.Access) == DBColumnKeys.Access)
            {
                Keys |= DBColumnKeys.Access;
            }
        }

        public void RefreshVirtualColumn(DBColumn baseColumn)
        {
            ParentColumn = baseColumn;
            Name = baseColumn.Name;
            GroupName = baseColumn.GroupName;
            Keys = baseColumn.Keys;
            Size = baseColumn.Size;
            Scale = baseColumn.Scale;
            DBDataType = baseColumn.DBDataType;
            ColumnType = baseColumn.ColumnType;
            Culture = baseColumn.Culture;
            PropertyInvoker = baseColumn.PropertyInvoker;
            PropertyInfo = baseColumn.PropertyInfo;
            ReferencePropertyInvoker = baseColumn.ReferencePropertyInvoker;
            ReferencePropertyInfo = baseColumn.ReferencePropertyInfo;
            Pull = baseColumn.Pull;
        }

        public IComparer CreateComparer(ListSortDirection direction = ListSortDirection.Ascending)
        {
            return CreateComparer(typeof(DBItem), direction);
        }

        public IComparer CreateComparer(Type type, ListSortDirection direction = ListSortDirection.Ascending)
        {
            var keyType = DataType;
            var compareType = typeof(DBComparer<,>).MakeGenericType(type, keyType);
            return (IComparer)EmitInvoker.CreateObject(compareType,
                new Type[] { typeof(DBColumn), typeof(ListSortDirection) },
                new object[] { this, direction }, true);
        }

        public IComparer<T> CreateComparer<T>(ListSortDirection direction = ListSortDirection.Ascending) where T : DBItem
        {
            return (IComparer<T>)CreateComparer(typeof(T), direction);
        }

        public abstract void Read(DbDataReader reader, DBItem row, int i);

        //public F ReadAndSelect<F>(DBTransaction transaction, int i) where F : DBItem => ReadAndSelect<F>(transaction.Reader, i);

        public abstract DBItem GetOrCreate(DbDataReader reader, int i, int typeIndex);

        public QExpression GetExpression()
        {
            if (expression == null)
            {
                expression = new QExpression();
                expression.Parse(query, Table);
            }
            return expression;
        }

        public List<string> GetSubList()
        {
            if (subList == null)
                return null;
            List<string> items = new List<string>();
            string[] split = System.Text.RegularExpressions.Regex.Split(subList, "\n|\rn|;");
            foreach (string s in split)
                if (s.Trim().Length != 0)
                    items.Add(s.Trim());
            items.Sort();
            return items;
        }

        public DBForeignKey GetForeign()
        {
            return Table?.Foreigns.GetForeignByColumn(this);
        }

        #region IComparable Members

        public new int CompareTo(object obj)
        {
            return CompareTo((DBColumn)obj);
        }

        #endregion

        #region IComparable<DBColumn> Members

        public int CompareTo(DBColumn other)
        {
            return order.CompareTo(other.order);
        }

        #endregion

        public override object Clone()
        {
            var column = DBColumnFactory.Create(DataType);
            column.subList = subList;
            column.name = name;
            column.size = size;
            column.scale = scale;
            column.culture = culture;
            column.keys = keys;
            column.format = format;
            column.ctype = ctype;
            column.type = type;
            column.cdefault = cdefault;
            column.gname = gname;
            column.query = query;
            //column.bcode = bcode;
            column.order = order;
            return column;
        }

        public override string FormatSql(DDLType ddlType, bool dependency = false)
        {
            var ddl = new StringBuilder();
            Table?.System.Format(ddl, this, ddlType, dependency);
            return ddl.ToString();
        }

        //public virtual object GetTag(int hindex)
        //{
        //    return tags == null ? null : tags.TryGetValue(hindex, out var obj) ? obj : null;
        //}

        //public virtual void RemoveTag(int hindex)
        //{
        //    if (tags != null)
        //    {
        //        tags.Remove(hindex);
        //    }
        //}

        //public virtual void SetTag(int hindex, object value)
        //{
        //    if (value == null)
        //        RemoveTag(hindex);
        //    else
        //    {
        //        if (tags == null)
        //            tags = new Dictionary<int, object>();
        //        tags[hindex] = value;
        //    }
        //}
        public abstract void AddIndex<T>(T item) where T : DBItem;

        public abstract void RemoveIndex<T>(T item) where T : DBItem;

        public abstract bool IsEmpty(DBItem item);

        public abstract bool IsEmpty(PullHandler handler);

        public abstract void Clear(DBItem item, DBSetValueMode mode = DBSetValueMode.Default);

        public abstract void Clear(PullHandler handler);

        public abstract void Copy(PullHandler fromIndex, PullHandler toIndex);

        public abstract void Copy(DBItem fromItem, DBColumn fromColumn, DBItem toItem, DBSetValueMode mode = DBSetValueMode.Default);

        public abstract void Copy(DBItem fromItem, DBItem toItem, DBSetValueMode mode = DBSetValueMode.Default);

        public abstract bool IsChanged(DBItem item);

        public abstract bool GetOld(DBItem item, out object obj);

        public abstract void Reject(DBItem dBItem, DBSetValueMode mode = DBSetValueMode.Default);

        public abstract void Clear();

        public abstract bool Equal(object oldValue, object value);

        public abstract object GetValue(object target);

        public abstract void SetValue(object target, object value);

        public abstract void SetValue(DBItem item, object value, DBSetValueMode mode);

        public DBItem GetReference(DBItem item, DBLoadParam param) => GetReference<DBItem>(item, param);

        public abstract R GetReference<R>(DBItem item, DBLoadParam param) where R : DBItem;

        public DBItem GetReference(DBItem item, ref DBItem reference, DBLoadParam param) => GetReference<DBItem>(item, ref reference, param);

        public abstract R GetReference<R>(DBItem item, ref R reference, DBLoadParam param) where R : DBItem;

        public void SetReference(DBItem item, DBItem value) => SetReference<DBItem>(item, value);

        public abstract void SetReference<R>(DBItem item, R value) where R : DBItem;

        public IEnumerable<DBItem> Load(DBItem item, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null) => Load<DBItem>(item, param, transaction);

        public abstract IEnumerable<R> Load<R>(DBItem item, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null) where R : DBItem;

        public IEnumerable<DBItem> Load(object key, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null) => Load<DBItem>(key, param, transaction);

        public abstract IEnumerable<R> Load<R>(object key, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null) where R : DBItem;

        internal protected abstract PullIndex CreatePullIndex();

        public string FormatCode(object value)
        {
            if (value is DBItem)
                value = ((DBItem)value).PrimaryId;

            if (value == null)
                return "null";
            else if (value is bool)
                return value.ToString().ToLowerInvariant();
            else if (value is string)
                return $"\"{((string)value).Replace("\"", "\\\"")}\"";
            else if (value is DateTime)
            {
                if (((DateTime)value).TimeOfDay == TimeSpan.Zero)
                    return $"DateTime.Parse(\"\"{((DateTime)value).ToString("yyyy-MM-dd")}\")";
                else
                    return $"DateTime.Parse(\"{((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff")}\")";
            }
            else if (value is Enum)
            {
                return $"{value.GetType().Name}.{value}";
            }
            else if (value is byte[])
            {
                var sBuilder = new StringBuilder("new byte[]{");
                var data = (byte[])value;
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append("0x");
                    sBuilder.Append(data[i].ToString("x2"));
                    sBuilder.Append(",");
                }
                sBuilder.Length -= 1;
                sBuilder.Append("}");
                return sBuilder.ToString();
            }
            else
            {
                return value.ToString().Replace(",", ".");
            }
        }

        public abstract void SetId(DBItem item, long id);

        public abstract object GetParameterValue(DBItem item);

        public abstract object GetParameterValue(object value);

        public abstract string FormatDisplay(DBItem item);

        public abstract string FormatDisplay(object val);

        public abstract string FormatQuery(DBItem item);

        public abstract string FormatQuery(object val);

        public abstract object ParseValue(object value);

        public abstract IEnumerable Distinct(IEnumerable<DBItem> enumerable);

        public bool IsSerializeable(Type type)
        {
            return PropertyName != null
                && PropertyInvoker != null && PropertyInvoker != this
                && PropertyInvoker.TargetType.IsAssignableFrom(type)
                && !TypeHelper.IsNonSerialize(PropertyInfo)
                //&& (Attribute.Keys & DBColumnKeys.Access) != DBColumnKeys.Access
                && (Keys & DBColumnKeys.Password) != DBColumnKeys.Password
                && (Keys & DBColumnKeys.File) != DBColumnKeys.File;
        }

        public void RemoveConstraints()
        {
            for (var j = 0; j < Table.Constraints.Count;)
            {
                var constraint = Table.Constraints[j];
                if (constraint.Columns.Contains(this))
                {
                    constraint.Columns.Remove(this);
                    if (constraint.Columns.Count == 0)
                    {
                        Table.Constraints.RemoveInternal(constraint, j);
                    }
                }
                else
                {
                    j++;
                }
            }
        }

        public void RemoveForeignKeys()
        {
            for (var j = 0; j < Table.Foreigns.Count;)
            {
                var reference = Table.Foreigns[j];
                if (reference.Column == this)
                {
                    Table.Foreigns.RemoveInternal(reference, j);
                }
                else
                {
                    j++;
                }
            }
        }

        public void RemoveIndexes()
        {
            for (var j = 0; j < Table.Indexes.Count;)
            {
                var index = Table.Indexes[j];
                if (index.Columns.Contains(this))
                {
                    index.Columns.Remove(this);
                    if (index.Columns.Count == 0)
                    {
                        Table.Indexes.RemoveInternal(index, j);
                    }
                }
                else
                {
                    j++;
                }
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public abstract bool CheckItem(DBItem item, object val2, CompareType comparer);

        public bool CheckItem(DBItem item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return CheckItem(item, typedValue, comparer);
        }

        public bool CheckItem(object item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return CheckItem((DBItem)item, typedValue, comparer);
        }

        public abstract IPullIndexCollection<T> SelectIndex<T>(CompareType comparer, object value) where T : DBItem;

        public abstract R FirstOrDefault<R>(DBItem value, IEnumerable<R> list) where R : DBItem;

        public virtual R FirstOrDefault<R>(object value, IEnumerable<R> list) where R : DBItem
        {
            value = ParseValue(value);
            if (PullIndex is IPullIndex index)
            {
                return (R)index.SelectOneObject(value);
            }
            return Search<R>(CompareType.Equal, value, list).FirstOrDefault();
        }

        public virtual IEnumerable<R> Search<R>(CompareType comparer, DBColumn column, IEnumerable<R> list) where R : DBItem
        {
            foreach (R item in list)
            {
                if (CheckItem(item, column.GetValue(item), comparer))
                    yield return item;
            }
        }

        public virtual IEnumerable<DBTuple> Search<R>(CompareType comparer, QColumn qColumn, QTable qTable, IEnumerable<DBTuple> list) where R : DBItem
        {
            foreach (var item in list)
            {
                if (CheckItem(item.Get(qTable), qColumn.Column.GetValue(item.Get(qColumn.QTable)), comparer))
                    yield return item;
            }
        }

        public virtual IEnumerable<R> Search<R>(CompareType comparer, object value, IEnumerable<R> list = null) where R : DBItem
        {
            foreach (R item in list)
            {
                if (CheckItem(item, value, comparer))
                    yield return item;
            }
        }

        public virtual IEnumerable<DBTuple> Search<R>(CompareType comparer, object value, QTable qTable, IEnumerable<DBTuple> list) where R : DBItem
        {
            foreach (var item in list)
            {
                if (CheckItem(item.Get(qTable), value, comparer))
                    yield return item;
            }
        }

        public abstract IEnumerable<R> Select<R>(CompareType comparer, DBItem item, IEnumerable<R> list = null) where R : DBItem;

        public virtual IEnumerable<R> Select<R>(CompareType comparer, object value, IEnumerable<R> list = null) where R : DBItem
        {
            if (value is IEnumerable<R> enumerabble)
            {
                return enumerabble;
            }

            if (PullIndex == null)
            {
                return Search(comparer, value, list ?? (IEnumerable<R>)Table);
            }

            var indexResult = SelectIndex<R>(comparer, value);

            if (list == null || list == Table)
            {
                return indexResult;
            }
            return list.Where(p => indexResult.Contains(p));
        }

        public virtual IEnumerable<DBTuple> Select<R>(CompareType comparer, object value, QTable qTable, IEnumerable<DBTuple> list) where R : DBItem
        {
            if (value is IEnumerable<R> enumerabble)
            {
                return list.Where(p => enumerabble.Contains((R)p.Get(qTable)));
            }

            if (PullIndex == null)
            {
                return Search<R>(comparer, value, qTable, list);
            }

            var indexResult = SelectIndex<R>(comparer, value);

            return list.Where(p => indexResult.Contains(p.Get(qTable)));
        }

        public DBColumn GetVirtualColumn(IDBTable table)
        {
            return table.GetColumn(name);
        }

        public bool CheckDefault(object value)
        {
            return false;
        }

        public abstract void Write(BinaryInvokerWriter writer, object element);

        public abstract void Write<E>(BinaryInvokerWriter writer, E element);

        public abstract void Read(BinaryInvokerReader reader, object element, TypeSerializeInfo itemInfo);

        public abstract void Read<E>(BinaryInvokerReader reader, E element, TypeSerializeInfo itemInfo);

        public abstract void Write(XmlInvokerWriter writer, object element);

        public abstract void Write<E>(XmlInvokerWriter writer, E element);

        public abstract void Read(XmlInvokerReader reader, object element, TypeSerializeInfo itemInfo);

        public abstract void Read<E>(XmlInvokerReader reader, E element, TypeSerializeInfo itemInfo);

        public abstract void Write(Utf8JsonWriter writer, object element, JsonSerializerOptions option = null);

        public abstract void Write<E>(Utf8JsonWriter writer, E element, JsonSerializerOptions option = null);

        public abstract void Read(ref Utf8JsonReader reader, object element, JsonSerializerOptions option = null);

        public abstract void Read<E>(ref Utf8JsonReader reader, E element, JsonSerializerOptions option = null);


    }
}