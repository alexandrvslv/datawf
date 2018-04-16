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
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Xml.Serialization;
using System.Data;
using System.Linq;

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
        ElementType = 1 << 22
    }

    public class DBColumn : DBTableItem, IComparable, IComparable<DBColumn>, ICloneable, IInvoker<DBItem, object>
    {
        public static DBColumn EmptyKey = new DBColumn();

        #region Variable
        private QExpression expression;
        protected DBTable cacheReferenceTable;
        protected DBColumnList list;
        CultureInfo cacheCulture;
        DBColumnGroup cacheGroup;
        private Pull pull;
        private DBPullIndex index;
        protected DBColumnKeys keys = DBColumnKeys.None;
        protected int size;
        protected int scale;
        private Type dataType = null;
        protected DBDataType type = DBDataType.String;
        protected DBColumnTypes ctype = DBColumnTypes.Default;
        protected string gname;
        protected string property;
        protected string culture;
        protected string btrue;
        protected string bfalse;
        protected string format;
        protected int order = -1;
        protected string cdefault;
        protected string query;
        protected string subList;
        private Dictionary<int, object> tags;
        private Dictionary<int, object> olds;

        #endregion

        public DBColumn()
        {
        }

        public DBColumn(string name)
            : this(name, typeof(string))
        {
        }

        public DBColumn(string name, Type type)
            : base(name)
        {
            DataType = type;
        }

        public DBColumn(string name, Type type, int size)
            : this(name, type)
        {
            Size = size;
        }

        public DBColumn(string name, DBTable reference)
            : this(name, reference.PrimaryKey.DataType, reference.PrimaryKey.Size)
        {
            ReferenceTable = reference;
        }

        public override string ToString()
        {
            return base.ToString();
        }

        [ReadOnly(true)]
        public string Property
        {
            get { return property; }
            set
            {
                if (property != value)
                {
                    property = value;
                    OnPropertyChanged(nameof(Property), false);
                }
            }
        }

        public bool CanWrite { get { return true; } }

        [XmlIgnore, Browsable(false)]
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

        internal protected virtual void CheckPull()
        {
            if (Pull != null && Pull.ItemType != DataType)
            {
                Pull.Clear();
                pull = null;
            }
            if (Pull == null && Table != null)
            {
                Pull = Pull.Fabric(DataType, Table.BlockSize);
            }
        }

        protected void CheckIndex()
        {
            if (Index != null && Index.Pull != pull)
            {
                Index.Dispose();
                index = null;
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
                Index = DBPullIndex.Fabric(Table, this);
            }
            else if (Index != null && !build)
            {
                Index.Dispose();
                Index = null;
            }
        }

        [XmlIgnore, Browsable(false)]
        public DBPullIndex Index
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

        [Category("Add")]
        public string CultureCode
        {
            get { return culture; }
            set
            {
                if (value == culture)
                    return;
                culture = value;
                if (value != null && value.Length != 0)
                    keys |= DBColumnKeys.View;
                cacheCulture = null;
                OnPropertyChanged(nameof(CultureCode), false);
            }
        }

        [XmlIgnore, Category("Add")]
        public CultureInfo Culture
        {
            get
            {
                if (cacheCulture == null && culture != null && culture.Length != 0)
                    cacheCulture = CultureInfo.GetCultureInfo(culture);
                return cacheCulture;
            }
            set
            {
                if (value == cacheCulture)
                    return;
                CultureCode = value?.Name;
            }
        }

        [Browsable(false), Category("Localizing"), ReadOnly(true)]
        public bool IsCulture
        {
            get { return Culture != null; }
        }

        [Browsable(false), Category("Add")]
        public string GroupName
        {
            get { return gname; }
            set
            {
                if (gname == value)
                    return;
                gname = value;
                cacheGroup = null;
                OnPropertyChanged(nameof(GroupName), false);
            }
        }

        [XmlIgnore, Category("Add")]
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
                GroupName = value == null ? null : value.Name;
                cacheGroup = value;
            }
        }

        [Category("Database"), DefaultValue(DBColumnKeys.None)]
        public DBColumnKeys Keys
        {
            get { return keys; }
            set
            {
                var isNotnull1 = (keys & DBColumnKeys.Notnull) == DBColumnKeys.Notnull;
                keys = value;
                var isNotnull2 = (keys & DBColumnKeys.Notnull) == DBColumnKeys.Notnull;
                CheckIndex();
                OnPropertyChanged(nameof(Keys), isNotnull1 != isNotnull2);
            }
        }

        public string BoolTrue
        {
            get { return btrue; }
            set
            {
                if (btrue == value)
                    return;
                btrue = value;
                OnPropertyChanged(nameof(BoolTrue), false);
            }
        }

        public string BoolFalse
        {
            get { return bfalse; }
            set
            {
                if (bfalse == value)
                    return;
                bfalse = value;
                OnPropertyChanged(nameof(BoolFalse), false);
            }
        }

        [Category("Database")]
        public string Format
        {
            get { return format; }
            set
            {
                format = value;
                OnPropertyChanged(nameof(Format), false);
            }
        }

        [Category("Database"), XmlIgnore]
        public int Order
        {
            get { return order; }
            set
            {
                if (order == value)
                    return;
                order = value;
                if (list != null)
                    list.SortInternal();
                OnPropertyChanged(nameof(Order), false);
            }
        }

        public DBForeignKey GetForeign()
        {
            return Table?.Foreigns.GetForeignByColumn(this);
        }

        [XmlIgnore, Category("Database")]
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

        [XmlIgnore, Browsable(false), Category("Database")]
        public bool IsView
        {
            get { return (Keys & DBColumnKeys.View) == DBColumnKeys.View; }
        }

        [XmlIgnore, Browsable(false), Category("Database")]
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
                    OnPropertyChanged(nameof(IsReference), false);
                }
            }
        }

        [Category("Database")]
        public DBDataType DBDataType
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
                OnPropertyChanged(nameof(DBDataType), true);
            }
        }

        [Browsable(false)]
        public Type TargetType { get { return typeof(DBItem); } }

        [Browsable(false), Category("Database")]
        public Type DataType
        {
            get { return dataType; }
            set
            {
                if (dataType == value)
                    return;
                dataType = value;
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

        [XmlIgnore, ReadOnly(true)]
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
                OnPropertyChanged(nameof(DefaultValue), true);
            }
        }

        [Category("Database"), DefaultValue(DBColumnTypes.Default)]
        public DBColumnTypes ColumnType
        {
            get { return ctype; }
            set
            {
                if (ctype == value)
                    return;
                ctype = value;
                OnPropertyChanged(nameof(ColumnType), false);
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
                OnPropertyChanged(nameof(Query), true);
            }
        }

        [Category("Database"), DefaultValue(0)]
        public int Size
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
                    OnPropertyChanged(nameof(Size), true);
                }
            }
        }

        [Category("Database"), DefaultValue(0)]
        public int Scale
        {
            get { return scale; }
            set
            {
                if (scale != value)
                {
                    scale = value;
                    OnPropertyChanged(nameof(Scale), true);
                }
            }
        }

        public virtual string SqlName
        {
            get { return Name; }
        }

        public bool IsPrimaryKey
        {
            get { return (Keys & DBColumnKeys.Primary) == DBColumnKeys.Primary; }
        }

        public bool IsNotNull
        {
            get { return (Keys & DBColumnKeys.Notnull) == DBColumnKeys.Notnull; }
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
            var column = new DBColumn(name);
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

        public override string FormatSql(DDLType ddlType)
        {
            var ddl = new StringBuilder();
            Schema?.Connection?.System.Format(ddl, this, ddlType);
            return ddl.ToString();
        }


        public object GetTag(int hindex)
        {
            return tags == null ? null : tags.TryGetValue(hindex, out var obj) ? obj : null;
        }

        public void RemoveTag(int hindex)
        {
            if (tags != null)
            {
                tags.Remove(hindex);
            }
        }

        public void SetTag(int hindex, object value)
        {
            if (value == null)
                RemoveTag(hindex);
            else
            {
                if (tags == null)
                    tags = new Dictionary<int, object>();
                tags[hindex] = value;
            }
        }

        public bool GetOld(int hindex, out object obj)
        {
            obj = null;
            return olds != null ? olds.TryGetValue(hindex, out obj) : false;
        }

        public void RemoveOld(int hindex)
        {
            if (olds != null)
            {
                olds.Remove(hindex);
            }
        }

        public void SetOld(int hindex, object value)
        {
            if (olds == null)
                olds = new Dictionary<int, object>();
            olds[hindex] = value;
        }

        public void Clear()
        {
            pull?.Clear();
            tags?.Clear();
            olds?.Clear();
        }

        public object Get(DBItem target)
        {
            return target.GetValue(this);
        }

        public void Set(DBItem target, object value)
        {
            target.SetValue(value, this);
        }

        public object Get(object target)
        {
            return Get((DBItem)target);
        }

        public void Set(object target, object value)
        {
            Set((DBItem)target, value);
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
                    AccessValue cash = new AccessValue();
                    cash.Read((byte[])val);
                    string rez = string.Empty;
                    foreach (var item in cash.Items)
                    {
                        rez += string.Format("{0}{1}", rez.Length > 0 ? "; " : string.Empty, item);
                    }
                    return rez;
                }
                else
                    return Helper.LengthFormat(((byte[])val).LongLength);
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
            else if (DataType == value.GetType())
                buf = value;
            else if (value is DBItem)
                buf = ((DBItem)value).PrimaryId;
            //else if (column.Pull.ItemType.IsG )
            //buf = ((DBItem)value).PrimaryId;
            else
                buf = ParseValue(value.ToString());

            if (buf is DateTime && buf.Equals(DateTime.MinValue))
                buf = null;
            return buf;
        }

        public object ParseValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;
            object val = null;
            var type = DataType;
            if (type == typeof(decimal))
            {
                if (decimal.TryParse(value.Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat, out var d))
                    val = d;
            }
            else if (type == typeof(DateTime))
            {
                var index = value.IndexOf('|');
                if (index >= 0)
                    value = value.Substring(0, index);
                DateTime date;
                if (value.Equals("getdate()", StringComparison.OrdinalIgnoreCase) || value.Equals("current_timestamp", StringComparison.OrdinalIgnoreCase))
                    val = DateTime.Now;
                if (DateTime.TryParse(value, out date))
                    val = date;
                else if (DateTime.TryParseExact(value, new string[] { "yyyyMMdd", "yyyyMM" }, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None, out date))
                    val = date;
            }
            else if (type == typeof(string))
                val = value;
            else if (type == typeof(int) || type.IsEnum)
            {
                if (int.TryParse(value, out int i))
                    val = i;
            }
            else if (type == typeof(byte))
            {
                if (byte.TryParse(value, out byte i))
                    val = i;
            }
            else if (type == typeof(TimeSpan))
            {
                val = TimeSpan.Parse(value);
            }
            else if (type == typeof(double))
            {
                if (double.TryParse(value, out double d))
                    val = d;
            }
            else if (type == typeof(float))
            {
                if (float.TryParse(value, out float f))
                    val = f;
            }
            else if (type == typeof(long))
            {
                if (long.TryParse(value, out long l))
                    val = l;
            }
            else if (type == typeof(bool))
            {
                if (bool.TryParse(value, out bool l))
                    val = l;
            }
            return val;
        }


    }
}