/*
 DBRow.cs
 
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
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

namespace DataWF.Data
{

    [DataContract]
    //[JsonConverter(typeof(DBItemJsonConverter))]
    public class DBItem : ICloneable, IComparable<DBItem>, IDisposable, IAccessable, ICheck, INotifyPropertyChanged, INotifyPropertyChanging, IEditable, IStatusable, IDBTableContent, IPullHandler
    {
        public static readonly DBItem EmptyItem = new DBItem() { cacheToString = "Loading" };

        public static DBTable<T> GetTable<T>() where T : DBItem, new()
        {
            return DBTable.GetTable<T>();
        }

        public object Tag;
        internal int handler = -1;
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

        //public object GetTag(DBColumn column)
        //{
        //    return column.GetTag(handler);
        //}

        //public void SetTag(DBColumn column, object value)
        //{
        //    if (value == null)
        //    {
        //        column.RemoveTag(handler);
        //    }
        //    else
        //    {
        //        column.SetTag(handler, value);
        //    }
        //}

        //protected virtual internal void RemoveTag()
        //{
        //    access = null;
        //    foreach (DBColumn column in Table.Columns)
        //    {
        //        object o = GetTag(column);
        //        if (o != null)
        //        {
        //            SetTag(column, null);
        //        }
        //    }
        //}

        //public object GetCache(string column)
        //{
        //    return GetCache(Table.Columns[column]);
        //}

        //public virtual object GetCache(DBColumn column)
        //{
        //    if (column == null)
        //        return null;

        //    return GetTag(column);
        //}

        //public void SetCache(string column, object Value)
        //{
        //    SetCache(Table.Columns[column], Value);
        //}

        //public void SetCache(DBColumn Column, object Value)
        //{
        //    if (Column != null)
        //        SetTag(Column, Value);
        //}

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

        public object GetValue(DBColumn column)
        {
            return column.GetValue(this);
        }

        public T GetValue<T>(DBColumn column)
        {
            return column.GetValue<T>(this);
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

        private void CheckState()
        {
            var temp = UpdateState;
            if (temp == DBUpdateState.Default || (temp & DBUpdateState.Commit) == DBUpdateState.Commit)
            {
                temp &= ~DBUpdateState.Commit;
                temp |= DBUpdateState.Update;
                if (DBTransaction.Current != null)
                {
                    DBTransaction.Current.Rows.Add(this);
                }
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

        //public void SetProperty(object value, [CallerMemberName] string property = null)
        //{
        //    SetValue(value, Table.Columns.GetByProperty(property));
        //}

        public void SetValue<T>(T value, DBColumn column)
        {
            SetValue<T>(value, column, column.ColumnType == DBColumnTypes.Default);
        }

        public void SetValue(object value, DBColumn column)
        {
            SetValue(value, column, column.ColumnType == DBColumnTypes.Default);
        }

        public void SetValue<T>(T value, DBColumn column, bool check, object tag = null)
        {
            //SetTag(column, tag);

            var field = column.GetValue<T>(this);

            if (DBService.Equal<T>(field, value))
            {
                return;
            }
            if (check)
            {
                RefreshOld(column, value, field);
            }

            OnPropertyChanging(column.Property ?? column.Name, column, field);

            column.SetValue<T>(this, value);

            OnPropertyChanged(column.Property ?? column.Name, column, value);

            if (check)
            {
                CheckState();
            }
        }

        public void SetValue(object value, DBColumn column, bool check, object tag = null)
        {
            //SetTag(column, tag);

            var field = column.GetValue(this);

            if (DBService.Equal(field, value))
            {
                return;
            }
            if (check)
            {
                RefreshOld(column, value, field);
            }

            OnPropertyChanging(column.Property ?? column.Name, column, field);

            column.SetValue(this, value);

            OnPropertyChanged(column.Property ?? column.Name, column, value);

            if (check)
            {
                CheckState();
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
                    SetValue(values[i], Column, false);
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
                    SetValue(kvp.Value, column, false);
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

        public DBItem GetRef(string code, DBLoadParam param = DBLoadParam.Load)
        {
            DBItem row = this;
            int pi = 0, i = code.IndexOf('.');
            while (i > 0)
            {
                var item = row.GetRef(row.Table.Columns[code.Substring(pi, i - pi)], param);
                if (item == null)
                    return null;
                row = item;
                pi = i + 1;
                i = code.IndexOf('.', pi);
            }
            return row.GetRef(row.Table.Columns[code.Substring(pi)], param);
        }

        public DBItem GetRef(DBColumn column, DBLoadParam param = DBLoadParam.Load)
        {
            if (column == null)
                return null;
            if (!column.IsReference)
                return null;
            if (column.IsPrimaryKey)
                return this;
            object value = GetValue(column);
            if (value == null)
                return null;
            return column.ReferenceTable.LoadItemById(value, param);
        }

        public DBItem GetReference(string code, ref DBItem item, DBLoadParam param = DBLoadParam.Load)
        {
            if (item != null)
                return item;
            return item = GetRef(code, param);
        }

        public DBItem GetReference(DBColumn column, ref DBItem item, DBLoadParam param = DBLoadParam.Load)
        {
            object value = GetValue(column);
            if (item?.PrimaryId == value)
                return item;

            return item = value == null ? null : column.ReferenceTable.LoadItemById(value, param);
        }

        public T GetReference<T>(DBColumn column, ref T item, DBLoadParam param = DBLoadParam.Load) where T : DBItem
        {
            object value = GetValue(column);
            if (item?.PrimaryId == value)
                return item;

            return item = value == null ? (T)null : (T)column.ReferenceTable.LoadItemById(value, param);
        }

        public T GetPropertyReference<T>(ref T item, [CallerMemberName] string property = null) where T : DBItem
        {
            var column = Table.Foreigns.GetByProperty(property)?.Column;
            return GetReference(column, ref item);
        }

        //public T GetReference<T>(string code, DBLoadParam param = DBLoadParam.Load) where T : DBItem, new()
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

        public T GetRef<T>(DBColumn column, DBLoadParam param = DBLoadParam.Load) where T : DBItem
        {
            return (T)GetRef(column, param);
        }

        public DBItem SetReference(DBItem value, string column)
        {
            return SetReference(value, Table.Columns[column]);
        }

        public T SetPropertyReference<T>(T value, [CallerMemberName] string property = null) where T : DBItem
        {
            return SetReference(value, Table.Foreigns.GetByProperty(property)?.Column);
        }

        public DBItem SetReference(DBItem value, DBColumn column)
        {
            SetValue(value?.PrimaryId, column, column.ColumnType == DBColumnTypes.Default, value);
            return value;
        }

        public T SetReference<T>(T value, DBColumn column) where T : DBItem
        {
            SetValue(value?.PrimaryId, column, column.ColumnType == DBColumnTypes.Default, value);
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

        public void Delete()
        {
            if ((UpdateState & DBUpdateState.Insert) == DBUpdateState.Insert)
            {
                Detach();
            }
            else
            {
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
                return table.Load(query);
            }
            return table.Select(query);
        }

        public IEnumerable<T> GetReferencing<T>(string property, DBLoadParam param) where T : DBItem, new()
        {
            var table = DBTable.GetTable<T>();
            return GetReferencing<T>(table, table.ParseProperty(property), param);
        }

        public IEnumerable<T> GetReferencing<T>(DBTable<T> table, DBColumn column, DBLoadParam param) where T : DBItem, new()
        {
            using (var query = new QQuery("", table))
            {
                query.BuildParam(column, CompareType.Equal, PrimaryId);
                return GetReferencing<T>(table, query, param);
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
            using (var query = new QQuery("", table))
            {
                query.BuildParam(column, CompareType.Equal, PrimaryId);
                return GetReferencing(query, param);
            }
        }

        public IEnumerable<DBItem> GetReferencing(QQuery query, DBLoadParam param)
        {
            if ((param & DBLoadParam.Load) == DBLoadParam.Load)
            {
                return query.Load();
            }

            return query.Select();
        }

        public IEnumerable<DBItem> GetReferencing(DBForeignKey relation, DBLoadParam param)
        {
            return GetReferencing(relation.Table, relation.Column, param);
        }

        public override string ToString()
        {
            if (cacheToString.Length == 0)
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

        public void Build(DBTable table, bool setDefauilts = true)
        {
            Table = table;
            if (setDefauilts)
            {
                SetDefaults();
            }
            if (Table.ItemTypeKey != null)
            {
                SetValue<int?>(table.GetTypeIndex(GetType()), table.ItemTypeKey, false);
            }
        }

        public void SetDefaults()
        {
            foreach (DBColumn column in Table.Columns)
            {
                if (!string.IsNullOrEmpty(column.DefaultValue))
                    SetValue(column.ParseValue(column.DefaultValue), column, false);
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

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public object PrimaryId
        {
            get { return Table.PrimaryKey == null ? null : GetValue(Table.PrimaryKey); }
            set { this[Table.PrimaryKey] = value; }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public string PrimaryCode
        {
            get { return Table.CodeKey == null ? null : GetValue<string>(Table.CodeKey); }
            set { this[Table.CodeKey] = value; }
        }

        [Browsable(false)]
        [DataMember]
        [DefaultValue(0)]
        [Column("item_type", GroupName = "system", Keys = DBColumnKeys.ItemType | DBColumnKeys.System, Order = 0)]
        public virtual int? ItemType
        {
            get { return Table.ItemTypeKey == null ? 0 : GetValue<int?>(Table.ItemTypeKey); }
            set { SetValue(value, Table.ItemTypeKey); }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        DBStatus IStatusable.Status { get => Status ?? DBStatus.Empty; set => Status = value; }

        [DataMember, DefaultValue(DBStatus.New), Column("status_id", GroupName = "system", Keys = DBColumnKeys.State | DBColumnKeys.System | DBColumnKeys.Indexing, Order = 99)]
        public DBStatus? Status
        {
            get { return Table.StatusKey == null ? DBStatus.Empty : GetValue<DBStatus?>(Table.StatusKey).GetValueOrDefault(); }
            set { SetValue(value, Table.StatusKey); }
        }

        [DataMember, Column("date_create", GroupName = "system", Keys = DBColumnKeys.Date | DBColumnKeys.System, Order = 100)]
        public DateTime? DateCreate
        {
            get { return Table.DateKey == null ? null : GetValue<DateTime?>(Table.DateKey); }
            set { SetValue(value, Table.DateKey); }
        }

        [Browsable(false)]
        [DataMember, Column("date_update", GroupName = "system", Keys = DBColumnKeys.Stamp | DBColumnKeys.NoLog | DBColumnKeys.System, Order = 101)]
        public DateTime? Stamp
        {
            get { return Table.StampKey == null ? null : GetValue<DateTime?>(Table.StampKey); }
            set { SetValue(value, Table.StampKey); }
        }

        IAccessValue IAccessable.Access { get => Access; set => Access = (AccessValue)value; }

        [XmlIgnore, JsonIgnore, NotMapped, Browsable(false)]
        [DataMember, Column("group_access", 512, DataType = typeof(byte[]), GroupName = "system", Keys = DBColumnKeys.Access | DBColumnKeys.System, Order = 102)]
        public virtual AccessValue Access
        {
            get
            {
                if (access == null)
                {
                    if (Table.AccessKey != null)
                    {
                        access = ReadAccess();
                    }
                    if (access == null)
                    {
                        return Table.Access;
                    }
                }
                return access;
            }
            set
            {
                if (Table.AccessKey != null)
                {
                    SetValue(value?.Write(), Table.AccessKey);
                }
                access = value;
            }
        }

        private AccessValue ReadAccess()
        {
            var accessData = GetValue<byte[]>(Table.AccessKey);
            return accessData != null ? new AccessValue(accessData) : null;
        }

        //[DataMember, Browsable(false)]
        //public virtual string Name
        //{
        //    get { return GetName(nameof(Name)); }
        //    set { SetName(nameof(Name), value); }
        //}

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public virtual DBTable Table
        {
            get { return table; }
            set
            {
                if (table != value)
                {
                    table = value is IDBVirtualTable virtualTable ? virtualTable.BaseTable : value;
                    handler = table.GetNextHandler();
                }
            }
        }

        public object this[int columnIndex]
        {
            get { return this[Table.Columns[columnIndex]]; }
            set { this[Table.Columns[columnIndex]] = value; }
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
                    var item = row.GetRef(scolumn);
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
                    var item = row.GetRef(row.Table.ParseColumnProperty(code.Substring(pi, i - pi)));
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
                         column.ColumnType == DBColumnTypes.Default,
                         value as DBItem);
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBItemState State
        {
            get { return state; }
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
            get { return update; }
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
                            SetValue(old, column, false);
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

        [Browsable(false)]
        public bool Attached
        {
            get { return (state & DBItemState.Attached) == DBItemState.Attached; }
        }

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

        public virtual void OnPropertyChanging(string property, DBColumn column = null, object value = null)
        {
            if (Attached)
            {
                Table.OnItemChanging(this, property, column, value);
            }
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(property));
        }

        public virtual void OnPropertyChanged([CallerMemberName]string property = null, DBColumn column = null, object value = null)
        {
            if (column != null && (column.Keys & DBColumnKeys.View) == DBColumnKeys.View)
            {
                cacheToString = string.Empty;
            }
            //if (string.IsNullOrEmpty(property))
            //{
            //    RemoveTag();
            //}
            if (Attached)
            {
                Table.OnItemChanged(this, property, column, value);
                if (property == nameof(Access) && access != null)
                {
                    access = ReadAccess();
                }
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        #endregion

        public void Refresh(IUserIdentity user)
        {
            Reject(user);
            Table.ReloadItem(PrimaryId);
        }

        public void GenerateId()
        {
            if (Table.Sequence == null || Table.PrimaryKey == null)
                return;
            if (PrimaryId == null)
            {
                PrimaryId = Table.Sequence.Next();
            }
            else
            {
                Table.Sequence.SetCurrent(PrimaryId);
            }
        }

        public void Clear()
        {
            Detach();
            foreach (DBColumn column in Table.Columns)
            {
                column.SetValue(this, null);
            }
            Table.FreeHandlers.Enqueue(handler);
        }

        [Browsable(false)]
        public bool IsChanged
        {
            get { return UpdateState != DBUpdateState.Default && (UpdateState & DBUpdateState.Commit) != DBUpdateState.Commit; }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public byte[] Image
        {
            get { return this[Table.ImageKey] as byte[]; }
            set
            {
                if (Table.ImageKey == null)
                    return;
                this[Table.ImageKey] = value;
            }
        }

        public virtual void Save(IUserIdentity user = null)
        {
            if (OnSaving(user))
            {
                Table.SaveItem(this, user);
                OnSaved(user);
            }
        }

        protected virtual void OnSaved(IUserIdentity user)
        {
        }

        protected virtual bool OnSaving(IUserIdentity user)
        {
            return true;
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as DBItem);
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public bool Check
        {
            get { return (state & DBItemState.Check) == DBItemState.Check; }
            set
            {
                if (Check != value)
                {
                    state = value ? state | DBItemState.Check : state & ~DBItemState.Check;
                    OnPropertyChanged();
                }
            }
        }

        public int Handler { get => handler; set => handler = value; }

        public int CompareTo(DBItem obj)
        {
            return obj == null ? 1 : obj.Table == Table ? handler.CompareTo(obj.handler) :
                GetHashCode().CompareTo(obj.GetHashCode());
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

        public void Delete(int recurs, DBLoadParam param = DBLoadParam.None, IUserIdentity user = null)
        {
            var dependencies = GetChilds(recurs, param).ToList();
            var owner = user ?? saveLock;
            var transaction = DBTransaction.GetTransaction(owner, Table.Schema.Connection);
            try
            {
                foreach (var item in dependencies)
                {
                    item.Delete();
                    if (item.Attached)
                    {
                        item.Save(user);
                    }
                }
                Delete();
                if (Attached)
                {
                    Save(user);
                }
                if (transaction.Owner == owner)
                {
                    transaction.Commit(user);
                }
            }
            catch (Exception ex)//TODO If Timeout Expired
            {
                Helper.OnException(ex);
                if (transaction.Owner == owner)
                {
                    transaction.Rollback(user);
                }
            }
            finally
            {
                if (transaction.Owner == owner)
                {
                    transaction.Dispose();
                }
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
                if (relation.Table is DBLogTable
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

        public void Merge(IEnumerable<DBItem> list)
        {
            var relations = Table.GetChildRelations().ToList();
            var rows = new List<DBItem> { this };
            foreach (DBItem item in list)
            {
                if (item == this)
                    continue;
                rows.Add(item);

                item.UpdateState |= DBUpdateState.Delete;
                foreach (DBColumn column in item.Table.Columns)
                {
                    if (this[column] == DBNull.Value && item[column] != DBNull.Value)
                        this[column] = item[column];
                }

                foreach (DBForeignKey relation in relations)
                    if (relation.Table.Type == DBTableType.Table)
                    {
                        var refings = item.GetReferencing(relation, DBLoadParam.Load | DBLoadParam.Synchronize).ToList();
                        if (refings.Count > 0)
                        {
                            foreach (DBItem refing in refings)
                                refing[relation.Column] = PrimaryId;

                            relation.Table.Save(refings);
                        }
                    }
            }

            Table.Save(rows);
        }

        public void SaveOrUpdate(DBLoadParam param = DBLoadParam.None, IUserIdentity user = null)
        {
            var exist = FindAndUpdate(param);
            if (exist != null)
            {
                exist.Save(user);
            }
            else
            {
                Save(user);
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
                            transaction.Rows.Add(reference);
                    }
                }
            }
        }

        public void SaveReferencing()
        {
            foreach (var relation in Table.GetChildRelations())
            {
                if (relation.Table != Table && !(relation.Table is IDBVirtualTable))
                {
                    var references = GetReferencing(relation, DBLoadParam.None);
                    var updatind = new List<DBItem>();
                    foreach (DBItem reference in references)
                    {
                        if (reference.IsChanged)
                            updatind.Add(reference);
                    }

                    if (updatind.Count > 0)
                        relation.Table.Save(updatind);
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

        public void SetStream(string filepath, DBColumn column, IUserIdentity user, int bufferSize = 81920)
        {
            using (var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                SetStream(stream, column, user, bufferSize);
            }
        }

        public void SetStream(Stream stream, DBColumn column, IUserIdentity user, int bufferSize = 81920)
        {
            SetValue(Helper.GetBytes(stream), column);
            if (Attached)
            {
                Save(user);
                SetValue(null, column, false);
            }
        }

        public MemoryStream GetMemoryStream(DBColumn column, int bufferSize = 81920)
        {
            var memoryStream = (MemoryStream)null;
            var temp = GetValue<byte[]>(column);
            if (temp != null)
            {
                return new MemoryStream(temp) { Position = 0 };
            }
            memoryStream = new MemoryStream();
            Table.System.ReadSequential(this, column, memoryStream, bufferSize);
            return memoryStream;
        }

        public FileStream GetFileStream(DBColumn column, string path, int bufferSize = 81920)
        {
            var fileStream = File.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            var temp = GetValue<byte[]>(column);
            if (temp != null)
            {
                fileStream.Write(temp, 0, temp.Length);
                fileStream.Position = 0;
                return fileStream;
            }

            Table.System.ReadSequential(this, column, fileStream, bufferSize);
            return fileStream;
        }

        public Stream GetZipMemoryStream(DBColumn column, int bufferSize = 81920)
        {
            var memoryStream = GetMemoryStream(column, bufferSize);
            return Helper.IsGZip(memoryStream) ? Helper.GetGZipStrem(memoryStream) : memoryStream;
        }

        public FileStream GetZipFileStream(DBColumn column, string path, int bufferSize = 81920)
        {
            var fileStream = GetFileStream(column, path, bufferSize);
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

    }
}

