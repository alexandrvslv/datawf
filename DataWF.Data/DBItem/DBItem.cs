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
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataWF.Data
{

    [DataContract]
    //[JsonConverter(typeof(DBItemJsonConverter))]
    public class DBItem : ICloneable, IComparable<DBItem>, IComparable, IDisposable, IAccessable, ICheck, INotifyPropertyChanged, INotifyPropertyChanging, IEditable, IStatusable, IDBTableContent, IPullHandler
    {
        public static readonly DBItem EmptyItem = new DBItem() { cacheToString = "Loading" };

        public static DBTable<T> GetTable<T>() where T : DBItem, new()
        {
            return DBTable.GetTable<T>();
        }

        public object Tag;
        internal int handler = -1;
        internal short block = -1;
        internal short blockIndex = -1;
        internal string cacheToString = string.Empty;
        protected DBTable table;
        protected DBItemState state = DBItemState.New;
        protected internal DBUpdateState update = DBUpdateState.Insert;
        private AccessValue access;
        protected object saveLock = new object();

        public DBItem()
        {
            var table = DBTable.GetTable(GetType());
            if (table != null)
                Build(table);
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public bool Attached => (state & DBItemState.Attached) == DBItemState.Attached;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public int Handler { get => handler; set => handler = value; }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public short Block { get => block; set => block = value; }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public short BlockIndex { get => blockIndex; set => blockIndex = value; }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public virtual string ParametersCategory
        {
            get => Table.CodeKey != null ? GetValue<string>(Table.CodeKey) : "General";
            set { }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public bool IsChanged
        {
            get { return UpdateState != DBUpdateState.Default && (UpdateState & DBUpdateState.Commit) != DBUpdateState.Commit; }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public bool IsReferencingChanged
        {
            get { return IsChanged || GetPropertyReferencing().Any(p => p.IsReferencingChanged); }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public byte[] Image
        {
            get => this[Table.ImageKey] as byte[];
            set
            {
                if (Table.ImageKey == null)
                    return;
                this[Table.ImageKey] = value;
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public object PrimaryId
        {
            get => Table.PrimaryKey?.GetValue(this);
            set => SetValue(Table.PrimaryKey.ParseValue(value), Table.PrimaryKey);
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public string PrimaryCode
        {
            get => Table.CodeKey?.GetValue<string>(this);
            set => SetValue(value, Table.CodeKey);
        }

        [Browsable(false), DefaultValue(0)]
        [Column("item_type", GroupName = "system", Keys = DBColumnKeys.ItemType, Order = 0)]// | DBColumnKeys.Indexing
        public virtual int? ItemType
        {
            get => Table.ItemTypeKey?.GetValueNullable<int>(this);
            set => SetValueNullable(value, Table.ItemTypeKey);
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        DBStatus IStatusable.Status
        {
            get => (DBStatus)Status;
            set => Status = value;
        }

        [DefaultValue(DBStatus.New), Column("status_id", GroupName = "system", Keys = DBColumnKeys.State | DBColumnKeys.Indexing, Order = 99)]
        public DBStatus? Status
        {
            get => Table.StatusKey?.GetValueNullable<DBStatus>(this) ?? DBStatus.Empty;
            set => SetValueNullable(value, Table.StatusKey);
        }

        [Column("date_create", GroupName = "system", Keys = DBColumnKeys.Date | DBColumnKeys.System | DBColumnKeys.UtcDate, Order = 100)]
        public virtual DateTime? DateCreate
        {
            get => Table.DateKey?.GetValueNullable<DateTime>(this);
            set => SetValueNullable(value, Table.DateKey);
        }

        [Browsable(false)]
        [Column("date_update", GroupName = "system", Keys = DBColumnKeys.Stamp | DBColumnKeys.NoLog | DBColumnKeys.System | DBColumnKeys.UtcDate, Order = 101)]
        public DateTime? Stamp
        {
            get => Table.StampKey?.GetValueNullable<DateTime>(this);
            set => SetValueNullable(value, Table.StampKey);
        }

        IAccessValue IAccessable.Access { get => Access; set => Access = (AccessValue)value; }

        [Column("group_access", 512, DataType = typeof(byte[]), GroupName = "system", Keys = DBColumnKeys.Access | DBColumnKeys.System, Order = 102)]
        public virtual AccessValue Access
        {
            get => InternalAccess ?? Table.Access;
            set
            {
                if (Table.AccessKey != null)
                {
                    SetValue(value?.Serialize(), Table.AccessKey);
                }
                access = value;
            }
        }

        [XmlIgnore, JsonIgnore, NotMapped, Browsable(false)]
        public AccessValue InternalAccess => access ?? (access = ReadAccess());

        //[Browsable(false)]
        //public virtual string Name
        //{
        //    get { return GetName(nameof(Name)); }
        //    set { SetName(nameof(Name), value); }
        //}

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public virtual DBTable Table
        {
            get => table;
            set
            {
                if (table != value)
                {
                    table = value is IDBVirtualTable virtualTable ? virtualTable.BaseTable : value;
                    handler = table.GetNextHandler(out block, out blockIndex);
                }
            }
        }

        public object this[int columnIndex]
        {
            get => this[Table.Columns[columnIndex]];
            set => this[Table.Columns[columnIndex]] = value;
        }

        public object this[string code]
        {
            get
            {
                if (code == null)
                    return null;
                DBItem row = this;
                int pi = 0, i = code.IndexOf('.');
                while (i > 0)
                {
                    var scolumn = row.Table.ParseColumnProperty(code.Substring(pi, i - pi));
                    if (scolumn == null)
                        return null;
                    var item = row.GetReference(scolumn);
                    if (item == null)
                        return null;
                    row = item;
                    pi = i + 1;
                    i = code.IndexOf('.', pi);
                }
                return row[row.Table.ParseColumnProperty(code.Substring(pi))];
            }
            set
            {
                DBItem row = this;
                int pi = 0, i = code.IndexOf('.');
                while (i > 0)
                {
                    var item = row.GetReference(row.Table.ParseColumnProperty(code.Substring(pi, i - pi)));
                    if (item == null)
                        return;
                    row = item;
                    pi = i + 1;
                    i = code.IndexOf('.', pi);
                }
                row[row.Table.ParseColumnProperty(code.Substring(pi))] = value;
            }
        }

        public object this[DBColumn column]
        {
            get
            {
                if (column == null)
                    return null;
                if (column.ColumnType == DBColumnTypes.Expression)
                    return column.GetExpression().GetValue(this);

                return GetValue(column);
            }
            set
            {
                if (column == null)
                    return;

                SetValue(column.ParseValue(value),
                         column,
                         DBSetValueMode.Default);
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBItemState State
        {
            get => state;
            set
            {
                if (state != value)
                {
                    state = value;
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public virtual DBUpdateState UpdateState
        {
            get => update;
            set
            {
                if (update != value)
                {
                    update = value;
                    OnPropertyChanged(nameof(UpdateState));
                    //var arg = new DBItemEventArgs(this) { State = update };
                    //DBService.OnStateEdited(arg);
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public bool Check
        {
            get => (state & DBItemState.Check) == DBItemState.Check;
            set
            {
                if (Check != value)
                {
                    state = value ? state | DBItemState.Check : state & ~DBItemState.Check;
                    OnPropertyChanged();
                }
            }
        }

        public bool GetOld(DBColumn column, out object value)
        {
            return column.GetOld(handler, out value);
        }

        public void RemoveOld(DBColumn column)
        {
            column.RemoveOld(handler);
        }

        public void SetOld(DBColumn column, object value)
        {
            column.SetOld(handler, value);
        }

        protected object GetPropertyValue(DBColumn column)
        {
            return column.PropertyInvoker != null ? column.PropertyInvoker.GetValue(this) : GetValue(column);
        }

        protected void SetPropertyValue(object value, DBColumn column, DBSetValueMode mode = DBSetValueMode.Default)
        {
            if (column.PropertyInvoker != null)
                column.PropertyInvoker.SetValue(this, value);
            else
                SetValue(value, column, mode);
        }

        public object GetValue(DBColumn column)
        {
            return column.GetValue(this);
        }

        public T GetValue<T>(DBColumn column)
        {
            return column.GetValue<T>(this);
        }

        public T? GetValueNullable<T>(DBColumn column) where T : struct
        {
            return column.GetValueNullable<T>(this);
        }

        public T GetProperty<T>([CallerMemberName] string property = null)
        {
            return GetValue<T>(Table.Columns.GetByProperty(property));
        }

        private void RefreshOld(DBColumn column, object value, object field)
        {
            if (GetOld(column, out object old))
            {
                if (DBService.Equal(old, value))
                {
                    RemoveOld(column);
                }
            }
            else
            {
                SetOld(column, field);
            }
        }

        private void CheckState(DBTransaction transaction)
        {
            var temp = UpdateState;
            if (temp == DBUpdateState.Default || (temp & DBUpdateState.Commit) == DBUpdateState.Commit)
            {
                temp &= ~DBUpdateState.Commit;
                temp |= DBUpdateState.Update;
                transaction?.AddItem(this);
            }
            else if (temp == DBUpdateState.Update && !GetIsChanged())
            {
                temp = DBUpdateState.Default;
            }
            UpdateState = temp;
            //DBService.OnEdited(args);
        }
        public void SetProperty<T>(T value, [CallerMemberName] string property = null)
        {
            SetValue<T>(value, Table.Columns.GetByProperty(property));
        }

        public void SetPropertyNullable<T>(T? value, [CallerMemberName] string property = null) where T : struct
        {
            SetValueNullable<T>(value, Table.Columns.GetByProperty(property));
        }

        public void SetPropertyClass<T>(T value, [CallerMemberName] string property = null) where T : class
        {
            SetValueClass<T>(value, Table.Columns.GetByProperty(property));
        }

        public void SetValue<T>(T value, DBColumn column)
        {
            SetValue<T>(value, column, DBSetValueMode.Default);
        }

        public void SetValueClass<T>(T value, DBColumn column) where T : class
        {
            SetValueClass<T>(value, column, DBSetValueMode.Default);
        }

        public void SetValueNullable<T>(T? value, DBColumn column) where T : struct
        {
            SetValueNullable<T>(value, column, DBSetValueMode.Default);
        }

        public void SetValue(object value, DBColumn column)
        {
            SetValue(value, column, DBSetValueMode.Default);
        }

        public void SetValue<T>(T value, DBColumn column, DBSetValueMode mode)
        {
            if (mode == DBSetValueMode.Loading && !Attached)
            {
                column.SetValue<T>(this, value);
                return;
            }
            var check = mode == DBSetValueMode.Default && column.ColumnType == DBColumnTypes.Default;
            var oldValue = column.GetValue<T>(this);

            if (DBService.Equal<T>(oldValue, value))
            {
                return;
            }
            if (check)
            {
                RefreshOld(column, value, oldValue);
            }

            OnPropertyChanging<T>(column.Property ?? column.Name, column, oldValue);

            column.SetValue<T>(this, value);

            OnPropertyChanged<T>(column.Property ?? column.Name, column, value);

            if (check)
            {
                CheckState(null);
            }
        }

        public void SetValueClass<T>(T value, DBColumn column, DBSetValueMode mode) where T : class
        {
            if (mode == DBSetValueMode.Loading && !Attached)
            {
                column.SetValue<T>(this, value);
                return;
            }
            var check = mode == DBSetValueMode.Default && column.ColumnType == DBColumnTypes.Default;
            var oldValue = column.GetValue<T>(this);

            if (DBService.EqualClass<T>(oldValue, value))
            {
                return;
            }
            if (check)
            {
                RefreshOld(column, value, oldValue);
            }

            OnPropertyChanging<T>(column.Property ?? column.Name, column, oldValue);

            column.SetValue<T>(this, value);

            OnPropertyChanged<T>(column.Property ?? column.Name, column, value);

            if (check)
            {
                CheckState(null);
            }
        }

        public void SetValueNullable<T>(T? value, DBColumn column, DBSetValueMode mode) where T : struct
        {
            if (mode == DBSetValueMode.Loading && !Attached)
            {
                column.SetValueNullable<T>(this, value);
                return;
            }
            var check = mode == DBSetValueMode.Default && column.ColumnType == DBColumnTypes.Default;
            var oldValue = column.GetValueNullable<T>(this);

            if (Nullable.Equals<T>(oldValue, value))
            {
                return;
            }
            if (check)
            {
                RefreshOld(column, value, oldValue);
            }

            OnPropertyChanging<T?>(column.Property ?? column.Name, column, oldValue);

            column.SetValueNullable<T>(this, value);

            OnPropertyChanged<T?>(column.Property ?? column.Name, column, value);

            if (check)
            {
                CheckState(null);
            }
        }

        public void SetValue(object value, DBColumn column, DBSetValueMode mode)
        {
            if (mode == DBSetValueMode.Loading && !Attached)
            {
                column.SetValue(this, value);
                return;
            }
            var check = mode == DBSetValueMode.Default && column.ColumnType == DBColumnTypes.Default;
            var oldValue = column.GetValue(this);

            if (DBService.Equal(oldValue, value))
            {
                return;
            }
            if (check)
            {
                RefreshOld(column, value, oldValue);
            }

            OnPropertyChanging(column.Property ?? column.Name, column, oldValue);

            column.SetValue(this, value);

            OnPropertyChanged(column.Property ?? column.Name, column, value);

            if (check)
            {
                CheckState(null);
            }
        }

        public bool IsChangedKey(DBColumn column)
        {
            return column.GetOld(handler, out object value);
        }

        public virtual bool GetIsChanged()
        {
            return GetChangeKeys().Any();
        }

        public IEnumerable<DBColumn> GetChangeKeys()
        {
            foreach (var column in Table.Columns)
            {
                if (column.GetOld(handler, out object value))
                {
                    yield return column;
                }
            }
        }

        public void SetValues(object[] values)
        {
            if (values == null)
                return;

            for (int i = 0; i < values.Length; i++)
            {
                var Column = Table.Columns[i];

                if (Column.ColumnType == DBColumnTypes.Default || Column.ColumnType == DBColumnTypes.Query)
                {
                    SetValue(values[i], Column, DBSetValueMode.Loading);
                }
            }
        }

        public void SetValues(Dictionary<string, object> values, DBTable Table)
        {
            if (values == null)
                return;

            foreach (var kvp in values)
            {
                var column = Table.ParseColumn(kvp.Key);
                if (column != null)
                {
                    SetValue(kvp.Value, column, DBSetValueMode.Loading);
                }
            }
        }

        protected virtual internal void RemoveOld()
        {
            foreach (DBColumn column in Table.Columns)
            {
                RemoveOld(column);
            }
        }

        public DBItem GetReference(string code, DBLoadParam param = DBLoadParam.Load | DBLoadParam.Referencing)
        {
            DBItem row = this;
            int pi = 0, i = code.IndexOf('.');
            while (i > 0)
            {
                var item = row.GetReference(row.Table.Columns[code.Substring(pi, i - pi)], param);
                if (item == null)
                    return null;
                row = item;
                pi = i + 1;
                i = code.IndexOf('.', pi);
            }
            return row.GetReference(row.Table.Columns[code.Substring(pi)], param);
        }

        public DBItem GetReference(DBColumn column, DBLoadParam param = DBLoadParam.Load | DBLoadParam.Referencing)
        {
            if (column == null)
                return null;
            if (!column.IsReference)
                return null;
            if (column.IsPrimaryKey)
                return this;
            object value = GetPropertyValue(column);
            if (value == null)
                return null;
            return column.ReferenceTable.LoadItemById(value, param);
        }

        public DBItem GetReference(string code, ref DBItem item, DBLoadParam param = DBLoadParam.Load | DBLoadParam.Referencing)
        {
            if (item != null)
                return item;
            return item = GetReference(code, param);
        }

        public DBItem GetReference(DBColumn column, ref DBItem item, DBLoadParam param = DBLoadParam.Load | DBLoadParam.Referencing)
        {
            object value = GetPropertyValue(column);
            if (value != null && value.Equals(item?.PrimaryId))
                return item;

            return item = value == null ? null : column.ReferenceTable.LoadItemById(value, param);
        }

        public T GetReference<T>(DBColumn column, ref T item, DBLoadParam param = DBLoadParam.Load | DBLoadParam.Referencing) where T : DBItem
        {
            object value = GetPropertyValue(column);
            if (value != null && value.Equals(item?.PrimaryId))
                return item;

            return item = value == null ? (T)null : (T)column.ReferenceTable.LoadItemById(value, param);
        }

        public T GetPropertyReference<T>(ref T item, [CallerMemberName] string property = null) where T : DBItem
        {
            var column = Table.Columns.GetByReferenceProperty(property);
            return GetReference(column, ref item);
        }

        //public T GetReference<T>(string code, DBLoadParam param = DBLoadParam.Load | DBLoadParam.Referencing) where T : DBItem, new()
        //{
        //    DBItem row = this;
        //    int pi = 0, i = code.IndexOf('.');
        //    while (i > 0)
        //    {
        //        var item = row.GetReference<T>(row.Table.Columns[code.Substring(pi, i - pi)], param);
        //        if (item == null)
        //            return null;
        //        row = item;
        //        pi = i + 1;
        //        i = code.IndexOf('.', pi);
        //    }
        //    return row.GetReference<T>(row.Table.Columns[code.Substring(pi)], param);
        //}

        public T GetReference<T>(DBColumn column, DBLoadParam param = DBLoadParam.Load | DBLoadParam.Referencing) where T : DBItem
        {
            return (T)GetReference(column, param);
        }

        public DBItem SetReference(DBItem value, string column)
        {
            return SetReference(value, Table.Columns[column]);
        }

        public T SetPropertyReference<T>(T value, [CallerMemberName] string property = null) where T : DBItem
        {
            return SetReference(value, Table.Columns.GetByReferenceProperty(property));
        }

        public DBItem SetReference(DBItem value, DBColumn column)
        {
            SetPropertyValue(value?.PrimaryId, column);
            return value;
        }

        public T SetReference<T>(T value, DBColumn column) where T : DBItem
        {
            SetPropertyValue(value?.PrimaryId, column);
            return value;
        }

        public bool GetBool(DBColumn column)
        {
            if (column == null || (column.Keys & DBColumnKeys.Boolean) != DBColumnKeys.Boolean)
                return false;

            return this[column].ToString() == column.BoolTrue;
        }

        public bool GetBool(string ColumnCode)
        {
            return GetBool(Table.Columns[ColumnCode]);
        }

        public void SetBool(DBColumn Column, bool value)
        {
            if (Column == null || (Column.Keys & DBColumnKeys.Boolean) != DBColumnKeys.Boolean)
                return;

            this[Column] = value ? Column.BoolTrue : Column.BoolFalse;
        }

        public void SetBool(string ColumnCode, bool value)
        {
            SetBool(Table.Columns[ColumnCode], value);
        }

        public Task Delete(DBTransaction transaction)
        {
            Delete();
            return Save(transaction);
        }

        public void Delete()
        {
            if ((UpdateState & DBUpdateState.Insert) == DBUpdateState.Insert)
            {
                Detach();
            }
            else
            {
                if (UpdateState != (DBUpdateState.Delete | DBUpdateState.Commit))
                    UpdateState |= DBUpdateState.Delete;
            }
        }

        public void SetReferencing<T>(IEnumerable<T> items, DBColumn column) where T : DBItem, new()
        {
            if (items == null)
            {
                return;
            }

            GenerateId();
            foreach (var item in items)
            {
                item.SetReference(this, column);
                item.Attach();
            }
        }

        public void SetReferencing<T>(IEnumerable<T> items, string property) where T : DBItem, new()
        {
            var table = DBTable.GetTable<T>();
            SetReferencing<T>(items, table.ParseProperty(property));
        }

        public IEnumerable<T> GetReferencing<T>(DBTable<T> table, QQuery query, DBLoadParam param) where T : DBItem, new()
        {
            //query.TypeFilter = typeof(T);
            if ((param & DBLoadParam.Load) == DBLoadParam.Load)
            {
                table.Load(query, param);
            }
            return table.Select(query);
        }

        public IEnumerable<T> GetReferencing<T>(string property, DBLoadParam param) where T : DBItem, new()
        {
            var table = DBTable.GetTable<T>();
            return GetReferencing<T>(table, table.ParseProperty(property), param);
        }

        public IEnumerable<T> GetReferencing<T>(DBColumn column, DBLoadParam param) where T : DBItem, new()
        {
            return GetReferencing<T>(GetTable<T>(), column, param);
        }

        public IEnumerable<T> GetReferencing<T>(DBTable<T> table, DBColumn column, DBLoadParam param) where T : DBItem, new()
        {
            if ((param & DBLoadParam.Load) == DBLoadParam.Load)
            {
                using (var query = new QQuery("", table))
                {
                    query.BuildParam(column, CompareType.Equal, PrimaryId);
                    return GetReferencing<T>(table, query, param);
                }
            }
            else
            {
                return table.Select(column, CompareType.Equal, PrimaryId);
            }
        }

        public IEnumerable<T> GetReferencing<T>(string tableCode, string columnCode, DBLoadParam param) where T : DBItem, new()
        {
            var table = (DBTable<T>)Table.Schema.ParseTable(tableCode);
            return table != null ? GetReferencing<T>(table, table.ParseColumn(columnCode), param) : null;
        }

        public IEnumerable<T> GetReferencing<T>(DBForeignKey relation, DBLoadParam param) where T : DBItem, new()
        {
            return GetReferencing<T>((DBTable<T>)relation.Table, relation.Column, param);
        }

        public IEnumerable<DBItem> GetReferencing(DBTable table, DBColumn column, DBLoadParam param)
        {
            if ((param & DBLoadParam.Load) == DBLoadParam.Load)
            {
                using (var query = new QQuery("", table))
                {
                    query.BuildParam(column, CompareType.Equal, PrimaryId);
                    return GetReferencing(table, query, param);
                }
            }
            else
            {
                return table.SelectItems(column, CompareType.Equal, PrimaryId);
            }
        }

        public IEnumerable<DBItem> GetReferencing(DBTable table, QQuery query, DBLoadParam param)
        {
            if ((param & DBLoadParam.Load) == DBLoadParam.Load)
            {
                return table.LoadItems(query);
            }

            return table.SelectItems(query);
        }

        public IEnumerable<DBItem> GetReferencing(DBForeignKey relation, DBLoadParam param)
        {
            return GetReferencing(relation.Table, relation.Column, param);
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(cacheToString))
                cacheToString = GetRowText();
            return cacheToString;
        }

        public string Format(DBColumn column)
        {
            return column.FormatValue(GetValue(column));
        }

        public string Format(string code)
        {
            return Format(Table.Columns[code]);
        }

        public void Build(DBTable table, bool setDefauilts = true, int itemType = -1)
        {
            Table = table;
            if (setDefauilts)
            {
                SetDefaults();
            }
            if (Table.ItemTypeKey != null)
            {
                SetValueNullable<int>(itemType < 0 ? table.GetTypeIndex(GetType()) : itemType, table.ItemTypeKey, DBSetValueMode.Loading);
            }
        }

        public void SetDefaults()
        {
            foreach (DBColumn column in Table.Columns)
            {
                if (!string.IsNullOrEmpty(column.DefaultValue))
                    SetValue(column.ParseValue(column.DefaultValue), column, DBSetValueMode.Loading);
            }
        }

        public void SetCultureStrings(string @group, LocaleItem value)
        {
            foreach (LocaleString c in value)
            {
                SetName(c.Culture, c.Value, @group);
            }
        }

        public LocaleItem GetCultureStrings(string @group, string cultures)
        {
            var cs = new LocaleItem();
            if (string.IsNullOrEmpty(cultures))
            {
                foreach (DBColumn col in Table.Columns.GetByGroup(@group))
                {
                    if (col.IsCulture)
                        cs.Add(this[col].ToString(), col.Culture);
                }
            }
            else
            {
                var temp = cultures.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in temp)
                {
                    string ts = s.Trim();
                    string val = GetValue<string>(Table.GetCultureColumn(@group, CultureInfo.GetCultureInfo(ts)));
                    if (val != null)
                        cs.Add(val, ts);
                }
            }
            return cs;
        }

        public string GetName([CallerMemberName] string @group = null)
        {
            return GetValue<string>(Table.GetNameKey(@group));
        }

        public void SetName(string value, [CallerMemberName] string @group = null)
        {
            SetValue(value, Table.GetNameKey(@group));
        }

        public void SetName(CultureInfo culture, string value, [CallerMemberName] string @group = null)
        {
            if (culture == null)
                return;
            foreach (var column in Table.Columns.GetByGroup(@group))
            {
                if (column.Culture.ThreeLetterISOLanguageName == culture.ThreeLetterISOLanguageName)
                {
                    this[column] = value;
                    break;
                }
            }
        }

        private AccessValue ReadAccess()
        {
            var accessData = Table.AccessKey != null ? GetValue<byte[]>(Table.AccessKey) : null;
            return accessData != null ? new AccessValue(accessData) : null;
        }

        public void Accept(string column)
        {
            Accept(Table.Columns[column]);
        }

        public void Accept(DBColumn column)
        {
            RemoveOld(column);
        }

        public virtual void Accept(IUserIdentity user)
        {
            OnAccepting(user);
            DBService.OnAccept(new DBItemEventArgs(this, null, user));
            if (IsChanged || (UpdateState & DBUpdateState.Commit) == DBUpdateState.Commit)
            {
                if ((UpdateState & DBUpdateState.Delete) == DBUpdateState.Delete)
                {
                    Detach();
                    UpdateState = DBUpdateState.Delete;
                }
                else
                {
                    Table.Accept(this);
                    UpdateState = DBUpdateState.Default;
                }
                RemoveOld();
            }
            OnAccepted(user);
        }

        public virtual void OnAccepted(IUserIdentity user)
        {
        }

        public virtual void OnAccepting(IUserIdentity user)
        {
        }

        public void Reject(string column)
        {
            Reject(Table.Columns[column]);
        }

        public void Reject(DBColumn column)
        {
            if (GetOld(column, out object value))
                SetValue(value, column);
        }

        public bool Changed(DBColumn column)
        {
            return column != null ? GetOld(column, out object value) : false;
        }

        public virtual void Reject(IUserIdentity user)
        {
            if (IsChanged || (UpdateState & DBUpdateState.Commit) == DBUpdateState.Commit)
            {
                if ((UpdateState & DBUpdateState.Insert) == DBUpdateState.Insert)
                {
                    Detach();
                    UpdateState = DBUpdateState.Insert;
                }
                else
                {
                    foreach (var column in Table.Columns)
                    {
                        if (GetOld(column, out object old))
                        {
                            SetValue(old, column, DBSetValueMode.Loading);
                            RemoveOld(column);
                        }
                    }
                    UpdateState = DBUpdateState.Default;
                }
                DBService.OnReject(new DBItemEventArgs(this, null, user));
            }
        }

        #region ICloneable Members

        public virtual object Clone()
        {
            var item = Table.NewItem(DBUpdateState.Insert, false, ItemType ?? 0);
            CopyTo(item);
            if (Table.PrimaryKey != null)
            {
                item.SetValue(null, Table.PrimaryKey);
                //item.GenerateId();
            }
            return item;
        }

        public void CopyTo(DBItem item)
        {
            foreach (var column in Table.Columns)
            {
                if (column.ColumnType == DBColumnTypes.Default)
                {
                    var value = GetValue(column);
                    if (value != null)
                    {
                        item.SetValue(value, column);
                    }
                }
            }
        }

        #endregion

        public void Attach()
        {
            if (!Attached)
            {
                Table.Add(this);
            }
        }

        public void Detach()
        {
            if (Attached)
            {
                Table.Remove(this);
            }
        }

        public virtual void OnAttached()
        {
            if (Attached)
                return;
            State |= DBItemState.Attached;
            OnPropertyChanged(nameof(Attached), null);
            //DBService.OnAdded(this);
        }

        public virtual void OnDetached()
        {
            if (!Attached)
                return;
            State &= ~DBItemState.Attached;
            OnPropertyChanged(nameof(Attached), null);
            //DBService.OnRemoved(this);
        }

        public virtual bool OnUpdating(DBItemEventArgs arg)
        {
            return Table.OnUpdating(arg);
        }

        public virtual void OnUpdated(DBItemEventArgs arg)
        {
            Table.OnUpdated(arg);
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangingEventHandler PropertyChanging;

        protected void OnPropertyChanging<V>(string property, DBColumn column = null, V value = default(V))
        {
            if (Attached)
            {
                Table.OnItemChanging<V>(this, property, column, value);
            }
            RaisePropertyChanging(property);
        }

        protected void OnPropertyChanging(string property, DBColumn column = null, object value = null)
        {
            if (Attached)
            {
                Table.OnItemChanging(this, property, column, value);
            }
            RaisePropertyChanging(property);
        }

        protected virtual void RaisePropertyChanging(string property)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(property));
        }

        protected void OnPropertyChanged<V>([CallerMemberName] string property = null, DBColumn column = null, V value = default(V))
        {
            if (column != null && (column.Keys & DBColumnKeys.View) == DBColumnKeys.View)
            {
                cacheToString = string.Empty;
            }
            if (Attached)
            {
                Table.OnItemChanged<V>(this, property, column, value);
                if (property == nameof(Access) && access != null)
                {
                    access = ReadAccess();
                }
            }
            RaisePropertyChanged(property);
        }

        protected void OnPropertyChanged([CallerMemberName] string property = null, DBColumn column = null, object value = null)
        {
            if (column != null && (column.Keys & DBColumnKeys.View) == DBColumnKeys.View)
            {
                cacheToString = string.Empty;
            }
            if (Attached)
            {
                Table.OnItemChanged(this, property, column, value);
                if (property == nameof(Access) && access != null)
                {
                    access = ReadAccess();
                }
            }
            RaisePropertyChanged(property);
        }

        protected virtual void RaisePropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        #endregion

        public void Refresh(IUserIdentity user)
        {
            Reject(user);
            Table.ReloadItem(PrimaryId);
        }

        public void GenerateId(DBTransaction transaction = null)
        {
            if (Table.Sequence == null || Table.PrimaryKey == null)
                return;
            if (PrimaryId == null)
            {
                PrimaryId = transaction != null ? Table.Sequence.GetNext(transaction) : Table.Sequence.GetNext();
            }
            else
            {
                Table.Sequence.SetCurrent(PrimaryId);
            }
        }

        public void Free()
        {
            Detach();
            foreach (DBColumn column in Table.Columns)
            {
                column.SetValue(this, null);
            }
            Table.FreeHandlers.Enqueue(handler);
        }

        public Task Save()
        {
            return Save((IUserIdentity)null);
        }

        public async Task Save(IUserIdentity user)
        {
            using (var transaction = new DBTransaction(Table.Connection, user))
            {
                try
                {
                    await Save(transaction);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                    transaction.Rollback();
                    throw new Exception("Save Error:", ex);
                }
            }
        }

        public virtual async Task Save(DBTransaction transaction)
        {
            if (transaction.AddItem(this))
            {
                SnapshotReferencing(transaction);
                if (await OnSaving(transaction))
                {
                    await SaveReferenced(transaction);
                    await Table.SaveItem(this, transaction);
                    await SaveReferencing(transaction);
                    await OnSaved(transaction);
                }
                else
                {
                    RejectReferencing(transaction);
                }
            }
        }

        protected virtual Task OnSaved(DBTransaction transaction)
        {
            return Task.CompletedTask;
        }

        protected virtual Task<bool> OnSaving(DBTransaction transaction)
        {
            return Task.FromResult(true);
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as DBItem);
        }

        public virtual int CompareTo(DBItem obj)
        {
            return obj == null ? 1 : table.index == obj.table.index ? handler.CompareTo(obj.handler) :
                table.index.CompareTo(obj.table.index);
        }

        public string FormatPatch()
        {
            var rez = new StringBuilder();
            rez.AppendLine(string.Format("if exists(select * from {0} where {1}={2})", Table.Name, Table.PrimaryKey.Name, PrimaryId));
            rez.AppendLine("    " + Table.System.FormatCommand(Table, DBCommandTypes.Update, this) + ";");
            rez.AppendLine("else");
            rez.AppendLine("    " + Table.System.FormatCommand(Table, DBCommandTypes.Insert, this) + ";");
            rez.AppendLine();
            return rez.ToString();
        }

        public virtual void Dispose()
        {

        }

        public string GetRowText(bool allColumns, bool showColumn, string separator)
        {
            return GetRowText((allColumns ? (IEnumerable<DBColumn>)Table.Columns : Table.Columns.GetIsView()), showColumn, separator);
        }

        public string GetRowText()
        {
            return GetRowText(Table.Columns.GetIsView(), false, " - ");
        }

        public string GetRowText(IEnumerable<DBColumn> parameters)
        {
            return GetRowText(parameters, false, " - ");
        }

        public string GetRowText(IEnumerable<DBColumn> parameters, bool showColumn, string separator)
        {
            //if (!Access.View)
            //    return "********";
            var builder = new StringBuilder();
            if (parameters == null)
                parameters = Table.Columns;
            //if (!parameters.Any())
            //{
            //    if (row.Table.CodeKey != null)
            //        parameters.Add(row.Table.CodeKey);
            //    else if (row.Table.PrimaryKey != null)
            //        parameters.Add(row.Table.PrimaryKey);
            //}
            string c = string.Empty;
            foreach (DBColumn column in parameters)
            {
                string header = (showColumn) ? header = $"{column}: " : string.Empty;
                string value = column.FormatValue(GetValue(column));
                if (column.IsCulture)
                {
                    if (value.Length != 0)
                    {
                        if (column.Culture.TwoLetterISOLanguageName == Locale.Instance.Culture.TwoLetterISOLanguageName)
                        {
                            builder.Append(header);
                            builder.Append(value);
                            builder.Append(separator);
                            c = null;
                        }
                        else if (c != null && c.Length == 0)
                        {
                            c = header + value;
                        }
                    }
                }
                else if (value.Length > 0)
                {
                    builder.Append(header);
                    builder.Append(value);
                    builder.Append(separator);
                }
            }
            if (c != null)
            {
                builder.Append(c);
            }
            if (builder.Length > separator.Length)
                builder.Length -= separator.Length;

            return builder.ToString();
        }

        public async Task Delete(int recurs, DBLoadParam param = DBLoadParam.None)
        {
            using (DBTransaction transaction = new DBTransaction(Table.Schema.Connection))
            {
                try
                {
                    await Delete(transaction, recurs, param);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                    transaction.Rollback();
                }
            }
        }

        public async Task Delete(DBTransaction transaction, int recurs, DBLoadParam param = DBLoadParam.None)
        {
            var dependencies = GetChilds(recurs, param).ToList();

            foreach (var item in dependencies)
            {
                item.Delete();
                if (item.Attached)
                {
                    await item.Save(transaction);
                }
            }
            Delete();
            if (Attached)
            {
                await Save(transaction);
            }
        }

        public IEnumerable<DBItem> GetChilds(int recurs = 2, DBLoadParam param = DBLoadParam.None)
        {
            recurs--;
            if (recurs < 0)//
            {
                yield break;
            }
            foreach (var relation in Table.GetChildRelations())
            {
                if (relation.Table is IDBLogTable
                    || relation.Table is IDBVirtualTable
                    || relation.Table.Type != DBTableType.Table
                    || relation.Column.ColumnType != DBColumnTypes.Default)
                    continue;

                foreach (DBItem item in GetReferencing(relation, param).ToList())
                {
                    if (item != this)
                    {
                        foreach (var child in item.GetChilds(recurs, param))
                        {
                            if (child != this)
                            {
                                yield return child;
                            }
                        }
                        yield return item;
                    }
                }
            }
        }

        public Task Merge(List<string> ids, DBTransaction transaction)
        {
            var items = Table.LoadItemsById(ids, transaction);
            return Merge(items, transaction);
        }

        public async Task Merge(IEnumerable<DBItem> list)
        {
            using (var transaction = new DBTransaction(Table.Connection))
            {
                try
                {
                    await Merge(list, transaction);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                    transaction.Rollback();
                }
            }
        }

        public virtual async Task Merge(IEnumerable<DBItem> list, DBTransaction transaction)
        {
            var relations = Table.GetChildRelations().ToList();
            var rows = new List<DBItem> { this };
            foreach (DBItem item in list)
            {
                if (item == this)
                {
                    continue;
                }

                rows.Add(item);

                item.UpdateState |= DBUpdateState.Delete;
                foreach (DBColumn column in item.Table.Columns)
                {
                    if (column.ColumnType != DBColumnTypes.Default)
                        continue;
                    if (GetValue(column) == null && item.GetValue(column) != null)
                    {
                        SetValue(item.GetValue(column), column);
                    }
                }

                foreach (DBForeignKey relation in relations)
                {
                    if (relation.Table.Type == DBTableType.Table)
                    {
                        var referencing = item.GetReferencing(relation, DBLoadParam.Load | DBLoadParam.Referencing).ToList();
                        if (referencing.Count > 0)
                        {
                            foreach (DBItem subItem in referencing)
                            {
                                subItem.SetValue(PrimaryId, relation.Column);
                                await relation.Table.SaveItem(subItem, transaction);
                            }
                        }
                    }
                }
            }
            foreach (var item in rows)
            {
                await Table.SaveItem(item, transaction);
            }
        }

        public Task SaveOrUpdate(DBTransaction transaction, DBLoadParam param = DBLoadParam.None)
        {
            var exist = FindAndUpdate(param);
            if (exist != null)
            {
                return exist.Save(transaction);
            }
            else
            {
                return Save(transaction);
            }
        }

        public virtual async Task SaveReferenced(DBTransaction transaction)
        {
            if ((UpdateState & DBUpdateState.Delete) == DBUpdateState.Delete)
            {
                return;
            }

            foreach (var column in Table.Columns.GetIsReference())
            {
                var invoker = column.ReferencePropertyInvoker;
                if (column.ColumnType == DBColumnTypes.Default
                    && invoker != null
                    && invoker.TargetType.IsAssignableFrom(GetType()))
                {
                    var item = invoker.GetValue(this) as DBItem;

                    if (item != null && item != this && item.IsChanged)
                    {
                        await item.Save(transaction);
                        if (GetValue(column) == null)
                        {
                            SetValue(item.PrimaryId, column);
                        }
                    }
                }
            }
        }

        public IEnumerable<DBItem> GetPropertyReferencing()
        {
            foreach (var referencing in Table.GetPropertyReferencing(GetType()))
            {
                var references = (IEnumerable)referencing.PropertyInvoker.GetValue(this);
                if (references != null)
                {
                    foreach (DBItem item in references)
                    {
                        yield return item;
                    }
                }
            }
        }

        public void CacheReferencing(DBTransaction transaction)
        {
            foreach (var relation in Table.GetChildRelations())
            {
                if (relation.Table != Table && !(relation.Table is IDBVirtualTable))
                {
                    var references = GetReferencing(relation, DBLoadParam.None);
                    foreach (DBItem reference in references)
                    {
                        if (reference.IsChanged)
                        {
                            transaction.AddItem(reference);
                        }
                    }
                }
            }
        }

        public void SnapshotReferencing(DBTransaction transaction)
        {
            foreach (DBItem item in GetPropertyReferencing())
            {
                if (item.IsChanged)
                {
                    transaction.AddItem(item);
                }
            }
        }

        public void RejectReferencing(DBTransaction transaction)
        {
            foreach (DBItem item in GetPropertyReferencing())
            {
                if (item.IsChanged && transaction.RemoveItem(item))
                {
                    item.Reject(transaction.Caller);
                }
            }
        }

        public async Task SaveReferencing(DBTransaction transaction)
        {
            foreach (DBItem item in GetPropertyReferencing())
            {
                if (item.IsChanged || item.IsReferencingChanged)
                {
                    await item.Save(transaction);
                }
            }
        }

        public void AttachOrUpdate(DBLoadParam param = DBLoadParam.None)
        {
            var exist = FindAndUpdate(param);
            if (exist == null)
            {
                Attach();
            }
        }

        public DBItem FindAndUpdate(DBLoadParam param = DBLoadParam.None)
        {
            var exist = PrimaryId == null
                ? Table.LoadItemByCode(PrimaryCode, Table.CodeKey, param)
                : Table.LoadItemById(PrimaryId, param);
            if (exist != null)
            {
                PrimaryId = exist.PrimaryId;

                foreach (var column in Table.Columns)
                {
                    // || (column.Keys & DBColumnKeys.State) == DBColumnKeys.State
                    if ((column.Keys & DBColumnKeys.Access) == DBColumnKeys.Access
                        || (column.Keys & DBColumnKeys.Stamp) == DBColumnKeys.Stamp)
                    {
                        continue;
                    }
                    var value = GetValue(column);
                    if (value != null)
                    {
                        exist.SetValue(value, column);
                    }
                }

            }
            return exist;
        }

        public static DateTime GetDateVal(object val)
        {
            if (val == null)
                return DateTime.MinValue;
            if (val is DateTime)
                return (DateTime)val;
            return DateTime.Parse(val.ToString());
        }

        public DateTime GetDate(DBColumn column)
        {
            return GetDateVal(this[column]);
        }

        public DateTime GetDate(string column)
        {
            return GetDateVal(this[column]);
        }

        public void SetDate(DBColumn column, DateTime value)
        {
            this[column] = value;
        }

        public TimeSpan GetTimeSpan(DBColumn column)
        {
            object val = this[column];
            if (val == null)
                return new TimeSpan();
            if (val is TimeSpan)
                return (TimeSpan)val;
            return TimeSpan.Parse(val.ToString());
        }

        public void SetTimeSpan(DBColumn column, TimeSpan value)
        {
            this[column] = value;
        }

        public async Task<Stream> GetStream(DBTransaction transaction, int bufferSize = 81920)
        {
            if (Table.FileLOBKey != null && GetValue(table.FileLOBKey) != null)
            {
                return await GetLOB(Table.FileLOBKey, transaction);
            }
            else if (Table.FileKey != null)
            {
                return GetZipMemoryStream(table.FileKey, transaction, bufferSize);
            }
            return null;
        }

        public async Task SetStream(string filepath, DBColumn column, DBTransaction transaction, int bufferSize = 81920)
        {
            using (var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                await SetStream(stream, column, transaction, bufferSize);
            }
        }

        public async Task SetStream(Stream stream, DBColumn column, DBTransaction transaction, int bufferSize = 81920)
        {
            SetValue(await Helper.GetBytesAsync(stream), column);
            if (Attached)
            {
                await Save(transaction);
                SetValue(null, column, DBSetValueMode.Loading);
            }
        }

        public virtual MemoryStream GetMemoryStream(DBColumn column, DBTransaction transaction, int bufferSize = 81920)
        {
            OnGetStream(column, transaction);
            var memoryStream = (MemoryStream)null;
            var temp = GetValue<byte[]>(column);
            if (temp != null)
            {
                return new MemoryStream(temp) { Position = 0 };
            }
            memoryStream = new MemoryStream();
            Table.System.ReadSequential(this, column, memoryStream, transaction, bufferSize);
            return memoryStream;
        }

        public virtual FileStream GetFileStream(DBColumn column, string path, DBTransaction transaction, int bufferSize = 81920)
        {
            OnGetStream(column, transaction);
            var fileStream = File.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            var temp = GetValue<byte[]>(column);
            if (temp != null)
            {
                fileStream.Write(temp, 0, temp.Length);
                fileStream.Position = 0;
                return fileStream;
            }

            Table.System.ReadSequential(this, column, fileStream, transaction, bufferSize);
            return fileStream;
        }

        protected virtual void OnGetStream(DBColumn column, DBTransaction transaction)
        {
        }

        protected virtual void OnSetStream(DBColumn column, DBTransaction transaction)
        {
        }

        public Stream GetZipMemoryStream(DBColumn column, DBTransaction transaction, int bufferSize = 81920)
        {
            var memoryStream = GetMemoryStream(column, transaction, bufferSize);
            return Helper.IsGZip(memoryStream) ? Helper.GetGZipStrem(memoryStream) : memoryStream;
        }

        public FileStream GetZipFileStream(DBColumn column, string path, DBTransaction transaction, int bufferSize = 81920)
        {
            var fileStream = GetFileStream(column, path, transaction, bufferSize);
            if (Helper.IsGZip(fileStream))
            {
                using (var gzip = Helper.GetGZipStrem(fileStream))
                using (var newFileStream = File.Open(path + ".zip", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    gzip.CopyTo(newFileStream, bufferSize);
                }
                File.Delete(path);
                File.Move(path + ".zip", path);
                return File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            }
            return fileStream;
        }

        public byte[] GetZip(DBColumn column)
        {
            var data = GetValue<byte[]>(column);
            if (data != null && Helper.IsGZip(data))
                data = Helper.ReadGZip(data);
            return data;
        }

        public byte[] SetZip(DBColumn column, byte[] data)
        {
            byte[] temp = data != null && data.Length > 500 ? Helper.WriteGZip(data) : data;
            SetValue(temp, column);
            return temp;
        }

        public async Task SetLOB(Stream value, DBColumn column, DBTransaction transaction)
        {
            var oid = await Table.System.SetLOB(value, transaction);
            SetValueNullable<uint>(oid, column);
            await Save(transaction);
            OnSetStream(column, transaction);
        }

        public Task<Stream> GetLOB(DBTransaction transaction, int bufferSize = 81920)
        {
            return GetLOB(Table.FileLOBKey, transaction, bufferSize);
        }

        public virtual Task<Stream> GetLOB(DBColumn column, DBTransaction transaction, int bufferSize = 81920)
        {
            OnGetStream(column, transaction);
            var oid = GetValue<uint?>(column);
            if (oid == null)
                return null;
            return Table.System.GetLOB(oid.Value, transaction, bufferSize);
        }

        public async Task<FileStream> GetLOBFileStream(DBColumn column, string path, int bufferSize = 81920)
        {
            using (var transaction = new DBTransaction(Table.Connection))
            {
                return await GetLOBFileStream(column, path, transaction, bufferSize);
            }
        }

        public async Task<FileStream> GetLOBFileStream(DBColumn column, string path, DBTransaction transaction, int bufferSize = 81920)
        {
            using (var lobStream = await GetLOB(column, transaction))
            {
                if (lobStream == null)
                {
                    return null;
                }
                var fileStream = File.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);

                var buffer = new byte[bufferSize];
                int count;
                while ((count = lobStream.Read(buffer, 0, bufferSize)) != 0)
                {
                    fileStream.Write(buffer, 0, count);
                }
                fileStream.Position = 0;
                return fileStream;
            }

        }

        public LinkModel GetLink()
        {
            return new LinkModel
            {
                ProtocolLink = $"{ProtocolSetting.Current.Protocol}://{ProtocolSetting.Current.Host}/{GetType().Name}/{PrimaryId}",
                WebLink = $"http://{ProtocolSetting.Current.Host}/api/{GetType().Name}/{PrimaryId}",
            };
        }

    }
}

