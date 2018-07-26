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
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace DataWF.Data
{
    [DataContract]
    public class DBGroupItem : DBItem, IGroup
    {
        private DBItem group = DBItem.EmptyItem;

        [Browsable(false)]
        public object GroupId
        {
            get { return GetValue(Table.GroupKey); }
            set { SetGroupValue(value); }
        }

        public T GetGroupValue<T>()
        {
            return GetValue<T>(Table.GroupKey);
        }

        public void SetGroupValue(object value)
        {
            if (value != null && value == PrimaryId)
            {
                throw new InvalidOperationException("Self reference validation!");
            }
            SetValue(value, Table.GroupKey);
        }

        public T GetGroupReference<T>() where T : DBGroupItem, new()
        {
            if (group == DBItem.EmptyItem)
            {
                var value = GetValue(Table.GroupKey);
                group = value == null ? null : Table.GroupKey.ReferenceTable.LoadItemById(value);
            }
            return (T)group;
        }

        public void SetGroupReference<T>(T value) where T : DBGroupItem, new()
        {
            if (value != null && value.GroupId != null && value.GroupId == PrimaryId)
            {
                throw new InvalidOperationException("Circle reference validation!");
            }
            SetGroupValue(value?.PrimaryId);
            group = value;
        }

        [Browsable(false)]
        public virtual DBGroupItem Group
        {
            get { return GetGroupReference<DBGroupItem>(); }
            set { SetGroupReference(value); }
        }

        IGroup IGroup.Group
        {
            get { return Group; }
            set { Group = value as DBGroupItem; }
        }

        [Browsable(false)]
        public string FullName
        {
            get
            {
                char separator = Path.PathSeparator;
                string buf = string.Empty;
                var row = this;
                while (row != null)
                {
                    buf = row.ToString() + (buf.Length == 0 ? string.Empty : (separator + buf));
                    row = row.Group;
                }
                return buf;
            }
        }

        [Browsable(false)]
        public bool IsExpanded
        {
            get { return GroupHelper.GetAllParentExpand(this); }
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
                if (Table.GroupKey == null)
                    return false;
                return GetSubGroups<DBGroupItem>(DBLoadParam.None).Any();
            }
        }

        public bool GroupCompare(string column, string value)
        {
            DBColumn col = Table.Columns[column];
            foreach (DBItem item in GroupHelper.GetAllParent<DBGroupItem>(this))
            {
                if (!item[col].ToString().Equals(value, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }

        public IEnumerable<T> GetParents<T>(bool addCurrent = false)
        {
            return GroupHelper.GetAllParent<T>(this, addCurrent);
        }

        public string GetParentIds()
        {
            string rez = "";
            rez = PrimaryId.ToString();
            foreach (var item in GroupHelper.GetAllParent<DBGroupItem>(this))
            {
                rez += "," + item.PrimaryId;
            }
            return rez;
        }

        public override object GetCache(DBColumn column)
        {
            if (column == Table.GroupKey)
                return group != DBItem.EmptyItem ? group : null;
            return base.GetCache(column);
        }

        public IEnumerable SelectChilds()
        {
            return Table.SelectItems(Table.GroupKey, CompareType.Equal, this);
        }

        public bool AllParentExpand()
        {
            return GroupHelper.IsExpand(this);
        }

        public IEnumerable<T> GetSubGroups<T>(DBLoadParam param) where T : DBGroupItem, new()
        {
            if (PrimaryId == null)
                return new List<T>(0);
            return GetReferencing<T>(Table, Table.GroupKey, param);
        }

        public List<T> GetSubGroupFull<T>(bool addCurrent = false) where T : DBGroupItem, new()
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

        public string GetSubGroupFullIds()
        {
            string rez = "";
            rez = PrimaryId.ToString();
            foreach (var row in GetSubGroupFull<DBGroupItem>())
                rez += "," + row.PrimaryId;
            return rez;
        }

        public IEnumerable<IGroup> GetGroups()
        {
            return GetSubGroups<DBGroupItem>(DBLoadParam.Load);
        }
    }
}

