/*
 DBView.cs
 
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
using System;
using System.Collections;
using System.Collections.Generic;
using DataWF.Common;
using System.ComponentModel;
using System.IO;
using System.Data;
using System.Linq;

namespace DataWF.Data
{
    public interface IDBList : IList, IDBTableContent
    {
    }

    public class DBTableView<T> : SelectableList<T>, IDBTableView where T : DBItem, new()
    {
        protected DBViewKeys keys = DBViewKeys.None;
        protected string defaultFilter = "";
        protected DBStatus statusFilter = 0;
        protected QQuery query = new QQuery();

        protected IDbCommand command;
        protected DBTable<T> table;

        public DBTableView(DBTable<T> table, string defaultFilter = "", DBViewKeys mode = DBViewKeys.None, DBStatus statusFilter = DBStatus.Empty)
        {
            keys = mode;
            table.AddView(this);
            this.table = table;
            this.defaultFilter = defaultFilter ?? string.Empty;
            this.statusFilter = statusFilter;
            UpdateFilter(string.Empty);
            if ((keys & DBViewKeys.Empty) != DBViewKeys.Empty)
                UpdateFilter();
        }

        ~DBTableView()
        {
            Dispose();
        }

        public bool AutoAttach
        {
            get { return (keys & DBViewKeys.NoAttach) != DBViewKeys.NoAttach; }
            set
            {
                if (value)
                    keys &= ~DBViewKeys.NoAttach;
                else
                    keys |= DBViewKeys.NoAttach;
            }
        }

        public bool CheckAccess
        {
            get { return (keys & DBViewKeys.Access) == DBViewKeys.Access; }
            set
            {
                if (CheckAccess == value)
                    return;
                if (value)
                {
                    keys |= DBViewKeys.Access;
                    for (int i = 0; i < Count;)
                    {
                        T document = this[i];
                        if (!document.Access.View)
                            RemoveAt(i);
                        else
                            i++;
                    }
                }
                else
                {
                    keys &= ~DBViewKeys.Access;
                }
            }
        }

        public bool IsStatic
        {
            get { return (keys & DBViewKeys.Static) == DBViewKeys.Static; }
            set
            {
                if (IsStatic == value)
                    return;
                if (value)
                    keys |= DBViewKeys.Static;
                else
                    keys &= ~DBViewKeys.Static;
            }
        }

        public override bool IsSynchronized
        {
            get { return (keys & DBViewKeys.Synch) == DBViewKeys.Synch; }
            set
            {
                if (IsSynchronized != value)
                {
                    if (value)
                        keys |= DBViewKeys.Synch;
                    else
                        keys &= ~DBViewKeys.Synch;
                }
            }
        }

        public T this[string code]
        {
            get
            {
                if (table.CodeKey == null)
                    return null;
                return table.LoadByCode(code);
            }
        }

        public string Filter
        {
            get { return query.ToWhere(); }
            set
            {
                keys &= ~DBViewKeys.Synch;
                UpdateFilter(value);
                UpdateFilter();
            }
        }

        public QQuery Query
        {
            get { return query; }
            set
            {
                if (value != null)
                {
                    query = value;
                    query.Table = table;
                }
            }
        }

        public Query FilterQuery { get; set; }

        public event EventHandler StatusFilterChanged;

        public DBStatus StatusFilter
        {
            get { return statusFilter; }
            set
            {
                if (statusFilter == value)
                    return;
                statusFilter = value;
                Filter = "";
                StatusFilterChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler DefaultFilterChanged;

        public virtual string DefaultFilter
        {
            get { return defaultFilter; }
            set
            {
                if (defaultFilter == value)
                    return;
                defaultFilter = value;
                Filter = "";
                DefaultFilterChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public IList ToList()
        {
            List<T> list = new List<T>(Count);
            foreach (T item in this)
                list.Add(item);
            return list;
        }

        public IDbCommand Command
        {
            get { return command; }
            set { command = value; }
        }

        public bool IsEdited
        {
            get { return GetEdited().Any(); }
        }

        public DBSchema Schema
        {
            get { return Table?.Schema; }
        }

        public DBTable Table
        {
            get { return table; }
            set { table = value as DBTable<T>; }
        }

        IEnumerable<DBItem> IDBTableView.SelectChilds(DBItem item)
        {
            return SelectChilds(item);
        }

        public IEnumerable<T> SelectChilds(DBItem group)
        {
            foreach (var item in items)
                if (item.Group == group)
                    yield return item;
        }

        IEnumerable<DBItem> IDBTableView.SelectParents()
        {
            return SelectParents();
        }

        public IEnumerable<T> SelectParents()
        {
            foreach (var item in items)
            {
                if (item.Group == null)
                    yield return item;
            }
        }

        IEnumerable<DBItem> IDBTableView.Load(DBLoadParam param)
        {
            return Load(param);
        }

        public IEnumerable<T> Load(DBLoadParam param = DBLoadParam.None)
        {
            return table.Load(Query, param, this);
        }

        public void LoadAsynch(DBLoadParam param = DBLoadParam.None)
        {
            table.LoadAsync(Query, param, this);
        }

        public override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnItemChanged((T)sender, e.PropertyName, ListChangedType.ItemChanged);
        }

        public void OnItemChanged(T item, string property, ListChangedType type)
        {
            if ((keys & DBViewKeys.Static) == DBViewKeys.Static && type != ListChangedType.ItemChanged)
            {
                return;
            }
            if (type == ListChangedType.Reset)
            {
                UpdateFilter();
            }
            else
                lock (items)
                {
                    int index = -1, newindex = -1;
                    index = newindex = items.BinarySearch(item);
                    if (index < 0)
                    {
                        newindex = (-index) - 1;
                        if (newindex > items.Count)
                            newindex = items.Count;
                    }
                    if (index < 0 || !item.Equals(items[index]))
                    {
                        index = type == ListChangedType.ItemAdded ? -1 : items.IndexOf(item);// && !exist ? -1 : 
                    }

                    switch (type)
                    {
                        case ListChangedType.ItemChanged:
                            if (index < 0)
                            {
                                if (table.CheckItem(item, query))
                                    Insert(newindex, item);
                            }
                            else if (!table.CheckItem(item, query))
                            {
                                RemoveAt(index);
                            }
                            else
                            {
                                if (newindex != index)
                                {
                                    if (newindex > index)
                                        newindex--;
                                    items.RemoveAt(index);
                                    items.Insert(newindex, item);
                                    // RaiseListChanged(ListChangedType.ItemMoved, newindex, index);
                                    OnListChanged(ListChangedType.ItemChanged, newindex, -1, property);
                                }
                                else
                                    OnListChanged(type, index, -1, property);
                            }
                            break;
                        case ListChangedType.ItemDeleted:
                            if (index >= 0)
                                RemoveAt(index);
                            break;
                        case ListChangedType.ItemAdded:
                            if (index < 0 && table.CheckItem(item, query))
                                Insert(newindex, item);
                            break;
                    }
                }
        }

        public void OnItemChanged(DBItem item, string property, ListChangedType type)
        {
            OnItemChanged((T)item, property, type);
        }

        private void SetItems(List<DBItem> list)
        {
            AddRange(list.Cast<T>());
        }

        #region IBindingListView Members

        public void UpdateFilter()
        {
            if (!query.IsEmpty())
            {
                items = table.Select(query).ToList();
                items.Sort();
            }
            else
            {
                items.Clear();
                items.AddRange(table);
            }
            OnListChanged(ListChangedType.Reset, -1);
        }

        public void Accept()
        {
            var edited = GetEdited().ToList();
            foreach (T item in edited)
            {
                if (IsStatic && (item.DBState & DBUpdateState.Delete) == DBUpdateState.Delete)
                    Remove(item);
                item.Accept();
            }
        }

        public void Reject()
        {
            var edited = GetEdited().ToList();
            foreach (T item in edited)
            {
                if (IsStatic && (item.DBState & DBUpdateState.Insert) == DBUpdateState.Insert)
                    Remove(item);
                item.Reject();
            }
        }

        public IEnumerable<T> GetEdited()
        {
            for (int i = 0; i < items.Count; i++)
            {
                T view = items[i];
                if (view != null && view.DBState != DBUpdateState.Default)
                    yield return view;
            }
        }

        public void Save()
        {
            Table.Save(GetEdited().ToList());
        }

        public void Sort(params DBColumn[] p)
        {
            items.Sort(new DBComparerList(p));
            OnListChanged(ListChangedType.Reset, -1);
        }

        public void Sort(params string[] columns)
        {
            items.Sort(new DBComparerList(table, columns));
            OnListChanged(ListChangedType.Reset, -1);
        }

        public void UpdateFilter(string value)
        {
            query.Table = table;
            string val = CheckDefaultFilter(value);
            if (val == "" && query.Parameters.Count > 0)
                query.CacheQuery = null;
            if (query.CacheQuery == val)
                return;
            query.Parse(val);
        }

        public string CheckDefaultFilter(string filter)
        {
            if (defaultFilter == null)
                defaultFilter = string.Empty;
            string stateFilter = DBService.FormatStatusFilter(Table, statusFilter);
            string rezult = stateFilter;
            if (defaultFilter.Length != 0)
                rezult = (stateFilter.Length != 0 ? stateFilter + " and " : string.Empty) + defaultFilter;

            if (filter.Length != 0)
                rezult = (stateFilter.Length != 0 ? stateFilter + " and " : string.Empty) +
                    (defaultFilter.Length != 0 ? "(" + defaultFilter + ") and " : string.Empty) +
                    filter;

            return rezult;
        }

        public void RemoveFilter()
        {
            query.Parameters.Clear();
            UpdateFilter();
        }

        public override int AddInternal(T item)
        {
            lock (items)
            {
                if (AutoAttach && !item.Attached)
                    table.Add(item);
                int index = GetIndexBySort(item);
                if (index < 0)
                    index = (-index) - 1;
                InsertInternal(index, item);
                return index;
            }
        }

        #endregion

        public override void Dispose()
        {
            base.Dispose();
            if (table != null)
            {
                table.RemoveView(this);
            }
        }

        public IEnumerable<T> GetTop()
        {
            return table.Select(Table.GroupKey.Name + " is null");
        }

        public IEnumerable<T> GetItems()
        {
            for (int i = 0; i < items.Count; i++)
                yield return items[i];
        }

        public override string ToString()
        {
            return table == null ? base.ToString() : table.ToString();
        }

        public List<DBGroup<T>> GroupBy(DBColumn column)
        {
            items.Sort(new DBComparer(column, ListSortDirection.Ascending));
            var groups = new List<DBGroup<T>>();
            DBGroup<T> group = null;
            foreach (T item in items)
            {
                if (group == null || !group.Value.Equals(item[column]))
                {
                    group = new DBGroup<T> { Value = item[column] };
                    groups.Add(group);
                }
                group.List.Add(item);
            }
            return groups;
        }

    }
}
