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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.GroupName), typeof(DBColumn.GroupNameInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.PropertyName), typeof(DBColumn.PropertyNameInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.ReferencePropertyName), typeof(DBColumn.ReferencePropertyNameInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.ReferenceTable), typeof(DBColumn.ReferenceTableInvoker))]
[assembly: Invoker(typeof(DBColumn), "ReferenceTableName", typeof(DBColumn.ReferenceTableNameInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.IsView), typeof(DBColumn.IsViewInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.IsReference), typeof(DBColumn.IsReferenceInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.Keys), typeof(DBColumn.KeysInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.Order), typeof(DBColumn.OrderInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.Size), typeof(DBColumn.SizeInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.Scale), typeof(DBColumn.ScaleInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.CultureCode), typeof(DBColumn.CultureCodeInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.Format), typeof(DBColumn.FormatInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.DBDataType), typeof(DBColumn.DBDataTypeInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.DataType), typeof(DBColumn.DataTypeInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.ColumnType), typeof(DBColumn.ColumnTypeInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.DefaultValue), typeof(DBColumn.DefaultValueInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.Query), typeof(DBColumn.QueryInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.SubList), typeof(DBColumn.SubListInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.SqlName), typeof(DBColumn.SqlNameInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.TargetType), typeof(DBColumn.TargetTypeInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.BaseName), typeof(DBColumn.BaseNameInvoker))]
[assembly: Invoker(typeof(DBColumn), nameof(DBColumn.BaseColumn), typeof(DBColumn.BaseColumnInvoker))]
namespace DataWF.Data
{
    public abstract class DBColumn : DBTableItem, IComparable, IComparable<DBColumn>, ICloneable, IInvoker, IPropertySerializeInfo
    {
        public static readonly DBColumn EmptyKey = new DBColumn<object>();
        public static string GetLogName(DBColumn column)
        {
            return column.Name + "_log";
        }

        #region Variable
        private QExpression expression;
        protected DBTable cacheReferenceTable;
        CultureInfo cacheCulture;
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
        protected string culture;
        protected string btrue;
        protected string bfalse;
        protected string format;
        protected int order = -1;
        protected string cdefault;
        protected string query;
        protected string subList;
        //private Dictionary<int, object> tags;
        private DBColumn logColumn;
        private DBColumn baseColumn = DBColumn.EmptyKey;
        private PropertyInfo propertyInfo;
        private PropertyInfo referencePropertyInfo;
        private JsonEncodedText? jsonName;
        private JsonEncodedText? jsonReferenceName;
        private const int bufferSize = 4048;

        #endregion

        public DBColumn()
        {
        }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public JsonEncodedText JsonName { get => jsonName ?? (jsonName = JsonEncodedText.Encode(PropertyName ?? Name, JavaScriptEncoder.UnsafeRelaxedJsonEscaping)).Value; }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public JsonEncodedText JsonReferenceName { get => jsonReferenceName ?? (jsonReferenceName = JsonEncodedText.Encode(ReferencePropertyName, JavaScriptEncoder.UnsafeRelaxedJsonEscaping)).Value; }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public DBSystem DBSystem { get => Schema?.System ?? DBSystem.Default; }

        [Browsable(false)]
        public string BaseName { get; set; }

        [XmlIgnore, JsonIgnore]
        public DBColumn BaseColumn
        {
            get { return baseColumn == DBColumn.EmptyKey ? (baseColumn = (Table as IDBDependTable)?.BaseTable?.Columns[BaseName]) : baseColumn; }
            set
            {
                baseColumn = value;
                BaseName = value?.Name;
            }
        }

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

        [Browsable(false), Category("Add")]
        public virtual string CultureCode
        {
            get => culture;
            set
            {
                if (value == culture)
                    return;
                culture = value;
                if (!string.IsNullOrEmpty(value))
                    keys |= DBColumnKeys.View;
                cacheCulture = null;
                OnPropertyChanged();
            }
        }

        [XmlIgnore, JsonIgnore, Category("Add")]
        public CultureInfo Culture
        {
            get
            {
                if (cacheCulture == null && !string.IsNullOrEmpty(CultureCode))
                    cacheCulture = CultureInfo.GetCultureInfo(CultureCode);
                return cacheCulture;
            }
            set
            {
                if (value == cacheCulture)
                    return;
                CultureCode = value?.Name;
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

                    OnPropertyChanged(nameof(Keys), isNotnull1 != isNotnull2 ? DDLType.Alter : DDLType.Default);
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
        public DBTable ReferenceTable
        {
            get
            {
                if (IsReference && cacheReferenceTable == null)
                    cacheReferenceTable = Table?.Foreigns.GetByColumn(this)?.FirstOrDefault()?.ReferenceTable ?? DBTable.GetTable(ReferencePropertyInvoker?.DataType);
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
                OnPropertyChanged(nameof(DBDataType), DDLType.Alter);
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
                OnPropertyChanged(nameof(DefaultValue), DDLType.Default);
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
                OnPropertyChanged(nameof(ColumnType), value == DBColumnTypes.Default ? DDLType.Create : DDLType.Default);
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
                OnPropertyChanged(nameof(Query), DDLType.Alter);
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
                    OnPropertyChanged(nameof(Size), DDLType.Alter);
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
                    OnPropertyChanged(nameof(Scale), DDLType.Alter);
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
        public bool IsFileLOB => (Keys & DBColumnKeys.FileLOB) == DBColumnKeys.FileLOB;

        [JsonIgnore, XmlIgnore]
        public DBColumn LogColumn => logColumn ?? (logColumn = Table?.LogTable?.GetLogColumn(this));

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

        internal protected abstract void CheckPull();

        internal protected abstract void CheckPullIndex();

        public void RefreshLogColumn(DBColumn baseColumn)
        {
            BaseColumn = baseColumn;
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
                Keys |= DBColumnKeys.FileLOB;
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
            BaseColumn = baseColumn;
            Name = baseColumn.Name;
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
            var keyType = GetDataType();
            if (IsReference)
            {
                keyType = typeof(string);
            }
            var compareType = typeof(DBComparer<,>).MakeGenericType(type, keyType);
            return (IComparer)EmitInvoker.CreateObject(compareType,
                new Type[] { typeof(DBColumn), typeof(ListSortDirection) },
                new object[] { this, direction }, true);
        }

        public IComparer<T> CreateComparer<T>(ListSortDirection direction = ListSortDirection.Ascending) where T : DBItem
        {
            return (IComparer<T>)CreateComparer(typeof(T), direction);
        }

        public abstract void Read(DBTransaction transaction, DBItem row, int i);

        public abstract F ReadAndSelect<F>(DBTransaction transaction, int i) where F : DBItem;

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

        public Type GetDataType()
        {
            if (DataType.IsValueType)
                return typeof(Nullable<>).MakeGenericType(DataType);
            return DataType;
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
            column.btrue = btrue;
            column.bfalse = bfalse;
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

        public abstract R GetReference<R>(DBItem item, DBLoadParam param) where R : DBItem;

        public abstract R GetReference<R>(DBItem item, ref R reference, DBLoadParam param) where R : DBItem;

        public abstract void SetReference<R>(DBItem item, R value) where R : DBItem;

        public abstract DBItem LoadByKey(DBItem item, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null);

        public abstract DBItem LoadByKey(object key, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null);

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

        public abstract object GetParameterValue(DBItem item);

        public abstract object GetParameterValue(object value);

        public abstract string FormatDisplay(DBItem item);

        public abstract string FormatDisplay(object val);

        public abstract string FormatQuery(DBItem item);

        public abstract string FormatQuery(object val);

        public abstract object ParseValue(object value);

        public abstract bool CheckItem(DBItem item, object val2, CompareType comparer);

        public abstract IEnumerable<T> SelectIndex<T>(object value, CompareType comparer) where T : DBItem;

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

        public IListIndex CreateIndex(bool concurrent)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public IQueryParameter CreateParameter()
        {
            throw new NotImplementedException();
        }

        public InvokerComparer CreateComparer()
        {
            throw new NotImplementedException();
        }

        public abstract bool CheckItem(DBItem item, object typedValue, CompareType comparer, IComparer comparision);

        public bool CheckItem(object item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return CheckItem((DBItem)item, typedValue, comparer, comparision);
        }

        public virtual IEnumerable<T> Search<T>(CompareType comparer, DBColumn column, IEnumerable<T> list) where T : DBItem
        {
            foreach (T item in list)
            {
                if (CheckItem(item, column.GetValue(item), comparer))
                    yield return item;
            }
        }

        public virtual IEnumerable<T> Search<T>(CompareType comparer, object value, IEnumerable<T> list) where T : DBItem
        {
            foreach (T item in list)
            {
                if (CheckItem(item, value, comparer))
                    yield return item;
            }
        }

        public DBColumn GetVirtualColumn(DBTable table)
        {
            return table.ParseColumn(name);
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

        public class GroupNameInvoker : Invoker<DBColumn, string>
        {
            public static readonly GroupNameInvoker Instance = new GroupNameInvoker();

            public override string Name => nameof(GroupName);

            public override bool CanWrite => true;

            public override string GetValue(DBColumn target) => target.GroupName;

            public override void SetValue(DBColumn target, string value) => target.GroupName = value;
        }

        public class PropertyNameInvoker : Invoker<DBColumn, string>
        {
            public static readonly PropertyNameInvoker Instance = new PropertyNameInvoker();

            public override string Name => nameof(PropertyName);

            public override bool CanWrite => true;

            public override string GetValue(DBColumn target) => target.PropertyName;

            public override void SetValue(DBColumn target, string value) => target.PropertyName = value;
        }

        public class ReferencePropertyNameInvoker : Invoker<DBColumn, string>
        {
            public static readonly ReferencePropertyNameInvoker Instance = new ReferencePropertyNameInvoker();

            public override string Name => nameof(ReferencePropertyName);

            public override bool CanWrite => true;

            public override string GetValue(DBColumn target) => target.ReferencePropertyName;

            public override void SetValue(DBColumn target, string value) => target.ReferencePropertyName = value;
        }

        public class ReferenceTableInvoker : Invoker<DBColumn, DBTable>
        {
            public static readonly ReferenceTableInvoker Instance = new ReferenceTableInvoker();

            public override string Name => nameof(ReferenceTable);

            public override bool CanWrite => false;

            public override DBTable GetValue(DBColumn target) => target.ReferenceTable;

            public override void SetValue(DBColumn target, DBTable value) => target.ReferenceTable = value;
        }

        public class ReferenceTableNameInvoker : Invoker<DBColumn, string>
        {
            public static readonly ReferenceTableNameInvoker Instance = new ReferenceTableNameInvoker();

            public override string Name => "ReferenceTableName";

            public override bool CanWrite => false;

            public override string GetValue(DBColumn target) => target.ReferenceTable?.Name;

            public override void SetValue(DBColumn target, string value) { }
        }

        public class IsViewInvoker : Invoker<DBColumn, bool>
        {
            public static readonly IsViewInvoker Instance = new IsViewInvoker();

            public override string Name => nameof(IsView);

            public override bool CanWrite => false;

            public override bool GetValue(DBColumn target) => target.IsView;

            public override void SetValue(DBColumn target, bool value) { }
        }

        public class IsReferenceInvoker : Invoker<DBColumn, bool>
        {
            public static readonly IsReferenceInvoker Instance = new IsReferenceInvoker();

            public override string Name => nameof(IsReference);

            public override bool CanWrite => false;

            public override bool GetValue(DBColumn target) => target.IsReference;

            public override void SetValue(DBColumn target, bool value) { }
        }

        public class KeysInvoker : Invoker<DBColumn, DBColumnKeys>
        {
            public static readonly KeysInvoker Instance = new KeysInvoker();
            public override string Name => nameof(Keys);

            public override bool CanWrite => true;

            public override DBColumnKeys GetValue(DBColumn target) => target.Keys;

            public override void SetValue(DBColumn target, DBColumnKeys value) => target.Keys = value;
        }

        public class OrderInvoker : Invoker<DBColumn, int>
        {
            public static readonly OrderInvoker Instance = new OrderInvoker();
            public override string Name => nameof(Order);

            public override bool CanWrite => true;

            public override int GetValue(DBColumn target) => target.Order;

            public override void SetValue(DBColumn target, int value) => target.Order = value;
        }

        public class SizeInvoker : Invoker<DBColumn, int>
        {
            public override string Name => nameof(Size);

            public override bool CanWrite => true;

            public override int GetValue(DBColumn target) => target.Size;

            public override void SetValue(DBColumn target, int value) => target.Size = value;
        }

        public class ScaleInvoker : Invoker<DBColumn, int>
        {
            public override string Name => nameof(Scale);

            public override bool CanWrite => true;

            public override int GetValue(DBColumn target) => target.Scale;

            public override void SetValue(DBColumn target, int value) => target.Scale = value;
        }

        public class CultureCodeInvoker : Invoker<DBColumn, string>
        {
            public override string Name => nameof(CultureCode);

            public override bool CanWrite => true;

            public override string GetValue(DBColumn target) => target.CultureCode;

            public override void SetValue(DBColumn target, string value) => target.CultureCode = value;
        }

        public class FormatInvoker : Invoker<DBColumn, string>
        {
            public override string Name => nameof(Format);

            public override bool CanWrite => true;

            public override string GetValue(DBColumn target) => target.Format;

            public override void SetValue(DBColumn target, string value) => target.Format = value;
        }

        public class DBDataTypeInvoker : Invoker<DBColumn, DBDataType>
        {
            public override string Name => nameof(DBDataType);

            public override bool CanWrite => true;

            public override DBDataType GetValue(DBColumn target) => target.DBDataType;

            public override void SetValue(DBColumn target, DBDataType value) => target.DBDataType = value;
        }

        public class DataTypeInvoker : Invoker<DBColumn, Type>
        {
            public override string Name => nameof(DBColumn.DataType);

            public override bool CanWrite => true;

            public override Type GetValue(DBColumn target) => target.DataType;

            public override void SetValue(DBColumn target, Type value) => target.DataType = value;
        }

        public class ColumnTypeInvoker : Invoker<DBColumn, DBColumnTypes>
        {
            public override string Name => nameof(ColumnType);

            public override bool CanWrite => true;

            public override DBColumnTypes GetValue(DBColumn target) => target.ColumnType;

            public override void SetValue(DBColumn target, DBColumnTypes value) => target.ColumnType = value;
        }

        public class DefaultValueInvoker : Invoker<DBColumn, string>
        {
            public override string Name => nameof(DefaultValue);

            public override bool CanWrite => true;

            public override string GetValue(DBColumn target) => target.DefaultValue;

            public override void SetValue(DBColumn target, string value) => target.DefaultValue = value;
        }

        public class QueryInvoker : Invoker<DBColumn, string>
        {
            public override string Name => nameof(Query);

            public override bool CanWrite => true;

            public override string GetValue(DBColumn target) => target.Query;

            public override void SetValue(DBColumn target, string value) => target.Query = value;
        }

        public class SubListInvoker : Invoker<DBColumn, string>
        {
            public override string Name => nameof(SubList);

            public override bool CanWrite => true;

            public override string GetValue(DBColumn target) => target.SubList;

            public override void SetValue(DBColumn target, string value) => target.SubList = value;
        }

        public class SqlNameInvoker : Invoker<DBColumn, string>
        {
            public override string Name => nameof(SqlName);

            public override bool CanWrite => false;

            public override string GetValue(DBColumn target) => target.SqlName;

            public override void SetValue(DBColumn target, string value) { }
        }

        public class TargetTypeInvoker : Invoker<DBColumn, Type>
        {
            public override string Name => nameof(DBColumn.TargetType);

            public override bool CanWrite => false;

            public override Type GetValue(DBColumn target) => target.TargetType;

            public override void SetValue(DBColumn target, Type value) { }
        }

        public class BaseNameInvoker : Invoker<DBColumn, string>
        {
            public static readonly BaseNameInvoker Instance = new BaseNameInvoker();
            public override string Name => nameof(DBColumn.BaseName);

            public override bool CanWrite => true;

            public override string GetValue(DBColumn target) => target.BaseName;

            public override void SetValue(DBColumn target, string value) => target.BaseName = value;
        }

        public class BaseColumnInvoker : Invoker<DBColumn, DBColumn>
        {
            public override string Name => nameof(DBColumn.BaseColumn);

            public override bool CanWrite => true;

            public override DBColumn GetValue(DBColumn target) => target.BaseColumn;

            public override void SetValue(DBColumn target, DBColumn value) => target.BaseColumn = value;
        }
    }
}