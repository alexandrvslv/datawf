/*
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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
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
    }

    public class DBColumn : DBTableItem, IComparable, IComparable<DBColumn>, ICloneable, IInvoker<DBItem, object>
    {
        public static DBColumn EmptyKey = new DBColumn();


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

        public override string ToString()
        {
            return base.ToString();
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

        [Browsable(false)]
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

        internal protected virtual void CheckPull()
        {
            if (Container == null
                || ColumnType == DBColumnTypes.Expression
                || ColumnType == DBColumnTypes.Code)
                return;
            if (Pull != null && Pull.ItemType != DataType)
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
        }

        protected void CheckIndex()
        {
            if (Index != null && Index.BasePull != pull)
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
                Index = DBPullIndexFabric.Create(Table, this);
            }
            else if (Index != null && !build)
            {
                Index.Dispose();
                Index = null;
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

        [Browsable(false), Category("Localizing"), ReadOnly(true)]
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
                if (gname == value)
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
                GroupName = value == null ? null : value.Name;
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

        public DBForeignKey GetForeign()
        {
            return Table?.Foreigns.GetForeignByColumn(this);
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

        [Browsable(false)]
        public bool IsPrimaryKey
        {
            get { return (Keys & DBColumnKeys.Primary) == DBColumnKeys.Primary; }
        }

        [Browsable(false)]
        public bool IsTypeKey
        {
            get { return (Keys & DBColumnKeys.ItemType) == DBColumnKeys.ItemType; }
        }

        [Browsable(false)]
        public bool IsNotNull
        {
            get { return (Keys & DBColumnKeys.Notnull) == DBColumnKeys.Notnull; }
        }

        [Browsable(false)]
        public bool IsSystem
        {
            get { return (Keys & DBColumnKeys.System) == DBColumnKeys.System; }
        }

        [JsonIgnore, XmlIgnore]
        public ColumnAttributeCache Attribute { get; internal set; }

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


        public virtual object GetTag(int hindex)
        {
            return tags == null ? null : tags.TryGetValue(hindex, out var obj) ? obj : null;
        }

        public virtual void RemoveTag(int hindex)
        {
            if (tags != null)
            {
                tags.Remove(hindex);
            }
        }

        public virtual void SetTag(int hindex, object value)
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

        public virtual bool GetOld(int hindex, out object obj)
        {
            obj = null;
            return olds != null ? olds.TryGetValue(hindex, out obj) : false;
        }

        public virtual void RemoveOld(int hindex)
        {
            if (olds != null)
            {
                olds.Remove(hindex);
            }
        }

        public virtual void SetOld(int hindex, object value)
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

        public object GetValue(DBItem target)
        {
            return Pull != null ? Pull.Get(target.handler) :
                TypeHelper.IsBaseType(target.GetType(), Attribute.PropertyInvoker.TargetType) ? Attribute.PropertyInvoker.GetValue(target) : null;
        }

        public T GetValue<T>(DBItem target)
        {
            return Pull != null ? Pull.GetValue<T>(target.handler) : (T)Attribute.PropertyInvoker.GetValue(target);
        }

        public object GetValue(object target)
        {
            return GetValue((DBItem)target);
        }

        public void SetValue(DBItem target, object value)
        {
            if (Pull != null)
            {
                Pull?.Set(target.handler, value);
            }
            else
            {
                Attribute.PropertyInvoker.SetValue(target, value);
            }
        }

        public void SetValue<T>(DBItem target, T value)
        {
            if (Pull != null)
            {
                Pull?.SetValue<T>(target.handler, value);
            }
            else
            {
                Attribute.PropertyInvoker.SetValue(target, value);
            }
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
            else if (DataType == value.GetType())
                buf = value;
            else if (value is DBItem item)
                buf = item.PrimaryId;
            else if (value is string text && DataType != typeof(string))
                buf = ParseValue(text);
            else if (value is long longValue)
            {
                if (DataType == typeof(int))
                    buf = (int)longValue;
                else if (DataType == typeof(short))
                    buf = (short)longValue;
                else if (DataType == typeof(byte))
                    buf = (byte)longValue;
                else if (DataType.IsEnum)
                    buf = (int)longValue;
            }
            else if (value is decimal mValue)
            {
                if (DataType == typeof(double))
                    buf = (double)mValue;
                if (DataType == typeof(float))
                    buf = (float)mValue;
            }
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
            else if (type == typeof(int))
            {
                if (int.TryParse(value, out int i))
                    val = i;
            }
            else if (type.IsEnum)
            {
                if (int.TryParse(value, out int i))
                {
                    val = i;
                }
                else
                {
                    val = Enum.Parse(type, value, true);
                }
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

        public IListIndex CreateIndex()
        {
            throw new NotImplementedException();
        }
    }
}