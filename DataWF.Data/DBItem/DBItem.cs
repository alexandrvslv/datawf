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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

namespace DataWF.Data
{

    [DataContract]
    //[JsonConverter(typeof(DBItemJsonConverter))]
    public class DBItem : ICloneable, IComparable<DBItem>, IDisposable, IAccessable, ICheck, INotifyPropertyChanged, IEditable, IStatusable, IDBTableContent
    {
        public static readonly DBItem EmptyItem = new DBItem() { cacheToString = "Loading" };

        public static DBTable<T> GetTable<T>() where T : DBItem, new()
        {
            return DBTable.GetTable<T>();
        }

        public object Tag;
        internal int hindex = -1;
        internal string cacheToString = string.Empty;
        protected DBTable table;
        protected DBItemState state = DBItemState.New;
        protected DBUpdateState update = DBUpdateState.Default;
        private AccessValue access;

        public DBItem()
        {
        }

        //public DBItem(DBTable table, DBUpdateState state = DBUpdateState.Insert, bool def = true)
        //{
        //    Build(table, state, def);
        //}

        public DBItem GetVirtual(DBTable table)// where T : DBItem, new()
        {
            if (PropertyChanged != null)
            {
                var delegates = PropertyChanged.GetInvocationList();
                foreach (var d in delegates)
                {
                    var target = d.Target as DBItem;
                    if (target != null && target.Table == table)
                        return target;
                }
            }
            return null;
        }

        public object GetTag(DBColumn column)
        {
            return column.GetTag(hindex);
        }

        public void SetTag(DBColumn column, object value)
        {
            if (value == null)
            {
                column.RemoveTag(hindex);
            }
            else
            {
                column.SetTag(hindex, value);
            }
        }

        public bool GetOld(DBColumn column, out object value)
        {
            return column.GetOld(hindex, out value);
        }

        public void RemoveOld(DBColumn column)
        {
            column.RemoveOld(hindex);
        }

        public void SetOld(DBColumn column, object value)
        {
            column.SetOld(hindex, value);
        }

        public virtual void RemoveIndex(DBColumn column, object value)
        {
            if (Attached && column.Index != null)
            {
                column.Index.Remove(this, value);
            }
            if (PropertyChanged != null)
            {
                var delegates = PropertyChanged.GetInvocationList();
                foreach (var handler in delegates)
                    if (handler.Target is DBVirtualItem && ((DBVirtualItem)handler.Target).Attached)
                    {
                        var target = (DBVirtualItem)handler.Target;
                        var dcolumn = target.VirtualTable.GetColumnByBase(column);
                        if (dcolumn != null && dcolumn.Index != null)
                            dcolumn.Index.Remove(target, value);
                    }
            }
        }

        public virtual void AddIndex(DBColumn column, object value)
        {
            if (Attached && column.Index != null)
            {
                column.Index.Add(this, value);
            }
            if (PropertyChanged != null)
            {
                var delegates = PropertyChanged.GetInvocationList();
                foreach (var handler in delegates)
                    if (handler.Target is DBItem && ((DBItem)handler.Target).Attached)
                    {
                        var target = (DBItem)handler.Target;
                        var dcolumn = target.Table.Columns.GetByBase(column.Name);
                        if (dcolumn != null && dcolumn.Index != null)
                            dcolumn.Index.Add(target, value);
                    }
            }
        }

        public object GetValue(DBColumn column)
        {
            return column.Pull.Get(hindex);
        }

        public T GetValue<T>(DBColumn column)
        {
            return column.Pull.GetValue<T>(hindex);
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
        }

        public void SetProperty(object value, [CallerMemberName] string property = null)
        {
            SetValue(value, Table.Columns.GetByProperty(property));
        }

        public void SetValue(object value, DBColumn column)
        {
            SetValue(value, column, column.ColumnType == DBColumnTypes.Default);
        }

        public void SetValue(object value, DBColumn column, bool check, object tag = null)
        {
            SetTag(column, tag);

            var field = GetValue(column);

            if (DBService.Equal(field, value))
            {
                return;
            }
            DBItemEventArgs args = null;
            if (check)
            {
                args = OnPropertyChanging(column.Name, column, value);
                if (args.Cancel)
                {
                    return;
                }
                RefreshOld(column, value, field);
            }

            RemoveIndex(column, field);

            column.Pull.Set(hindex, value);

            AddIndex(column, value);

            OnPropertyChanged(column.Name, column, value);

            if (check)
            {
                CheckState();
                DBService.OnEdited(args);
            }
        }

        public bool IsChangedKey(DBColumn column)
        {
            return column.GetOld(hindex, out object value);
        }

        public virtual bool GetIsChanged()
        {
            return GetChangeKeys().Any();
        }

        public IEnumerable<DBColumn> GetChangeKeys()
        {
            foreach (var column in Table.Columns)
            {
                if (column.GetOld(hindex, out object value))
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
            foreach (DBColumn column in table.Columns)
            {
                RemoveOld(column);
            }
        }

        protected virtual internal void RemoveTag()
        {
            access = null;
            foreach (DBColumn column in table.Columns)
            {
                object o = GetTag(column);
                if (o != null)
                {
                    SetTag(column, null);
                }
            }
        }

        public DBItem GetReference(string code)
        {
            DBItem row = this;
            int pi = 0, i = code.IndexOf('.');
            while (i > 0)
            {
                var item = row.GetReference(row.table.Columns[code.Substring(pi, i - pi)]);
                if (item == null)
                    return null;
                row = item;
                pi = i + 1;
                i = code.IndexOf('.', pi);
            }
            return row.GetReference(row.table.Columns[code.Substring(pi)]);
        }

        public DBItem GetReference(DBColumn column, DBLoadParam param = DBLoadParam.Load)
        {
            var item = GetTag(column) as DBItem;

            if (item == null)
            {
                object value = GetValue(column);
                if (value == null)
                    return null;

                item = column.ReferenceTable.LoadItemById(value, param);
                SetTag(column, item);
            }
            return item;
        }

        public T GetReference<T>(string code, DBLoadParam param = DBLoadParam.Load) where T : DBItem, new()
        {
            DBItem row = this;
            int pi = 0, i = code.IndexOf('.');
            while (i > 0)
            {
                var item = row.GetReference<T>(row.table.Columns[code.Substring(pi, i - pi)], param);
                if (item == null)
                    return null;
                row = item;
                pi = i + 1;
                i = code.IndexOf('.', pi);
            }
            return row.GetReference<T>(row.table.Columns[code.Substring(pi)], param);
        }

        public T GetPropertyReference<T>([CallerMemberName] string property = null) where T : DBItem, new()
        {
            return GetReference<T>(Table.Foreigns.GetByProperty(property)?.Column);
        }

        public T GetReference<T>(DBColumn column, DBLoadParam param = DBLoadParam.Load) where T : DBItem, new()
        {
            if (column == null)
                return null;
            if (column.IsPrimaryKey)
                return (T)this;
            if (!column.IsReference)
                return null;

            T item = GetTag(column) as T;

            if (item == null)
            {
                object value = this[column];
                if (value == null)
                    return null;

                item = (T)column.ReferenceTable.LoadItemById(value, param);
                SetTag(column, item);// item == null ? (object)DBNull.Value : item);
            }
            return item;
        }

        public void SetReference(DBItem value, string column)
        {
            SetReference(value, Table.Columns[column]);
        }

        public void SetPropertyReference(DBItem value, [CallerMemberName] string property = null)
        {
            SetReference(value, Table.Foreigns.GetByProperty(property)?.Column);
        }

        public void SetReference(DBItem value, DBColumn column)
        {
            SetValue(value?.PrimaryId, column, column.ColumnType == DBColumnTypes.Default, value);
        }

        public object GetCache(string column)
        {
            return GetCache(Table.Columns[column]);
        }

        public virtual object GetCache(DBColumn column)
        {
            if (column == null)
                return null;

            return GetTag(column);
        }

        public void SetCache(string column, object Value)
        {
            SetCache(Table.Columns[column], Value);
        }

        public void SetCache(DBColumn Column, object Value)
        {
            if (Column != null)
                SetTag(Column, Value);
        }

        public bool GetBool(DBColumn column)
        {
            if (column == null || (column.Keys & DBColumnKeys.Boolean) != DBColumnKeys.Boolean)
                return false;

            return this[column].ToString() == column.BoolTrue;
        }

        public bool GetBool(string ColumnCode)
        {
            return GetBool(table.Columns[ColumnCode]);
        }

        public void SetBool(DBColumn Column, bool value)
        {
            if (Column == null || (Column.Keys & DBColumnKeys.Boolean) != DBColumnKeys.Boolean)
                return;

            this[Column] = value ? Column.BoolTrue : Column.BoolFalse;
        }

        public void SetBool(string ColumnCode, bool value)
        {
            SetBool(table.Columns[ColumnCode], value);
        }

        public void Delete()
        {
            if ((UpdateState & DBUpdateState.Insert) == DBUpdateState.Insert)
                Detach();
            else
                UpdateState |= DBUpdateState.Delete;
        }

        public IEnumerable<T> GetReferencing<T>(QQuery query, DBLoadParam param) where T : DBItem, new()
        {
            query.TypeFilter = typeof(T);
            if ((param & DBLoadParam.Load) == DBLoadParam.Load)
                return (IEnumerable<T>)query.Load(param);
            return (IEnumerable<T>)query.Select();
        }

        public IEnumerable<T> GetReferencing<T>(DBTable table, string query, DBLoadParam param) where T : DBItem, new()
        {
            return GetReferencing<T>(new QQuery(query, table), param);
        }

        public IEnumerable<T> GetReferencing<T>(string property, DBLoadParam param) where T : DBItem, new()
        {
            var table = DBTable.GetTableAttribute<T>(true)?.Table;
            return GetReferencing<T>(table, table.ParseProperty(property), param);
        }

        public IEnumerable<T> GetReferencing<T>(DBTable table, DBColumn column, DBLoadParam param) where T : DBItem, new()
        {
            var query = new QQuery("", table);
            query.BuildParam(column, CompareType.Equal, PrimaryId);
            return GetReferencing<T>(query, param);
        }

        public IEnumerable<T> GetReferencing<T>(string tableCode, string columnCode, DBLoadParam param) where T : DBItem, new()
        {
            DBTable table = DBService.ParseTable(tableCode, Table.Schema);
            return table != null ? GetReferencing<T>(table, table.ParseColumn(columnCode), param) : null;
        }

        public IEnumerable<T> GetReferencing<T>(DBForeignKey relation, DBLoadParam param) where T : DBItem, new()
        {
            return GetReferencing<T>(relation.Table, relation.Column, param);
        }

        public IEnumerable<DBItem> GetReferencing(DBTable table, DBColumn column, DBLoadParam param)
        {
            var query = new QQuery("", table);
            query.BuildParam(column, CompareType.Equal, PrimaryId);
            return GetReferencing(query, param);
        }

        public IEnumerable<DBItem> GetReferencing(QQuery query, DBLoadParam param)
        {
            if ((param & DBLoadParam.Load) == DBLoadParam.Load)
                return (IEnumerable<DBItem>)query.Load();
            else
                return (IEnumerable<DBItem>)query.Select();
        }

        public IEnumerable GetReferencing(DBForeignKey relation, DBLoadParam param)
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
            return Format(table.Columns[code]);
        }

        public virtual void Build(DBTable table, DBUpdateState state = DBUpdateState.Insert, bool def = true)
        {
            Table = table;
            if (def)
            {
                foreach (DBColumn column in table.Columns)
                {
                    if (column.DefaultValue != null)
                        SetValue(column.ParseValue(column.DefaultValue), column, false);
                }
            }
            update = state;
        }

        public void SetCultureStrings(string @group, LocaleItem value)
        {
            foreach (LocaleString c in value)
            {
                SetName(@group, c.Culture, c.Value);
            }
        }

        public LocaleItem GetCultureStrings(string @group, string cultures)
        {
            var cs = new LocaleItem();
            if (string.IsNullOrEmpty(cultures))
            {
                foreach (DBColumn col in table.Columns.GetByGroup(@group))
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
                    string val = this.GetName(@group, ts);
                    if (val != null)
                        cs.Add(val, ts);
                }
            }
            return cs;
        }

        public string GetName(string @group, string culture)
        {
            return GetName(@group, CultureInfo.GetCultureInfo(culture));
        }

        public string GetName(string @group)
        {
            return GetName(@group, Locale.Instance.Culture);
        }

        public string GetName(string @group, CultureInfo culture)
        {
            if (culture == null)
                return null;
            foreach (var column in table.Columns.GetByGroup(@group))
            {
                if (column.Culture != null && column.Culture.ThreeLetterISOLanguageName == culture.ThreeLetterISOLanguageName)
                    return GetValue<string>(column);
            }
            return null;
        }

        public void SetName(string @group, string s, string value)
        {
            SetName(@group, CultureInfo.GetCultureInfo(s), value);
        }

        public void SetName(string @group, string value)
        {
            SetName(@group, Locale.Instance.Culture, value);
        }

        public void SetName(string @group, CultureInfo culture, string value)
        {
            if (culture == null)
                return;
            foreach (var column in table.Columns.GetByGroup(@group))
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
        [DataMember, Column("item_type", GroupName = "system", Keys = DBColumnKeys.ItemType | DBColumnKeys.System, Order = 98, Default = "0")]
        public int? ItemType
        {
            get { return Table.ItemTypeKey == null ? 0 : GetValue<int?>(Table.ItemTypeKey); }
            set { SetValue(value, Table.ItemTypeKey); }
        }

        [DataMember, Column("status_id", Default = "1", GroupName = "system", Keys = DBColumnKeys.State | DBColumnKeys.System | DBColumnKeys.Indexing, Order = 99)]
        public DBStatus Status
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

        [XmlIgnore, JsonIgnore, NotMapped, Browsable(false)]
        [DataMember, Column("group_access", 512, DataType = typeof(byte[]), GroupName = "system", Keys = DBColumnKeys.Access | DBColumnKeys.System, Order = 102)]
        public virtual AccessValue Access
        {
            get
            {
                if (access == null)
                {
                    if (table.AccessKey != null)
                    {
                        var accessData = GetValue<byte[]>(table.AccessKey);
                        if (accessData == null)
                            access = Table.Access;
                        else
                            access = new AccessValue(accessData);
                    }
                    else
                    {
                        access = Table.Access;
                    }
                }
                return access;
            }
            set
            {
                if (value != null && table.AccessKey != null)
                {
                    SetValue(value.Write(), table.AccessKey);
                    access = value;
                }
            }
        }

        [DataMember, Browsable(false)]
        public virtual string Name
        {
            get { return GetName(nameof(Name)); }
            set { SetName(nameof(Name), value); }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public virtual DBTable Table
        {
            get { return table; }
            set
            {
                if (table != value)
                {
                    table = value;
                    hindex = Pull.GetHIndexUnsafe(++table.Hash, table.BlockSize);
                }
            }
        }

        public object this[int columnIndex]
        {
            get { return this[table.Columns[columnIndex]]; }
            set { this[table.Columns[columnIndex]] = value; }
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
                    var item = row.GetReference(row.table.Columns[code.Substring(pi, i - pi)]);
                    if (item == null)
                        return null;
                    row = item;
                    pi = i + 1;
                    i = code.IndexOf('.', pi);
                }
                return row[row.table.Columns[code.Substring(pi)]];
            }
            set
            {
                DBItem row = this;
                int pi = 0, i = code.IndexOf('.');
                while (i > 0)
                {
                    var item = row.GetReference(row.table.Columns[code.Substring(pi, i - pi)]);
                    if (item == null)
                        return;
                    row = item;
                    pi = i + 1;
                    i = code.IndexOf('.', pi);
                }
                row[row.table.Columns[code.Substring(pi)]] = value;
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
                    OnPropertyChanged(nameof(UpdateState), null, value);
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

        public void Accept()
        {
            if (IsChanged || (UpdateState & DBUpdateState.Commit) == DBUpdateState.Commit)
            {
                DBService.OnAccept(this);
                if ((UpdateState & DBUpdateState.Delete) == DBUpdateState.Delete)
                {
                    Detach();
                    UpdateState = DBUpdateState.Delete;
                }
                else
                {
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

        public virtual void Reject()
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
                DBService.OnReject(this);
            }
        }

        #region ICloneable Members

        public virtual object Clone()
        {
            var item = Table.NewItem();
            foreach (var column in Table.Columns)
            {
                var value = GetValue(column);
                if (value != null)
                    item.SetValue(value, column);
            }

            if (Table.PrimaryKey != null)
                item.SetValue(null, Table.PrimaryKey);

            return item;
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
            State |= DBItemState.Attached;
            OnPropertyChanged(nameof(Attached), null);
            DBService.OnAdded(this);
        }

        public virtual void OnDetached()
        {
            State &= ~DBItemState.Attached;
            OnPropertyChanged(nameof(Attached), null);
            DBService.OnRemoved(this);
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
        public event DBItemEditEventHandler PropertyChanging;

        public virtual DBItemEventArgs OnPropertyChanging(string property, DBColumn column = null, object value = null)
        {
            var args = new DBItemEventArgs(this, column, column.Name, value);
            DBService.OnEditing(args);
            PropertyChanging?.Invoke(args);
            return args;
        }

        public virtual void OnPropertyChanged(string property, DBColumn column = null, object value = null)
        {
            // clear ToString internal cache 
            if (column != null && (column.Keys & DBColumnKeys.View) == DBColumnKeys.View)
                cacheToString = string.Empty;

            // clear objects cache if value changed
            if (property.Length == 0)
                RemoveTag();

            if (Attached)
                table.OnItemChanged(this, property, NotifyCollectionChangedAction.Reset);
            // raise events
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(column?.Property ?? property));
        }
        #endregion

        public void Refresh()
        {
            Reject();
            Table.ReloadItem(PrimaryId);
        }

        public void GenerateId()
        {
            if (Table.PrimaryKey != null && PrimaryId == null && Table.Sequence != null)
            {
                PrimaryId = Table.Sequence.NextValue();
            }
        }

        public void Clear()
        {
            foreach (DBColumn column in table.Columns)
                SetValue(null, column);
        }

        [Browsable(false)]
        public bool IsChanged
        {
            get { return UpdateState != DBUpdateState.Default && (UpdateState & DBUpdateState.Commit) != DBUpdateState.Commit; }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public byte[] Image
        {
            get { return this[table.ImageKey] as byte[]; }
            set
            {
                if (table.ImageKey == null)
                    return;
                this[table.ImageKey] = value;
            }
        }

        public virtual void Save()
        {
            if (IsChanged)
            {
                Table.SaveItem(this);
            }
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
                    OnPropertyChanged(nameof(Check));
                }
            }
        }

        public int CompareTo(DBItem obj)
        {
            return obj == null ? 1 : obj.table == table ? hindex.CompareTo(obj.hindex) :
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
            if (!Access.View)
                return "********";
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
                if (!column.Access.View)
                {
                    //bufRez += temprez;
                    continue;
                }
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

        public void Delete(int recurs = 2, DBLoadParam param = DBLoadParam.None)
        {
            try
            {
                recurs--;
                var relations = Table.GetChildRelations();
                foreach (DBForeignKey relation in relations)
                {
                    if (relation.Table.Name.IndexOf("drlog", StringComparison.OrdinalIgnoreCase) >= 0
                        || relation.Table.Type != DBTableType.Table
                        || relation.Column.ColumnType != DBColumnTypes.Default)
                        continue;
                    if (recurs >= 0 || relation.Table == Table)
                    {
                        var list = GetReferencing(relation, param);
                        foreach (DBItem item in list)
                        {
                            if (item.Attached)
                                item.Delete(recurs, param);
                        }
                    }
                }
                if ((UpdateState & DBUpdateState.Insert) == DBUpdateState.Insert)
                    Table.Remove(this);
                else
                {
                    Delete();
                    Save();
                }
            }
            catch (Exception ex)//TODO If Timeout Expired
            {
                Helper.OnException(ex);
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
                if (relation.Table is DBLogTable ||
                    relation.Table.Type != DBTableType.Table ||
                    relation.Column.ColumnType != DBColumnTypes.Default)
                    continue;

                foreach (DBItem item in GetReferencing(relation, param))
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
            var rows = new List<DBItem>();
            rows.Add(this);
            foreach (DBItem item in list)
                if (item != this)
                {
                    rows.Add(item);

                    item.UpdateState |= DBUpdateState.Delete;
                    foreach (DBColumn column in item.Table.Columns)
                        if (this[column] == DBNull.Value && item[column] != DBNull.Value)
                            this[column] = item[column];

                    foreach (DBForeignKey relation in relations)
                        if (relation.Table.Type == DBTableType.Table)
                        {
                            var refings = item.GetReferencing<DBItem>(relation, DBLoadParam.Load | DBLoadParam.Synchronize).ToList();
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

        public void SaveOrUpdate(DBLoadParam param = DBLoadParam.None)
        {
            var exist = FindAndUpdate(param);
            if (exist != null)
            {
                exist.Save();
            }
            else
            {
                Save();
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
                    if ((column.Keys & DBColumnKeys.Access) == DBColumnKeys.Access
                        || (column.Keys & DBColumnKeys.State) == DBColumnKeys.State
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

