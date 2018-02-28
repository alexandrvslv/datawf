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
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace DataWF.Data
{
    public class DBItem : ICloneable, IComparable<DBItem>, IDisposable, IGroup, IAccessable, ICheck, INotifyPropertyChanged, IEditable, IStatus, IDBTableContent
    {
        public static readonly DBItem EmptyItem = new DBItem() { cacheToString = "Loading" };
        public object Tag;
        internal int hindex = -1;
        internal string cacheToString = string.Empty;
        protected DBTable table;
        protected DBItemState state = DBItemState.New;
        protected DBUpdateState update = DBUpdateState.Default;

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
            return GetValue<T>(ParseProperty(property));
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
            var temp = DBState;
            if (temp == DBUpdateState.Default || (temp & DBUpdateState.Commit) == DBUpdateState.Commit)
            {
                temp &= ~DBUpdateState.Commit;
                temp |= DBUpdateState.Update;
            }
            else if (temp == DBUpdateState.Update && !GetIsChanged())
            {
                temp = DBUpdateState.Default;
            }
            DBState = temp;
        }

        public void SetProperty(object value, [CallerMemberName] string property = null)
        {
            SetValue(value, ParseProperty(property));
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

        public DBItem GetReference(DBColumn column, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null)
        {
            object value = GetValue(column);
            return value == null ? null : column.ReferenceTable.LoadItemById(value, param, transaction);
        }

        public T GetReference<T>(string code, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null) where T : DBItem, new()
        {
            DBItem row = this;
            int pi = 0, i = code.IndexOf('.');
            while (i > 0)
            {
                var item = row.GetReference<T>(row.table.Columns[code.Substring(pi, i - pi)], param, transaction);
                if (item == null)
                    return null;
                row = item;
                pi = i + 1;
                i = code.IndexOf('.', pi);
            }
            return row.GetReference<T>(row.table.Columns[code.Substring(pi)], param, transaction);
        }

        public T GetPropertyReference<T>(string property) where T : DBItem, new()
        {
            return GetReference<T>(ParseProperty(property));
        }

        public T GetReference<T>(DBColumn column, DBLoadParam param = DBLoadParam.Load, DBTransaction transaction = null) where T : DBItem, new()
        {
            if (column == null)
                return null;
            if (column.IsPrimaryKey)
                return (T)this;
            if (!column.IsReference)
                return null;

            if (column is DBVirtualColumn && ((DBVirtualColumn)column).BaseColumn.IsReference)
            {
                var baseitem = GetReference<T>(((DBVirtualColumn)column).BaseColumn, param, transaction);
                return baseitem == null ? null : baseitem.GetVirtual(column.ReferenceTable) as T;
            }

            T item = GetTag(column) as T;

            if (item == null)
            {
                object value = this[column];
                if (value == null)
                    return null;

                item = ((DBTable<T>)column.ReferenceTable).LoadById(value, param, transaction);
                SetTag(column, item);// item == null ? (object)DBNull.Value : item);             
            }
            return item;
        }

        public void SetReference(DBItem value, string column)
        {
            SetReference(value, Table.Columns[column]);
        }

        public void SetPropertyReference(DBItem value, string property)
        {
            SetReference(value, ParseProperty(property));
        }

        public void SetReference(DBItem value, DBColumn column)
        {
            SetValue(value?.PrimaryId, column, column.ColumnType == DBColumnTypes.Default, value);
        }

        public object GetCache(string column)
        {
            return GetCache(Table.Columns[column]);
        }

        public object GetCache(DBColumn column)
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
            if ((DBState & DBUpdateState.Insert) == DBUpdateState.Insert)
                Detach();
            else
                DBState |= DBUpdateState.Delete;
        }

        public IEnumerable<T> GetReferencing<T>(QQuery query, DBLoadParam param) where T : DBItem, new()
        {
            if ((param & DBLoadParam.Load) == DBLoadParam.Load)
                return ((DBTable<T>)query.Table).Load(query, param, null);
            return ((DBTable<T>)query.Table).Select(query);
        }

        public IEnumerable<T> GetReferencing<T>(DBTable table, string query, DBLoadParam param) where T : DBItem, new()
        {
            return GetReferencing<T>(new QQuery(query, table), param);
        }

        public IEnumerable<T> GetReferencing<T>(DBTable table, DBColumn column, DBLoadParam param) where T : DBItem, new()
        {
            if ((param & DBLoadParam.Load) == DBLoadParam.Load)
            {
                var query = new QQuery("", table);
                query.BuildParam(column, CompareType.Equal, PrimaryId);
                return GetReferencing<T>(query, param);
            }
            return ((DBTable<T>)table).Select(column, PrimaryId, CompareType.Equal);
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
                return table.LoadItems(query);
            else
                return table.SelectItems(query);
        }

        public IEnumerable GetReferencing(DBForeignKey relation, DBLoadParam param)
        {
            return GetReferencing(relation.Table, relation.Column, param);
        }

        public override string ToString()
        {
            if (cacheToString.Length == 0)
                cacheToString = DBService.GetRowText(this);
            return cacheToString;
        }

        public string Format(DBColumn column)
        {
            return DBService.FormatValue(column, this[column]);
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
                        SetValue(DBService.ParseValue(column, column.DefaultValue), column, false);
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

        public DBColumn ParseProperty(string property)
        {
            return Table.Info?.GetColumnByProperty(property)?.Column;
        }

        [Browsable(false)]
        public object PrimaryId
        {
            get { return Table.PrimaryKey == null ? null : this[Table.PrimaryKey]; }
            set { this[Table.PrimaryKey] = value; }
        }

        [Browsable(false)]
        public string PrimaryCode
        {
            get { return Table.CodeKey == null ? null : GetValue<string>(Table.CodeKey); }
            set { this[Table.CodeKey] = value; }
        }

        [Browsable(false)]
        [Column("stateid", Default = "1", GroupName = "system", Keys = DBColumnKeys.State, Order = 99)]
        public DBStatus Status
        {
            get { return Table.StatusKey == null ? DBStatus.Empty : GetValue<DBStatus?>(Table.StatusKey).GetValueOrDefault(); }
            set { SetValue(value, Table.StatusKey); }
        }


        [Column("datecreate", GroupName = "system", Keys = DBColumnKeys.Date, Order = 100)]
        public DateTime? Date
        {
            get { return Table.DateKey == null ? null : GetValue<DateTime?>(Table.DateKey); }
            set { SetValue(value, Table.DateKey); }
        }

        [Browsable(false)]
        [Column("dateupdate", GroupName = "system", Keys = DBColumnKeys.Stamp | DBColumnKeys.NoLog, Order = 101)]
        public DateTime? Stamp
        {
            get { return Table.StampKey == null ? null : GetValue<DateTime?>(Table.StampKey); }
            set { SetValue(value, Table.StampKey); }
        }

        [Browsable(false)]
        [Column("gaccess", 512, DataType = typeof(byte[]), GroupName = "system", Keys = DBColumnKeys.Access, Order = 102)]
        public virtual AccessValue Access
        {
            get
            {
                AccessValue access = Table.Access;

                if (table.AccessKey != null && this[table.AccessKey] != null)
                {
                    access = GetCache(table.AccessKey) as AccessValue;
                    if (access == null)
                    {
                        access = new AccessValue((byte[])this[table.AccessKey]);
                        SetCache(table.AccessKey, access);
                    }
                }
                return access;
            }
            set
            {
                if (value != null && table.AccessKey != null)
                {
                    //if (!value.IsEqual(_table.Access))
                    {
                        SetValue(value.Write(), table.AccessKey);
                    }
                    SetCache(table.AccessKey, value);
                }
            }
        }

        [Browsable(false)]
        public object GroupId
        {
            get { return table.GroupKey == null ? null : this[table.GroupKey]; }
            set { SetValue(value, table.GroupKey); }
        }

        [Browsable(false)]
        public DBItem Group
        {
            get { return table.GroupKey == null ? null : GetReference(table.GroupKey); }
            set
            {
                if (value == null || (value.GroupId != PrimaryId && value.PrimaryId != PrimaryId))
                {
                    SetReference(value, Table.GroupKey);
                }
            }
        }

        IGroup IGroup.Group
        {
            get { return Group; }
            set { Group = value as DBItem; }
        }

        public virtual string Name
        {
            get { return GetName("name"); }
            set { SetName("name", value); }
        }

        [Browsable(false)]
        public string FullName
        {
            get
            {
                char separator = Path.PathSeparator;
                string buf = string.Empty;
                DBItem row = this;
                while (row != null)
                {
                    buf = row.Name + (buf.Length == 0 ? string.Empty : (separator + buf));
                    row = row.Group;
                }
                return buf;
            }
        }

        public IEnumerable<DBItem> GetGroups()
        {
            if (Group == null)
                yield break;
            yield return Group;

            var g = Group;
            while (g.Group != null)
            {
                yield return g.Group;
                g = g.Group;
            }
        }

        public bool AllParentExpand()
        {
            return GroupHelper.IsExpand(this);
        }

        public bool GroupCompare(string column, string value)
        {
            DBColumn col = table.Columns[column];
            foreach (DBItem g in GetGroups())
            {
                if (!g[col].ToString().Equals(value, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }

        [Browsable(false)]
        public DBTable Table
        {
            get { return table; }
            set
            {
                table = value;
                hindex = Pull.GetHIndex(++table.Hash, table.BlockSize);
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

                SetValue(DBService.ParseValue(column, value),
                         column,
                         column.ColumnType == DBColumnTypes.Default,
                         value as DBItem);
            }
        }
        [Browsable(false)]
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

        [Browsable(false)]
        public virtual DBUpdateState DBState
        {
            get { return update; }
            set
            {
                if (update != value)
                {
                    var arg = new DBItemEventArgs(this) { State = update };
                    update = value;
                    OnPropertyChanged(nameof(DBState), null, value);
                    DBService.OnStateEdited(arg);
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
            if (IsChanged || (DBState & DBUpdateState.Commit) == DBUpdateState.Commit)
            {
                if ((DBState & DBUpdateState.Delete) == DBUpdateState.Delete)
                {
                    Detach();
                    DBState = DBUpdateState.Delete;
                }
                else
                {
                    DBState = DBUpdateState.Default;
                }
                RemoveOld();
                DBService.OnAccept(this);
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
            if (IsChanged || (DBState & DBUpdateState.Commit) == DBUpdateState.Commit)
            {
                if ((DBState & DBUpdateState.Insert) == DBUpdateState.Insert)
                {
                    Detach();
                    DBState = DBUpdateState.Insert;
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
                    DBState = DBUpdateState.Default;
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
        public IEnumerable<T> GetSubGroups<T>(DBLoadParam param) where T : DBItem, new()
        {
            if (PrimaryId == null)
                return new List<T>(0);
            return GetReferencing<T>(Table, Table.GroupKey, param);
        }

        public List<T> GetSubGroupFull<T>(bool addCurrent = false) where T : DBItem, new()
        {
            var buf = GetSubGroups<T>(DBLoadParam.None);
            var rez = new List<T>();
            if (addCurrent)
                rez.Add((T)this);
            rez.AddRange(buf);
            foreach (var row in buf)
                rez.AddRange(row.GetSubGroupFull<T>());
            return rez;
        }

        public string GetSubGroupIds()
        {
            string rez = "";
            rez = PrimaryId.ToString();
            foreach (DBItem row in GetSubGroupFull<DBItem>())
                rez += "," + row.PrimaryId;
            return rez;
        }

        public IEnumerable<T> GetParents<T>(bool addCurrent = false)
        {
            return GroupHelper.GetAllParent<T>(this, addCurrent);
        }

        public string GetParentIds()
        {
            string rez = "";
            rez = PrimaryId.ToString();
            foreach (DBItem row in GroupHelper.GetAllParent<DBItem>(this))
            {
                rez += "," + row.PrimaryId;
            }
            return rez;
        }

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
                OnPropertyChanged(nameof(Attached), null);
            }
        }

        internal void Detach()
        {
            if (Attached)
            {
                Table.Remove(this);
                OnPropertyChanged(nameof(Attached), null);
            }
        }

        public virtual bool OnUpdating(DBItemEventArgs arg)
        {
            return Table.OnUpdating(arg);
        }

        public virtual bool OnUpdated(DBItemEventArgs arg)
        {
            return Table.OnUpdated(arg);
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
                table.OnListChanged(this, property, ListChangedType.ItemChanged);
            // raise events
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        #endregion

        public void Refresh()
        {
            Reject();
            using (var transaction = new DBTransaction(Table.Schema.Connection))
                Table.ReloadItem(PrimaryId, transaction);
        }

        public void GenerateId(DBTransaction transaction = null)
        {
            if (Table.PrimaryKey != null && PrimaryId == null && Table.Sequence != null)
            {
                PrimaryId = Table.Sequence.NextValue(transaction);
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
            get { return DBState != DBUpdateState.Default && (DBState & DBUpdateState.Commit) != DBUpdateState.Commit; }
        }

        [Browsable(false)]
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

        #region IGroupable implementation

        [Browsable(false)]
        public bool IsExpanded
        {
            get { return IsParentActive(this); }
        }

        public static bool IsParentActive(DBItem item)
        {
            if (item.Group == null)
                return true;
            if (!item.Group.Expand)
                return false;
            return IsParentActive(item.Group);
        }

        [Browsable(false)]
        public bool Expand
        {
            get { return (state & DBItemState.Expand) == DBItemState.Expand; }
            set
            {
                if (Expand != value)
                {
                    state = value ? state | DBItemState.Expand : state & ~DBItemState.Expand;
                    OnPropertyChanged(nameof(Expand), null);
                }
            }
        }

        [Browsable(false)]
        public bool IsCompaund
        {
            get
            {
                if (table.GroupKey == null)
                    return false;
                return GetSubGroups<DBItem>(DBLoadParam.None).Any();
            }
        }
        #endregion

        public void Save()
        {
            Save(true);
        }

        public virtual void Save(bool reference, object tag = null)
        {
            if (IsChanged)
            {
                using (var transaction = new DBTransaction(Table.Schema.Connection) { Reference = reference, Tag = tag })
                {
                    Save(transaction);
                    transaction.Commit();
                }
            }
        }

        public virtual void Save(DBTransaction transaction)
        {
            Table.SaveItem(this, transaction);
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as DBItem);
        }

        [Browsable(false)]
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

        public string DMLPatch()
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
    }
}

