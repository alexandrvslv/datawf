﻿/*
 DBColumn.cs
 
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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [Flags()]
    public enum DBColumnKeys
    {
        None = 0,
        Primary = 1 << 0,
        Increment = 1 << 1,
        Stamp = 1 << 2,
        Date = 1 << 3,
        Group = 1 << 4,
        Code = 1 << 5,
        ItemType = 1 << 6,
        Image = 1 << 7,
        System = 1 << 8,
        Unique = 1 << 9,
        State = 1 << 10,
        Notnull = 1 << 11,
        Boolean = 1 << 12,
        Indexing = 1 << 13,
        Access = 1 << 14,
        View = 1 << 15,
        Culture = 1 << 16,
        Encript = 1 << 17,
        Password = 1 << 18,
        Reference = 1 << 19,
        TableReference = 1 << 20,
        NoLog = 1 << 21,
        ElementType = 1 << 22,
        File = 1 << 23,
        FileName = 1 << 24,
        FileLOB = 1 << 25,
        FileLastWrite = 1 << 26,
    }

    public class DBColumn : DBTableItem, IComparable, IComparable<DBColumn>, ICloneable, IInvoker<DBItem, object>
    {
        public static readonly DBColumn EmptyKey = new DBColumn();


        #region Variable
        private QExpression expression;
        protected DBTable cacheReferenceTable;
        CultureInfo cacheCulture;
        DBColumnGroup cacheGroup;
        private Pull pull;
        private PullIndex index;
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
        private ConcurrentDictionary<int, object> olds;
        private DBLogColumn logColumn;
        private PropertyInfo propertyInfo;
        private IInvoker propertyInvoker;
        private PropertyInfo referencePropertyInfo;
        private const int bufferSize = 4048;

        #endregion

        public DBColumn() : this(null)
        {
        }

        public DBColumn(string name) : this(name, typeof(string))
        {
        }

        public DBColumn(string name, Type type) : base(name)
        {
            DataType = type;
        }

        public DBColumn(string name, Type type, int size) : this(name, type)
        {
            Size = size;
        }

        public DBColumn(string name, DBTable reference)
            : this(name, reference.PrimaryKey.DataType, reference.PrimaryKey.Size)
        {
            ReferenceTable = reference;
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
                        if (Property == null)
                            Property = propertyInfo.Name;
                        if (PropertyInvoker == this && string.Equals(Property, propertyInfo.Name, StringComparison.Ordinal))
                        {
                            PropertyInvoker = EmitInvoker.Initialize(propertyInfo, true);
                        }
                        else
                        { }
                    }
                    OnPropertyChanged(nameof(PropertyInfo));
                }
            }
        }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public virtual IInvoker PropertyInvoker
        {
            get => propertyInvoker ?? this;
            set
            {
                if (propertyInvoker != value)
                {
                    propertyInvoker = value;
                    OnPropertyChanged(nameof(PropertyInvoker));
                }
            }
        }

        [Browsable(false)]
        public virtual string Property
        {
            get { return property; }
            set
            {
                if (property != value)
                {
                    property = value;
                    OnPropertyChanged(nameof(Property));
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
                    ReferenceProperty = value?.Name;
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
        public virtual string ReferenceProperty
        {
            get { return referenceProperty; }
            set
            {
                if (referenceProperty != value)
                {
                    referenceProperty = value;
                    OnPropertyChanged(nameof(ReferenceProperty));
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public bool CanWrite { get { return true; } }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public Pull Pull
        {
            get { return pull; }
            internal set
            {
                if (pull != value)
                {
                    pull = value;
                    CheckIndex();
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public PullIndex Index
        {
            get { return index; }
            set { index = value; }
        }

        [Category("Name")]
        public override string FullName
        {
            get { return string.Format("{0}.{1}.{2}", Schema?.Name, Table?.Name, name); }
        }


        [XmlText, DefaultValue((string)null)]
        public string SubList
        {
            get { return subList; }
            set { subList = value; }
        }

        [Browsable(false), Category("Add")]
        public virtual string CultureCode
        {
            get { return culture; }
            set
            {
                if (value == culture)
                    return;
                culture = value;
                if (!string.IsNullOrEmpty(value))
                    keys |= DBColumnKeys.View;
                cacheCulture = null;
                OnPropertyChanged(nameof(CultureCode));
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
        public bool IsCulture
        {
            get { return Culture != null; }
        }

        [Browsable(false), Category("Add")]
        public virtual string GroupName
        {
            get { return gname; }
            set
            {
                if (string.Equals(gname, value, StringComparison.Ordinal))
                    return;
                gname = value;
                cacheGroup = null;
                OnPropertyChanged(nameof(GroupName));
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
            get { return keys; }
            set
            {
                if (Keys != value)
                {
                    var isNotnull1 = (keys & DBColumnKeys.Notnull) == DBColumnKeys.Notnull;
                    keys = value;
                    var isNotnull2 = (keys & DBColumnKeys.Notnull) == DBColumnKeys.Notnull;

                    OnPropertyChanged(nameof(Keys), isNotnull1 != isNotnull2 ? DDLType.Alter : DDLType.Default);
                }
                CheckIndex();
            }
        }

        [Browsable(false)]
        public string BoolTrue
        {
            get { return btrue; }
            set
            {
                if (btrue == value)
                    return;
                btrue = value;
                OnPropertyChanged(nameof(BoolTrue));
            }
        }

        [Browsable(false)]
        public string BoolFalse
        {
            get { return bfalse; }
            set
            {
                if (bfalse == value)
                    return;
                bfalse = value;
                OnPropertyChanged(nameof(BoolFalse));
            }
        }

        [Category("Database")]
        public string Format
        {
            get { return format; }
            set
            {
                format = value;
                OnPropertyChanged(nameof(Format));
            }
        }

        [Browsable(false), Category("Database"), XmlIgnore, JsonIgnore]
        public int Order
        {
            get { return order; }
            set
            {
                if (order == value)
                    return;
                order = value;
                OnPropertyChanged(nameof(Order));
            }
        }

        [XmlIgnore, JsonIgnore, Category("Database")]
        public DBTable ReferenceTable
        {
            get
            {
                if (IsReference && cacheReferenceTable == null)
                    cacheReferenceTable = Table?.Foreigns.GetByColumn(this)?.FirstOrDefault()?.ReferenceTable;
                return cacheReferenceTable;
            }
            set
            {
                IsReference = value != null;
                cacheReferenceTable = value;
                if (value != null && value.PrimaryKey != null)
                {
                    DataType = value.PrimaryKey.DataType;
                    Size = value.PrimaryKey.Size;
                    Scale = value.PrimaryKey.Scale;
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false), Category("Database")]
        public bool IsView
        {
            get { return (Keys & DBColumnKeys.View) == DBColumnKeys.View; }
        }

        [XmlIgnore, JsonIgnore, Browsable(false), Category("Database")]
        public bool IsReference
        {
            get { return (Keys & DBColumnKeys.Reference) == DBColumnKeys.Reference; }
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
            get { return type; }
            set
            {
                if (type == value)
                    return;
                type = value;
                if (dataType == null)
                    switch (type)
                    {
                        case DBDataType.ByteArray:
                        case DBDataType.Blob: dataType = typeof(byte[]); break;
                        case DBDataType.LargeObject: dataType = typeof(uint); break;
                        case DBDataType.Bool: dataType = typeof(bool); break;
                        case DBDataType.Clob: dataType = typeof(string); break;
                        case DBDataType.Date:
                        case DBDataType.DateTime:
                        case DBDataType.TimeStamp: dataType = typeof(DateTime); break;
                        case DBDataType.Double: dataType = typeof(double); break;
                        case DBDataType.Float: dataType = typeof(float); break;
                        case DBDataType.TinyInt: dataType = typeof(byte); break;
                        case DBDataType.Int: dataType = typeof(int); break;
                        case DBDataType.BigInt: dataType = typeof(long); break;
                        case DBDataType.Decimal: dataType = typeof(decimal); break;
                        case DBDataType.ShortInt: dataType = typeof(short); break;
                        case DBDataType.String: dataType = typeof(string); break;
                        case DBDataType.TimeSpan: dataType = typeof(TimeSpan); break;
                        default: dataType = typeof(object); break;
                    }
                CheckPull();
                OnPropertyChanged(nameof(DBDataType), DDLType.Alter);
            }
        }

        [Browsable(false)]
        public Type TargetType { get { return typeof(DBItem); } }

        [Browsable(false), Category("Database")]
        public virtual Type DataType
        {
            get { return dataType; }
            set
            {
                if (dataType == value)
                    return;
                dataType = value;
                if (value == null)
                {
                    DBDataType = DBDataType.String;
                }
                if (value == typeof(byte[]))
                {
                    if (size <= 0 || size > 4000)
                        DBDataType = DBDataType.Blob;
                    else
                        DBDataType = DBDataType.ByteArray;
                }
                else if (value == typeof(byte))
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
                else if (value == typeof(DateTime))
                    DBDataType = DBDataType.DateTime;
                else if (value == typeof(decimal))
                    DBDataType = DBDataType.Decimal;
                else if (value == typeof(double))
                    DBDataType = DBDataType.Double;
                else if (value == typeof(float))
                    DBDataType = DBDataType.Float;
                else if (value == typeof(long))
                    DBDataType = DBDataType.BigInt;
                else if (value == typeof(int))
                    DBDataType = DBDataType.Int;
                else if (value == typeof(uint))
                {
                    if (DBDataType != DBDataType.LargeObject)
                        DBDataType = DBDataType.Int;
                }
                else if (value == typeof(short))
                    DBDataType = DBDataType.ShortInt;
                else if (value == typeof(sbyte))
                    DBDataType = DBDataType.TinyInt;
                else if (value == typeof(string))
                    DBDataType = DBDataType.String;
                else if (value == typeof(TimeSpan))
                    DBDataType = DBDataType.TimeSpan;
                else if (value.IsEnum)
                    DBDataType = DBDataType.Int;
                else
                    DBDataType = DBDataType.Object;
            }
        }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public Type ReaderDataType { get; set; }

        [Category("Database")]
        public string DefaultValue
        {
            get { return cdefault; }
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
            get { return ctype; }
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
            get { return query; }
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
            get { return size; }
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
            get { return scale; }
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
        public virtual string SqlName
        {
            get { return Name; }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public bool IsPrimaryKey
        {
            get { return (Keys & DBColumnKeys.Primary) == DBColumnKeys.Primary; }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public bool IsTypeKey
        {
            get { return (Keys & DBColumnKeys.ItemType) == DBColumnKeys.ItemType; }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public bool IsNotNull
        {
            get { return (Keys & DBColumnKeys.Notnull) == DBColumnKeys.Notnull; }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public bool IsSystem
        {
            get { return (Keys & DBColumnKeys.System) == DBColumnKeys.System; }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public bool IsFile
        {
            get { return (Keys & DBColumnKeys.File) == DBColumnKeys.File; }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public bool IsFileName
        {
            get { return (Keys & DBColumnKeys.FileName) == DBColumnKeys.FileName; }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public bool IsFileLOB
        {
            get { return (Keys & DBColumnKeys.FileLOB) == DBColumnKeys.FileLOB; }
        }


        [JsonIgnore, XmlIgnore]
        public DBLogColumn LogColumn => logColumn ?? (logColumn = Table?.LogTable?.GetLogColumn(this));

        internal protected virtual void CheckPull()
        {
            if (!Containers.Any()
                || ColumnType == DBColumnTypes.Expression
                || ColumnType == DBColumnTypes.Code)
                return;
            if (Pull != null &&
                (Pull.ItemType != DataType))
            {
                Pull.Clear();
                pull = null;
            }
            if (Pull == null && Table != null)
            {
                if (DataType == null)
                    throw new InvalidOperationException($"{nameof(DataType)} not specified!");
                Pull = Pull.Fabric(DataType, Table.BlockSize);
            }
            else if (Pull.BlockSize != Table.BlockSize)
            {
                Pull.BlockSize = Table.BlockSize;
            }
        }

        public IComparer CreateComparer(ListSortDirection direction = ListSortDirection.Ascending)
        {
            return CreateComparer<DBItem>(direction);
        }

        public IComparer CreateComparer<T>(ListSortDirection direction = ListSortDirection.Ascending) where T : DBItem
        {
            var type = typeof(DBComparer<,>).MakeGenericType(typeof(T), GetDataType());
            return (IComparer)EmitInvoker.CreateObject(type,
                new Type[] { typeof(DBColumn), typeof(ListSortDirection) },
                new object[] { this, direction }, true);
        }

        public void LoadFromReader(DBTransaction transaction, DBItem row, int i)
        {
            if (row.Attached && row.UpdateState != DBUpdateState.Default && row.GetOld(this, out _))
            {
                return;
            }
            var isNull = transaction.Reader.IsDBNull(i);
            switch (DBDataType)
            {
                case DBDataType.String:
                case DBDataType.Clob:
                    var stringValue = isNull ? null : transaction.Reader.GetString(i);
                    row.SetValueClass<string>(stringValue, this, DBSetValueMode.Loading);
                    break;
                case DBDataType.Int:
                    var intValue = isNull ? (int?)null : transaction.Reader.GetInt32(i);
                    if (DataType == typeof(int))
                    {
                        row.SetValueNullable<int>(intValue, this, DBSetValueMode.Loading);
                    }
                    else if (DataType == typeof(uint))
                    {
                        row.SetValueNullable<uint>(isNull ? (uint?)null : (uint?)intValue, this, DBSetValueMode.Loading);
                    }
                    else
                    {
                        row.SetValue((object)intValue, this, DBSetValueMode.Loading);
                    }
                    break;
                case DBDataType.BigInt:
                    var longValue = isNull ? (long?)null : transaction.Reader.GetInt64(i);
                    row.SetValueNullable<long>(longValue, this, DBSetValueMode.Loading);
                    break;
                case DBDataType.ShortInt:
                    var shortValue = isNull ? (short?)null : transaction.Reader.GetInt16(i);
                    row.SetValueNullable<short>(shortValue, this, DBSetValueMode.Loading);
                    break;
                case DBDataType.Date:
                case DBDataType.DateTime:
                case DBDataType.TimeStamp:
                    var dateValue = isNull ? (DateTime?)null : (DateTime?)transaction.Reader.GetDateTime(i);
                    if (!isNull && (Keys & (DBColumnKeys.Date | DBColumnKeys.Stamp)) != 0)
                    {
                        dateValue = DateTime.SpecifyKind(dateValue.Value, DateTimeKind.Utc);
                    }
                    row.SetValueNullable<DateTime>(dateValue, this, DBSetValueMode.Loading);
                    break;
                case DBDataType.Bool:
                    var boolValue = isNull ? (bool?)null : transaction.Reader.GetBoolean(i);
                    row.SetValueNullable<bool>(boolValue, this, DBSetValueMode.Loading);
                    break;
                case DBDataType.Blob:
                case DBDataType.ByteArray:
                    var arrayValue = isNull ? null : (byte[])transaction.Reader.GetValue(i);
                    row.SetValueClass<byte[]>(arrayValue, this, DBSetValueMode.Loading);
                    break;
                case DBDataType.LargeObject:
                    var unitValue = isNull ? (uint?)null : transaction.ReadOID(i);
                    row.SetValueNullable<uint>(unitValue, this, DBSetValueMode.Loading);
                    break;
                case DBDataType.Decimal:
                    var decimalValue = isNull ? (decimal?)null : transaction.Reader.GetDecimal(i);
                    row.SetValueNullable<decimal>(decimalValue, this, DBSetValueMode.Loading);
                    break;
                case DBDataType.Double:
                    var doubleValue = isNull ? (double?)null : transaction.Reader.GetDouble(i);
                    row.SetValueNullable<double>(doubleValue, this, DBSetValueMode.Loading);
                    break;
                case DBDataType.Float:
                    var floatValue = isNull ? (float?)null : transaction.Reader.GetFloat(i);
                    row.SetValueNullable<float>(floatValue, this, DBSetValueMode.Loading);
                    break;
                case DBDataType.TimeSpan:
                    var spanValue = isNull ? (TimeSpan?)null : transaction.ReadTimeSpan(i);
                    row.SetValueNullable<TimeSpan>(spanValue, this, DBSetValueMode.Loading);
                    break;
                case DBDataType.TinyInt:
                    var byteValue = isNull ? (byte?)null : transaction.Reader.GetByte(i);
                    if (DataType == typeof(sbyte))
                    {
                        row.SetValueNullable<sbyte>(isNull ? (sbyte?)null : (sbyte?)byteValue, this, DBSetValueMode.Loading);
                    }
                    else
                    {
                        row.SetValueNullable<byte>(byteValue, this, DBSetValueMode.Loading);
                    }
                    break;
                default:
                    var value = isNull ? null : transaction.Reader.GetValue(i);
                    row.SetValue(value, this, DBSetValueMode.Loading);
                    break;
            }
            //var value = transaction.DbConnection.System.ReadValue(this, transaction.Reader.GetValue(i));
            //row.SetValue(value, this, false);
        }

        internal F SelectOneFromReader<F>(DBTransaction transaction, int i) where F : DBItem
        {
            switch (DBDataType)
            {
                case DBDataType.String:
                case DBDataType.Clob:
                    var stringValue = transaction.Reader.GetString(i);
                    return Index.SelectOne<F, string>(stringValue);
                case DBDataType.Int:
                    var intValue = transaction.Reader.GetInt32(i);
                    if (DataType == typeof(int))
                    {
                        return Index.SelectOne<F, int?>(intValue);
                    }
                    else if (DataType == typeof(uint))
                    {
                        return Index.SelectOne<F, uint?>((uint)intValue);
                    }
                    break;
                case DBDataType.BigInt:
                    var longValue = transaction.Reader.GetInt64(i);
                    return Index.SelectOne<F, long?>(longValue);
                case DBDataType.ShortInt:
                    var shortValue = transaction.Reader.GetInt16(i);
                    return Index.SelectOne<F, short?>(shortValue);
                case DBDataType.Date:
                case DBDataType.DateTime:
                case DBDataType.TimeStamp:
                    var dateValue = transaction.Reader.GetDateTime(i);
                    if ((Keys & (DBColumnKeys.Date | DBColumnKeys.Stamp)) != 0)
                    {
                        dateValue = DateTime.SpecifyKind(dateValue, DateTimeKind.Utc);
                    }
                    return Index.SelectOne<F, DateTime?>(dateValue);
                case DBDataType.Bool:
                    var boolValue = transaction.Reader.GetBoolean(i);
                    return Index.SelectOne<F, bool?>(boolValue);
                case DBDataType.Blob:
                case DBDataType.ByteArray:
                    var arrayValue = (byte[])transaction.Reader.GetValue(i);
                    return Index.SelectOne<F, byte[]>(arrayValue);
                case DBDataType.LargeObject:
                    var uintValue = transaction.ReadOID(i);
                    return Index.SelectOne<F, uint?>(uintValue);
                case DBDataType.Decimal:
                    var decimalValue = transaction.Reader.GetDecimal(i);
                    return Index.SelectOne<F, decimal?>(decimalValue);
                case DBDataType.Double:
                    var doubleValue = transaction.Reader.GetDouble(i);
                    return Index.SelectOne<F, double?>(doubleValue);
                case DBDataType.Float:
                    var floatValue = transaction.Reader.GetFloat(i);
                    return Index.SelectOne<F, float?>(floatValue);
                case DBDataType.TimeSpan:
                    var spanValue = transaction.ReadTimeSpan(i);
                    return Index.SelectOne<F, TimeSpan?>(spanValue);
                case DBDataType.TinyInt:
                    var byteValue = transaction.Reader.GetByte(i);
                    if (DataType == typeof(sbyte))
                    {
                        return Index.SelectOne<F, sbyte?>((sbyte)byteValue);
                    }
                    else
                    {
                        return Index.SelectOne<F, byte?>((byte)byteValue);
                    }
                default:
                    var value = transaction.Reader.GetValue(i);
                    return Index.SelectOne<F>(value);
            }
            return default(F);
        }

        protected void CheckIndex()
        {
            if (Index != null && Index.BasePull != pull)
            {
                Index.Dispose();
                Index = null;
            }
            if (pull == null)
            {
                return;
            }
            var build = IsPrimaryKey
                || (Keys & DBColumnKeys.Indexing) == DBColumnKeys.Indexing
                || (Keys & DBColumnKeys.Reference) == DBColumnKeys.Reference;
            if (Index == null && build)
            {
                Index = DBPullIndexFabric.Create(Table, this);
            }
            else if (Index != null && !build)
            {
                Index.Dispose();
                Index = null;
            }
        }

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
            return (CompareTo((DBColumn)obj));
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
            var column = new DBColumn(name)
            {
                subList = subList,
                name = name,
                size = size,
                scale = scale,
                culture = culture,
                keys = keys,
                btrue = btrue,
                bfalse = bfalse,
                format = format,
                ctype = ctype,
                type = type,
                cdefault = cdefault,
                gname = gname,
                query = query,
                //column.bcode = bcode;
                order = order
            };
            return column;
        }

        public override string FormatSql(DDLType ddlType)
        {
            var ddl = new StringBuilder();
            Table?.System.Format(ddl, this, ddlType);
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

        public virtual bool GetOld(int hindex, out object obj)
        {
            obj = null;
            return olds?.TryGetValue(hindex, out obj) ?? false;
        }

        public virtual void RemoveOld(int hindex)
        {
            olds?.TryRemove(hindex, out _);
        }

        public virtual void SetOld(int hindex, object value)
        {
            if (olds == null)
                olds = new ConcurrentDictionary<int, object>();
            olds.TryAdd(hindex, value);
        }

        public void Clear()
        {
            pull?.Clear();
            //tags?.Clear();
            olds?.Clear();
        }

        public object GetValue(DBItem target)
        {
            return Pull != null ? Pull.Get(target.block, target.blockIndex)
                : propertyInvoker?.GetValue(target);
        }

        public T GetValue<T>(DBItem target)
        {
            if (Pull != null)
            {
                return Pull.GetValue<T>(target.block, target.blockIndex);
            }
            if (propertyInvoker is IValuedInvoker<T> valueInvoker)
            {
                return valueInvoker.GetValue(target);
            }
            return (T)propertyInvoker.GetValue(target);
        }

        public T? GetValueNullable<T>(DBItem target) where T : struct
        {
            return ((NullablePullArray<T>)Pull).GetValue(target.block, target.blockIndex);
        }

        public object GetValue(object target)
        {
            return GetValue((DBItem)target);
        }

        public void SetValue(DBItem target, object value)
        {
            if (Pull != null)
            {
                Pull.Set(target.block, target.blockIndex, value);
            }
            else
            {
                propertyInvoker.SetValue(target, value);
            }
        }

        public void SetValue<T>(DBItem target, T value)
        {
            if (Pull != null)
            {
                Pull.SetValue<T>(target.block, target.blockIndex, value);
            }
            else if (propertyInvoker is IValuedInvoker<T> valueInvoker)
            {
                valueInvoker.SetValue(target, value);
            }
            else
            {
                propertyInvoker.SetValue(target, value);
            }
        }

        public void SetValueNullable<T>(DBItem target, T? value) where T : struct
        {
            ((NullablePullArray<T>)Pull).SetValue(target.block, target.blockIndex, value);
        }

        public void SetValue(object target, object value)
        {
            SetValue((DBItem)target, value);
        }

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

        public string FormatValue(object val)
        {
            //if value passed to format is null
            if (val == null)
                return string.Empty;
            if ((Keys & DBColumnKeys.Boolean) == DBColumnKeys.Boolean)
            {
                if (val.ToString().Equals(BoolTrue))
                    return "Check";
                else
                    return "Uncheck";
            }
            if (IsReference)
            {
                DBItem temp = ReferenceTable.LoadItemById(val);
                return temp == null ? "<new or empty>" : temp.ToString();
            }

            if (DataType == typeof(string))
                return val.ToString();

            if (DataType == typeof(byte[]))
            {
                if ((Keys & DBColumnKeys.Access) == DBColumnKeys.Access)
                {
                    var cash = new AccessValue((byte[])val);
                    string rez = string.Empty;
                    foreach (var item in cash)
                    {
                        rez += string.Format("{0}{1}", rez.Length > 0 ? "; " : string.Empty, item);
                    }
                    return rez;
                }
                else
                    return Helper.LenghtFormat(((byte[])val).LongLength);
            }
            if (Format != null)
            {
                var mi = val.GetType().GetMethod("ToString", new Type[] { typeof(string) });
                if (Format.ToLower() == "p")
                    if (val is decimal)
                        return ((decimal)val * 100).ToString("N") + "%";
                    else if (val is double)
                        return ((double)val * 100).ToString("N") + "%";
                    else if (val is float)
                        return ((float)val * 100).ToString("N") + "%";
                    else
                        return (decimal.Parse(val.ToString()) * 100).ToString("N") + "%";
                if (Format.ToLower() == "b" && DataType == typeof(string) && Size == 1)
                    if (val.ToString() == "RowSetting")
                        return "V";
                    else
                        return "X";
                else if (mi != null)
                    return EmitInvoker.Invoke(mi, val, Format) as string;
            }

            if (val is DateTime)
            {
                return val.Equals(((DateTime)val).Date) ? ((DateTime)val).ToString("yyyy.MM.dd") : val.ToString();
            }
            return val.ToString();
        }

        public object ParseValue(object value)
        {
            object buf = null;
            if (value is bool && (Keys & DBColumnKeys.Boolean) == DBColumnKeys.Boolean && DataType != typeof(bool))
                value = (bool)value ? BoolTrue : BoolFalse;

            if (value == null || value == DBNull.Value)
                buf = null;
            else if (value is DBItem item)
                buf = item.PrimaryId;
            else
                buf = Helper.Parse(value, DataType);

            if (buf is DateTime && buf.Equals(DateTime.MinValue))
                buf = null;

            return buf;
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

        public bool CheckItem(DBItem item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return ListHelper.CheckItem(GetValue(item), typedValue, comparer, comparision);
        }

        public bool CheckItem(object item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return CheckItem((DBItem)item, typedValue, comparer, comparision);
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.GroupName))]
        public class GroupNameInvoker<T> : Invoker<T, string> where T : DBColumn
        {
            public static readonly GroupNameInvoker<T> Instance = new GroupNameInvoker<T>();

            public override string Name => nameof(DBColumn.GroupName);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.GroupName;

            public override void SetValue(T target, string value) => target.GroupName = value;
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.Property))]
        public class PropertyNameInvoker<T> : Invoker<T, string> where T : DBColumn
        {
            public static readonly PropertyNameInvoker<T> Instance = new PropertyNameInvoker<T>();

            public override string Name => nameof(DBColumn.Property);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.Property;

            public override void SetValue(T target, string value) => target.Property = value;
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.ReferenceProperty))]
        public class ReferencePropertyNameInvoker<T> : Invoker<T, string> where T : DBColumn
        {
            public static readonly ReferencePropertyNameInvoker<T> Instance = new ReferencePropertyNameInvoker<T>();

            public override string Name => nameof(DBColumn.ReferenceProperty);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.ReferenceProperty;

            public override void SetValue(T target, string value) => target.ReferenceProperty = value;
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.ReferenceTable))]
        public class ReferenceTableInvoker<T> : Invoker<T, string> where T : DBColumn
        {
            public static readonly ReferenceTableInvoker<T> Instance = new ReferenceTableInvoker<T>();

            public override string Name => nameof(DBColumn.ReferenceTable);

            public override bool CanWrite => false;

            public override string GetValue(T target) => target.ReferenceTable?.Name;

            public override void SetValue(T target, string value) { }
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.IsView))]
        public class IsViewInvoker<T> : Invoker<T, bool> where T : DBColumn
        {
            public static readonly IsViewInvoker<T> Instance = new IsViewInvoker<T>();

            public override string Name => nameof(DBColumn.IsView);

            public override bool CanWrite => false;

            public override bool GetValue(T target) => target.IsView;

            public override void SetValue(T target, bool value) { }
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.IsReference))]
        public class IsReferenceInvoker<T> : Invoker<T, bool> where T : DBColumn
        {
            public static readonly IsReferenceInvoker<T> Instance = new IsReferenceInvoker<T>();

            public override string Name => nameof(DBColumn.IsReference);

            public override bool CanWrite => false;

            public override bool GetValue(T target) => target.IsReference;

            public override void SetValue(T target, bool value) { }
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.Keys))]
        public class KeysInvoker<T> : Invoker<T, DBColumnKeys> where T : DBColumn
        {
            public static readonly KeysInvoker<T> Instance = new KeysInvoker<T>();
            public override string Name => nameof(DBColumn.Keys);

            public override bool CanWrite => true;

            public override DBColumnKeys GetValue(T target) => target.Keys;

            public override void SetValue(T target, DBColumnKeys value) => target.Keys = value;
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.Order))]
        public class OrderInvoker<T> : Invoker<T, int> where T : DBColumn
        {
            public static readonly OrderInvoker<T> Instance = new OrderInvoker<T>();
            public override string Name => nameof(DBColumn.Order);

            public override bool CanWrite => true;

            public override int GetValue(T target) => target.Order;

            public override void SetValue(T target, int value) => target.Order = value;
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.Size))]
        public class SizeInvoker<T> : Invoker<T, int> where T : DBColumn
        {
            public override string Name => nameof(DBColumn.Size);

            public override bool CanWrite => true;

            public override int GetValue(T target) => target.Size;

            public override void SetValue(T target, int value) => target.Size = value;
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.Scale))]
        public class ScaleInvoker<T> : Invoker<T, int> where T : DBColumn
        {
            public override string Name => nameof(DBColumn.Scale);

            public override bool CanWrite => true;

            public override int GetValue(T target) => target.Scale;

            public override void SetValue(T target, int value) => target.Scale = value;
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.CultureCode))]
        public class CultureCodeInvoker<T> : Invoker<T, string> where T : DBColumn
        {
            public override string Name => nameof(DBColumn.CultureCode);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.CultureCode;

            public override void SetValue(T target, string value) => target.CultureCode = value;
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.Format))]
        public class FormatInvoker<T> : Invoker<T, string> where T : DBColumn
        {
            public override string Name => nameof(DBColumn.Format);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.Format;

            public override void SetValue(T target, string value) => target.Format = value;
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.DBDataType))]
        public class DBDataTypeInvoker<T> : Invoker<T, DBDataType> where T : DBColumn
        {
            public override string Name => nameof(DBColumn.DBDataType);

            public override bool CanWrite => true;

            public override DBDataType GetValue(T target) => target.DBDataType;

            public override void SetValue(T target, DBDataType value) => target.DBDataType = value;
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.DataType))]
        public class DataTypeInvoker<T> : Invoker<T, Type> where T : DBColumn
        {
            public override string Name => nameof(DBColumn.DataType);

            public override bool CanWrite => true;

            public override Type GetValue(T target) => target.DataType;

            public override void SetValue(T target, Type value) => target.DataType = value;
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.ColumnType))]
        public class ColumnTypeInvoker<T> : Invoker<T, DBColumnTypes> where T : DBColumn
        {
            public override string Name => nameof(DBColumn.ColumnType);

            public override bool CanWrite => true;

            public override DBColumnTypes GetValue(T target) => target.ColumnType;

            public override void SetValue(T target, DBColumnTypes value) => target.ColumnType = value;
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.DefaultValue))]
        public class DefaultValueInvoker<T> : Invoker<T, string> where T : DBColumn
        {
            public override string Name => nameof(DBColumn.DefaultValue);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.DefaultValue;

            public override void SetValue(T target, string value) => target.DefaultValue = value;
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.Query))]
        public class QueryInvoker<T> : Invoker<T, string> where T : DBColumn
        {
            public override string Name => nameof(DBColumn.Query);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.Query;

            public override void SetValue(T target, string value) => target.Query = value;
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.SubList))]
        public class SubListInvoker<T> : Invoker<T, string> where T : DBColumn
        {
            public override string Name => nameof(DBColumn.SubList);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.SubList;

            public override void SetValue(T target, string value) => target.SubList = value;
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.BoolTrue))]
        public class BoolTrueInvoker<T> : Invoker<T, string> where T : DBColumn
        {
            public override string Name => nameof(DBColumn.BoolTrue);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.BoolTrue;

            public override void SetValue(T target, string value) => target.BoolTrue = value;
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.BoolFalse))]
        public class BoolFalseInvoker<T> : Invoker<T, string> where T : DBColumn
        {
            public override string Name => nameof(DBColumn.BoolFalse);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.BoolFalse;

            public override void SetValue(T target, string value) => target.BoolFalse = value;
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.SqlName))]
        public class SqlNameInvoker<T> : Invoker<T, string> where T : DBColumn
        {
            public override string Name => nameof(DBColumn.SqlName);

            public override bool CanWrite => false;

            public override string GetValue(T target) => target.SqlName;

            public override void SetValue(T target, string value) { }
        }

        [Invoker(typeof(DBColumn), nameof(DBColumn.TargetType))]
        public class TargetTypeInvoker<T> : Invoker<T, Type> where T : DBColumn
        {
            public override string Name => nameof(DBColumn.TargetType);

            public override bool CanWrite => false;

            public override Type GetValue(T target) => target.TargetType;

            public override void SetValue(T target, Type value) { }
        }
    }
}