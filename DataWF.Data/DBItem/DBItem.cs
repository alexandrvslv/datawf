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
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [ElementSerializer(typeof(DBItemSerializer))]
    [InvokerGenerator]
    public partial class DBItem : ICloneable, IComparable<DBItem>, IComparable, IDisposable, IAccessable, ICheck, INotifyPropertyChanged, INotifyPropertyChanging, IEditable, IStatusable, IPullHandler, IDBTableContent
    {
        public static readonly DBItem EmptyItem = new DBItem(null) { cacheToString = "Loading" };

        internal PullHandler? oldHandler;
        internal PullHandler handler;
        internal string cacheToString = string.Empty;
        protected DBTable table;
        protected DBItemState state = DBItemState.New;
        protected internal DBUpdateState update = DBUpdateState.Insert;
        private AccessValue access;

        //public DBItem()
        //{
        //    var table = DBService.GetTable(GetType());
        //    if (table != null)
        //        Build(table);
        //}

        public DBItem(IDBTable table)
        {
            if (table != null)
                Build(table);
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public virtual string AccessorName => ToString();

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public bool Loading
        {
            get => (state & DBItemState.Load) == DBItemState.Load;
            set
            {
                if (Loading != value)
                {
                    state = value ? state | DBItemState.Load : state & ~DBItemState.Load;
                    //OnPropertyChanged<bool>();
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public bool Attached => (state & DBItemState.Attached) == DBItemState.Attached;

        public ref readonly PullHandler GetRefHandler() => ref handler;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public PullHandler Handler { get => handler; }

        [XmlIgnore, JsonIgnore]
        public long HandlerValue => handler.Value;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public virtual string ParametersCategory
        {
            get => Table.CodeKey != null ? GetValue<string>(Table.CodeKey) : "General";
            set { }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public bool IsChanged
        {
            get => UpdateState != DBUpdateState.Default && (UpdateState & DBUpdateState.Commit) != DBUpdateState.Commit;
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public bool IsReferencingChanged
        {
            get { return IsChanged || GetPropertyReferencing().Any(p => p.IsReferencingChanged); }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public byte[] Image
        {
            get => Table.ImageKey == null ? null : GetValue(Table.ImageKey);
            set
            {
                if (Table.ImageKey == null)
                    return;
                SetValue(value, Table.ImageKey);
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public object PrimaryId
        {
            get => Table.PrimaryKey?.GetValue(this);
            set => SetValue(value, Table.PrimaryKey);
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public string PrimaryCode
        {
            get => GetValue(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
        }

        [Browsable(false), DefaultValue(0)]
        [Column("item_type", GroupName = "system", Keys = DBColumnKeys.ItemType, Order = 0)]// | DBColumnKeys.Indexing
        public virtual int ItemType
        {
            get => GetValue<int>(Table.ItemTypeKey);
            set => SetValue(value, Table.ItemTypeKey);
        }

        [DefaultValue(DBStatus.New), Column("status_id", GroupName = "system", Keys = DBColumnKeys.State | DBColumnKeys.Indexing, Order = 1000)]
        public DBStatus Status
        {
            get => GetValue(Table.StatusKey);
            set => SetValue(value, Table.StatusKey);
        }

        [Column("date_create", GroupName = "system", Keys = DBColumnKeys.Date | DBColumnKeys.System | DBColumnKeys.UtcDate, Order = 1001)]
        public virtual DateTime DateCreate
        {
            get => GetValue(Table.DateCreateKey);
            set => SetValue(value, Table.DateCreateKey);
        }

        [Browsable(false)]
        [Column("date_update", GroupName = "system", Keys = DBColumnKeys.Stamp | DBColumnKeys.NoLog | DBColumnKeys.System | DBColumnKeys.UtcDate, Order = 2)]
        public DateTime Stamp
        {
            get => GetValue(Table.StampKey);
            set => SetValue(value, Table.StampKey);
        }

        [Browsable(false), XmlIgnore, JsonIgnore]
        [Column("date_replicate", GroupName = "system", Keys = DBColumnKeys.ReplicateStamp | DBColumnKeys.NoLog | DBColumnKeys.NoReplicate | DBColumnKeys.System | DBColumnKeys.UtcDate, Order = 1003)]
        public DateTime? ReplicateStamp
        {
            get => GetValue(Table.ReplicateStampKey);
            set => SetValue(value, Table.ReplicateStampKey);
        }

        IAccessValue IAccessable.Access { get => Access; set => Access = (AccessValue)value; }

        [Column("group_access", 512, DataType = typeof(byte[]), GroupName = "system", Keys = DBColumnKeys.Access | DBColumnKeys.System, Order = 1004)]
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
        public virtual IDBSchema Schema
        {
            get => Table.Schema;
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public IDBTable Table
        {
            get => table;
            set
            {
                if (table != value)
                {
                    table = (DBTable)value.GetVirtualTable(GetType());
                    handler = table.GetNextHandler();
                }
            }
        }

        public object this[int columnIndex]
        {
            get => GetValue(Table.Columns[columnIndex]);
            set => SetValue(value, Table.Columns[columnIndex]);
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
                return row.GetValue(row.Table.ParseColumnProperty(code.Substring(pi)));
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
                row.SetValue(value, row.Table.ParseColumnProperty(code.Substring(pi)));
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

                SetValue(value, column, DBSetValueMode.Default);
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
                    OnPropertyChanged<DBUpdateState>();
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
                    OnPropertyChanged<bool>();
                }
            }
        }

        public bool GetOld(DBColumn column, out object value)
        {
            return column.GetOld(this, out value);
        }

        public bool GetOld<T>(DBColumn<T> column, out T value)
        {
            return column.GetOld(this, out value);
        }

        protected object GetPropertyValue(DBColumn column)
        {
            return column.PropertyInvoker != null ? column.PropertyInvoker.GetValue(this) : GetValue(column);
        }

        protected T GetPropertyValue<T>(DBColumn<T> column)
        {
            return column.PropertyInvoker is IValuedInvoker<T> valueInvoker ? valueInvoker.GetValue(this) : GetValue(column);
        }

        protected void SetPropertyValue(object value, DBColumn column, DBSetValueMode mode = DBSetValueMode.Default)
        {
            if (column.PropertyInvoker != null)
                column.PropertyInvoker.SetValue(this, value);
            else
                SetValue(value, column, mode);
        }

        protected void SetPropertyValue<T>(T value, DBColumn<T> column, DBSetValueMode mode = DBSetValueMode.Default)
        {
            if (column.PropertyInvoker is IValuedInvoker<T> valueInvoker)
                valueInvoker.SetValue(this, value);
            else
                SetValue(value, column, mode);
        }

        public object GetValue(DBColumn column)
        {
            return column.GetValue(this);
        }

        public T GetValue<T>(DBColumn column)
        {
            if (column is DBColumn<T> typedColumn)
                return typedColumn.GetValue(this);
            else
                return default(T);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetValue<T>(DBColumn<T> column)
        {
            return column.GetValue(this);
        }

        public T GetProperty<T>([CallerMemberName] string property = null)
        {
            return GetValue<T>((DBColumn<T>)Table.Columns.GetByProperty(property));
        }

        protected virtual internal void ClearBackup(bool accept)
        {
            if (oldHandler is PullHandler oldHandlerValue)
            {
                if (accept)
                {
                    FreeHandler(oldHandlerValue);
                }
                else
                {
                    foreach (var column in GetChangeKeys())
                    {
                        column.Reject(this);
                    }
                    var newHandler = handler;
                    handler = oldHandlerValue;
                    FreeHandler(newHandler);
                }
                oldHandler = null;
            }
        }

        protected virtual internal void Backup()
        {
            if (oldHandler == null && (UpdateState & DBUpdateState.Insert) == 0)
            {
                var newHandler = table.GetNextHandler();
                CopyTo(newHandler);
                //swap
                oldHandler = handler;
                handler = newHandler;
            }
        }

        internal void CheckState()
        {
            var temp = UpdateState;
            if (temp == DBUpdateState.Default || (temp & DBUpdateState.Commit) == DBUpdateState.Commit)
            {
                temp &= ~DBUpdateState.Commit;
                temp |= DBUpdateState.Update;
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
            SetValue<T>(value, (DBColumn<T>)Table.Columns.GetByProperty(property));
        }

        public void SetValue<T>(T value, DBColumn column, DBSetValueMode mode = DBSetValueMode.Default)
        {
            if (column is DBColumn<T> typedColumn)
            {
                SetValue(value, typedColumn, mode);
            }
            else throw new Exception($"Wrong Type {typeof(T)} for column {column}");
        }

        public void SetValue<T>(T value, DBColumn<T> column, DBSetValueMode mode = DBSetValueMode.Default)
        {
            column.SetValue(this, value, mode);
        }

        public void SetValue(object value, DBColumn column, DBSetValueMode mode = DBSetValueMode.Default)
        {
            column.SetValue(this, value, mode);
        }

        public bool IsChangedKey(DBColumn column)
        {
            return column.IsChanged(this);
        }

        public virtual bool GetIsChanged()
        {
            return GetChangeKeys().Any();
        }

        public IEnumerable<DBColumn> GetChangeKeys()
        {
            foreach (var column in Table.Columns)
            {
                if (column.IsChanged(this))
                {
                    yield return column;
                }
            }
        }

        public void SetValues(object[] values, DBSetValueMode mode = DBSetValueMode.Loading)
        {
            if (values == null)
                return;

            for (int i = 0; i < values.Length; i++)
            {
                var column = Table.Columns[i];

                if (column.ColumnType == DBColumnTypes.Default || column.ColumnType == DBColumnTypes.Query)
                {
                    SetValue(values[i], column, mode);
                }
            }
        }

        public void SetValues(Dictionary<string, object> values, DBSetValueMode mode = DBSetValueMode.Loading)
        {
            if (values == null)
                return;

            foreach (var kvp in values)
            {
                var column = Table.ParseColumn(kvp.Key);

                if (column != null)
                {
                    SetValue(kvp.Value, column, mode);
                }
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
            return GetReference<DBItem>(column, param);
        }

        public R GetReference<R>(DBColumn column, DBLoadParam param = DBLoadParam.Load | DBLoadParam.Referencing) where R : DBItem
        {
            return column?.GetReference<R>(this, param);
        }

        public R GetReference<R>(DBColumn column, ref R item, DBLoadParam param = DBLoadParam.Load | DBLoadParam.Referencing) where R : DBItem
        {
            return column?.GetReference<R>(this, ref item, param);
        }

        public R GetPropertyReference<R>(ref R item, [CallerMemberName] string property = null) where R : DBItem
        {
            return GetReference(Table.Columns.GetByReferenceProperty(property), ref item);
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

        public T SetPropertyReference<T>(T value, [CallerMemberName] string property = null) where T : DBItem
        {
            return SetReference(value, Table.Columns.GetByReferenceProperty(property));
        }

        public DBItem SetReference(DBItem value, string column)
        {
            return SetReference<DBItem>(value, Table.Columns[column]);
        }

        public DBItem SetReference(DBItem value, DBColumn column)
        {
            return SetReference<DBItem>(value, column);
        }

        public R SetReference<R>(R value, DBColumn column) where R : DBItem
        {
            column.SetReference<R>(this, value);
            return value;
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

        public void SetReferencing<T>(IEnumerable<T> items, DBColumn column) where T : DBItem
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

        public void SetReferencing<T>(IEnumerable<T> items, string property) where T : DBItem
        {
            var table = Schema.GetTable<T>();
            SetReferencing<T>(items, table.ParseProperty(property));
        }

        public IEnumerable<T> GetReferencing<T>(DBTable<T> table, QQuery query, DBLoadParam param) where T : DBItem
        {
            //query.TypeFilter = typeof(T);
            if ((param & DBLoadParam.Load) == DBLoadParam.Load)
            {
                table.Load(query, param);
            }
            return table.Select(query);
        }

        public IEnumerable<T> GetReferencing<T>(string property, DBLoadParam param) where T : DBItem
        {
            var table = Schema.GetTable<T>();
            return GetReferencing<T>(table, table.ParseProperty(property), param);
        }

        public IEnumerable<T> GetReferencing<T>(DBColumn column, DBLoadParam param) where T : DBItem
        {
            return GetReferencing<T>((DBTable<T>)column.Table.GetVirtualTable(typeof(T)), column, param);
        }

        public IEnumerable<T> GetReferencing<T>(DBTable<T> table, DBColumn column, DBLoadParam param) where T : DBItem
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

        public IEnumerable<T> GetReferencing<T>(string tableCode, string columnCode, DBLoadParam param) where T : DBItem
        {
            var table = (DBTable<T>)Schema.ParseTable(tableCode);
            return table != null ? GetReferencing<T>(table, table.ParseColumn(columnCode), param) : null;
        }

        public IEnumerable<T> GetReferencing<T>(DBForeignKey relation, DBLoadParam param) where T : DBItem
        {
            return GetReferencing<T>((DBTable<T>)relation.Table, relation.Column, param);
        }

        public IEnumerable<DBItem> GetReferencing(DBColumn column, DBLoadParam param)
        {
            return GetReferencing(column.Table, column, param);
        }

        public IEnumerable<DBItem> GetReferencing(IDBTable table, DBColumn column, DBLoadParam param)
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

        public IEnumerable<DBItem> GetReferencing(IDBTable table, QQuery query, DBLoadParam param)
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
            return column.FormatDisplay(this);
        }

        public string Format(string code)
        {
            return Format(Table.Columns[code]);
        }

        public void Build(IDBTable table, bool setDefauilts = true, int itemType = -1)
        {
            Table = table;
            if (setDefauilts)
            {
                SetDefaults();
            }
            if (Table.ItemTypeKey != null)
            {
                SetValue(itemType < 0 ? table.GetTypeIndex(GetType()) : itemType, table.ItemTypeKey, DBSetValueMode.Loading);
            }
        }

        public void SetDefaults()
        {
            foreach (DBColumn column in Table.Columns)
            {
                if (!string.IsNullOrEmpty(column.DefaultValue))
                    SetValue((object)column.DefaultValue, column, DBSetValueMode.Loading);
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
                var temp = cultures.Split(QQuery.CommaSeparator, StringSplitOptions.RemoveEmptyEntries);
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
            foreach (DBColumn<string> column in Table.Columns.GetByGroup(@group))
            {
                if (column.Culture.ThreeLetterISOLanguageName == culture.ThreeLetterISOLanguageName)
                {
                    SetValue(value, column);
                    break;
                }
            }
        }

        private AccessValue ReadAccess()
        {
            var accessData = Table.AccessKey != null ? GetValue(Table.AccessKey) : null;
            return accessData != null ? new AccessValue(accessData) { Owner = this } : null;
        }

        public virtual void Accept(IUserIdentity user)
        {
            var oldState = UpdateState;
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
                ClearBackup(true);
            }
            OnAccepted(user, oldState);
        }

        protected virtual void OnAccepted(IUserIdentity user, DBUpdateState oldState)
        {
            DBService.OnAccept(new DBItemEventArgs(this, null, user) { State = oldState });
        }

        public void Reject(string column)
        {
            Reject(Table.Columns[column]);
        }

        public void Reject(DBColumn column)
        {
            column.Reject(this);
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
                    ClearBackup(false);
                    UpdateState = DBUpdateState.Default;
                }
                DBService.OnReject(new DBItemEventArgs(this, null, user));
            }
        }

        #region ICloneable Members

        public virtual object Clone()
        {
            var item = Table.NewItem(DBUpdateState.Insert, false, ItemType);
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
            CopyTo(item.handler);
        }

        public void CopyTo(PullHandler hindex)
        {
            foreach (var column in Table.Columns)
            {
                if (column.Pull != null)
                {
                    column.Copy(this.handler, hindex);
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
            OnPropertyChanged<bool>(nameof(Attached), null, Attached);
            //DBService.OnAdded(this);
        }

        public virtual void OnDetached()
        {
            if (!Attached)
                return;
            State &= ~DBItemState.Attached;
            OnPropertyChanged<bool>(nameof(Attached), null, Attached);
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

        protected internal void OnPropertyChanging<V>(string property, DBColumn<V> column = null, V value = default(V))
        {
            if (Attached)
            {
                Table.OnItemChanging<V>(this, property, column, value);
            }
            RaisePropertyChanging(property);
        }

        protected virtual void RaisePropertyChanging(string property)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(property));
        }

        protected internal void OnPropertyChanged<V>([CallerMemberName] string property = null, DBColumn<V> column = null, V value = default(V))
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
            if (Table.Sequence == null || Table.PrimaryKey == null || UpdateState != DBUpdateState.Insert)
                return;
            if (Table.PrimaryKey.IsEmpty(this))
            {
                Table.PrimaryKey.SetId(this, Table.GenerateId(transaction));
            }
        }

        public void Free()
        {
            Detach();
            FreeHandler(handler);
        }

        private void FreeHandler(PullHandler handler)
        {
            foreach (DBColumn column in Table.Columns)
            {
                if (column.Pull != null)
                    column.Clear(handler);
            }
            table.FreeHandlers.Enqueue(handler);
        }

        public Task Save()
        {
            return Save((IUserIdentity)null);
        }

        public async Task Save(IUserIdentity user)
        {
            using (var transaction = new DBTransaction(Table, user))
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
            if (transaction.AddItem(this, true))
            {
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
            if (obj == null)
                return 1;
            var index = table.index.CompareTo(obj.table.index);
            return index == 0 ? handler.CompareTo(in obj.handler) : index;
        }

        public string FormatPatch()
        {
            var rez = new StringBuilder();
            rez.AppendLine($"if exists(select * from {Table.Name} where {Table.PrimaryKey.SqlName}={Table.PrimaryKey.FormatQuery(this)})");
            rez.AppendLine("    " + Table.System.FormatCommand(table, DBCommandTypes.Update, this) + ";");
            rez.AppendLine("else");
            rez.AppendLine("    " + Table.System.FormatCommand(table, DBCommandTypes.Insert, this) + ";");
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
                string value = column.FormatDisplay(this);
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
            using (DBTransaction transaction = new DBTransaction(Table))
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
                if (relation.Table is IDBTableLog
                    || relation.Table.IsVirtual
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
            using (var transaction = new DBTransaction(Table))
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
                    if (column.IsEmpty(this))
                    {
                        column.Copy(item, this);
                    }
                }

                foreach (DBForeignKey relation in relations)
                {
                    if (relation.Table.Type == DBTableType.Table)
                    {
                        var referencing = item.GetReferencing(relation, DBLoadParam.Load | DBLoadParam.Referencing).ToList();
                        if (referencing.Count > 0)
                        {
                            var primaryKey = Table.PrimaryKey;
                            foreach (DBItem subItem in referencing)
                            {
                                relation.Column.Copy(this, primaryKey, subItem);
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

        public Task SaveOrUpdate(IUserIdentity user, DBLoadParam param = DBLoadParam.None)
        {
            var exist = FindAndUpdate(param);
            if (exist != null)
            {
                return exist.Save(user);
            }
            else
            {
                return Save(user);
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
                        column.Copy(item, item.Table.PrimaryKey, this);
                    }
                }
            }
        }

        public IEnumerable<DBItem> GetPropertyReferenced()
        {
            foreach (var column in Table.Columns.GetIsReference())
            {
                var invoker = column.ReferencePropertyInvoker;
                if (column.ColumnType == DBColumnTypes.Default
                    && invoker != null
                    && invoker.TargetType.IsAssignableFrom(GetType()))
                {
                    var item = invoker.GetValue(this) as DBItem;

                    if (item != null && item != this)
                    {
                        yield return item;
                    }
                }
            }
        }

        public IEnumerable<DBItem> GetPropertyReferencing()
        {
            foreach (var referencing in Table.GetPropertyReferencing(GetType()))
            {
                var references = (IEnumerable<DBItem>)referencing.PropertyInvoker.GetValue(this);
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
                if (relation.Table != Table && !relation.Table.IsVirtual)
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

        public DBItem AttachOrUpdate(DBLoadParam param = DBLoadParam.None)
        {
            var exist = FindAndUpdate(param);
            if (exist == null)
            {
                Attach();
            }
            else
            {
                Free();
            }
            return exist ?? this;
        }

        public DBItem FindAndUpdate(DBLoadParam param = DBLoadParam.None)
        {
            var exist = (Table.PrimaryKey?.IsEmpty(this) ?? true) ? null : Table.PrimaryKey.LoadByKey(this, param);
            if (exist != null)
            {
                foreach (var column in GetChangeKeys())
                {
                    if (column == Table.PrimaryKey
                        || column == Table.ItemTypeKey)
                        continue;
                    column.Copy(this, exist);
                }
            }
            return exist;
        }

        public DateTime GetDate(string column)
        {
            return GetDate(Table.ParseColumnProperty(column));
        }

        public DateTime GetDate(DBColumn column)
        {
            if (column is DBColumn<DateTime> dateColumn)
                return dateColumn.GetValue(this);
            if (column is DBColumn<DateTime?> nDateColumn)
                return nDateColumn.GetValue(this) ?? DateTime.MinValue;
            if (column is DBColumn<string> stringColumn)
                return DateTime.Parse(stringColumn.GetValue(this));
            else
                throw new Exception("Unsupported Column type");
        }

        public void SetDate(DBColumn column, DateTime value)
        {
            if (column is DBColumn<DateTime> dateColumn)
                SetValue(value, dateColumn);
            if (column is DBColumn<DateTime?> nDateColumn)
                SetValue<DateTime?>(value, nDateColumn);
            if (column is DBColumn<string> stringColumn)
                SetValue(value.ToString("u"), stringColumn);
            else
                throw new Exception("Unsupported Column type");
        }

        public TimeSpan GetTimeSpan(DBColumn column)
        {
            if (column is DBColumn<TimeSpan> dateColumn)
                return dateColumn.GetValue(this);
            if (column is DBColumn<TimeSpan?> nDateColumn)
                return nDateColumn.GetValue(this) ?? TimeSpan.MinValue;
            if (column is DBColumn<string> stringColumn)
                return TimeSpan.Parse(stringColumn.GetValue(this));
            else
                throw new Exception("Unsupported Column type");
        }

        public void SetTimeSpan(DBColumn column, TimeSpan value)
        {
            if (column is DBColumn<TimeSpan> dateColumn)
                SetValue(value, dateColumn);
            if (column is DBColumn<TimeSpan?> nDateColumn)
                SetValue<TimeSpan?>(value, nDateColumn);
            if (column is DBColumn<string> stringColumn)
                SetValue(value.ToString(), stringColumn);
            else
                throw new Exception("Unsupported Column type");
        }

        public async Task<Stream> GetStream(DBTransaction transaction, int bufferSize = 81920)
        {
            if (!(Table.FileOIDKey?.IsEmpty(this) ?? true))
            {
                return await GetBlob(Table.FileOIDKey, transaction);
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
            SetValue(await Helper.GetBufferedBytesAsync(stream), column);
            await Save(transaction);
            SetValue(null, column, DBSetValueMode.Loading);
            await OnSetStream(column, transaction);
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

        protected virtual Task OnSetStream(DBColumn column, DBTransaction transaction)
        {
            return Task.CompletedTask;
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
                using (var newFileStream = File.Open(path + ".unzip", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    gzip.CopyTo(newFileStream, bufferSize);
                }
                File.Delete(path);
                File.Move(path + ".unzip", path);
                return File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            }
            return fileStream;
        }

        public ArraySegment<byte>? GetZip(DBColumn column)
        {
            var data = GetValue<byte[]>(column);
            if (data != null)
            {
                var bytes = new ArraySegment<byte>(data);
                return Helper.IsGZip(data) ? Helper.ReadGZip(bytes) : bytes;
            }
            return null;
        }

        public ArraySegment<byte>? SetZip(DBColumn column, byte[] data)
        {
            var temp = data != null && data.Length > 500
                ? Helper.WriteGZip(new ArraySegment<byte>(data))
                : new ArraySegment<byte>(data);
            SetValue(temp, column);
            return temp;
        }

        public Task<long> SetBlob(Stream value, DBTransaction transaction)
        {
            return SetBlob(value, Table.FileOIDKey, transaction);
        }

        public async Task<long> SetBlob(Stream value, DBColumn<long?> column, DBTransaction transaction)
        {
            var id = await Table.System.SetBlob(value, transaction);
            SetValue<long?>(id, column);
            await Save(transaction);
            await OnSetStream(column, transaction);
            return id;
        }

        public Task<Stream> GetBlob(DBTransaction transaction, int bufferSize = 81920)
        {
            return GetBlob(Table.FileOIDKey, transaction, bufferSize);
        }

        public virtual Task<Stream> GetBlob(DBColumn<long?> column, DBTransaction transaction, int bufferSize = 81920)
        {
            OnGetStream(column, transaction);
            var oid = GetValue<long?>(column);
            if (oid == null)
                return null;
            if (transaction.Schema == null)
            {
                transaction.Schema = Schema;
            }
            return Table.System.GetBlob(oid.Value, transaction, bufferSize);
        }

        public async Task<FileStream> GetBlobFileStream(DBColumn<long?> column, string path, int bufferSize = 81920)
        {
            using (var transaction = new DBTransaction(Table))
            {
                return await GetBlobFileStream(column, path, transaction, bufferSize);
            }
        }

        public async Task<FileStream> GetBlobFileStream(DBColumn<long?> column, string path, DBTransaction transaction, int bufferSize = 81920)
        {
            using (var lobStream = await GetBlob(column, transaction))
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
                ProtocolLink = $"{ProtocolSetting.Current.Protocol}://{ProtocolSetting.Current.Host}/{GetType().Name}/{Table.PrimaryKey.FormatDisplay(this)}",
                WebLink = $"{ProtocolSetting.Current.WebProtocol}://{ProtocolSetting.Current.Host}/api/{GetType().Name}/{Table.PrimaryKey.FormatDisplay(this)}",
            };
        }

        internal int CompareByHandler(DBItem y)
        {
            ref readonly var xHandler = ref handler;
            ref readonly var yHandler = ref y.handler;
            return xHandler.Value.CompareTo(yHandler.Value);
        }
    }
}

