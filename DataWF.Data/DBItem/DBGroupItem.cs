﻿//  The MIT License (MIT)
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
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [DataContract]
    public class DBGroupItem : DBItem, IGroup
    {
        private DBItem group;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public object GroupId
        {
            get => GetValue(Table.GroupKey);
            set => SetGroupValue(value);
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public virtual DBGroupItem Group
        {
            get => GetGroupReference<DBGroupItem>();
            set => SetGroupReference(value);
        }

        IGroup IGroup.Group
        {
            get => Group;
            set => Group = value as DBGroupItem;
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
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
                    if (row.Group == row || row.Group?.Group == row)
                    {
                        buf = "Self Reference" + buf;
                        row = null;
                    }
                    row = row.Group;
                }
                return buf;
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public bool IsExpanded => GroupHelper.GetAllParentExpand(this);

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public bool Expand
        {
            get => (state & DBItemState.Expand) == DBItemState.Expand;
            set
            {
                if (Expand != value)
                {
                    state = value ? state | DBItemState.Expand : state & ~DBItemState.Expand;
                    OnPropertyChanged();
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public bool IsCompaund
        {
            get
            {
                if (Table.GroupKey == null)
                    return false;
                return GetSubGroups(DBLoadParam.None).Any();
            }
        }

        public T? GetGroupValueNullable<T>() where T : struct
        {
            return GetValueNullable<T>(Table.GroupKey);
        }

        public T GetGroupValue<T>()
        {
            return GetValue<T>(Table.GroupKey);
        }

        public void SetGroupValueNullable<T>(T? value) where T : struct
        {
            if (value != null && value.Equals(PrimaryId))
            {
                throw new InvalidOperationException("Self reference detected!");
            }
            SetValueNullable(value, Table.GroupKey);
            group = null;
        }

        public void SetGroupValue<T>(T value)
        {
            if (value != null && value.Equals(PrimaryId))
            {
                throw new InvalidOperationException("Self reference detected!");
            }
            SetValue(value, Table.GroupKey);
            group = null;
        }

        public T GetGroupReference<T>(DBLoadParam loadParam = DBLoadParam.Load | DBLoadParam.Referencing) where T : DBGroupItem, new()
        {
            GetReference(Table.GroupKey, ref group, loadParam);
            //Check recursion
            if (group == this)
                return null;
            return (T)group;
        }

        public void SetGroupReference<T>(T value) where T : DBGroupItem, new()
        {
            if (value != null && value.GroupId != null && value.GroupId == PrimaryId)
            {
                throw new InvalidOperationException("Circle reference detected!");
            }
            SetReference<T>((T)(group = value), Table.GroupKey);
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
            string rez = PrimaryId.ToString();
            foreach (var item in GroupHelper.GetAllParent<DBGroupItem>(this))
            {
                rez += "," + item.PrimaryId;
            }
            return rez;
        }

        //public override object GetCache(DBColumn column)
        //{
        //    if (column == Table.GroupKey)
        //        return group != DBItem.EmptyItem ? group : null;
        //    return base.GetCache(column);
        //}

        public IEnumerable SelectChilds()
        {
            return Table.SelectItems(Table.GroupKey, CompareType.Equal, this);
        }

        public bool AllParentExpand()
        {
            return GroupHelper.IsExpand(this);
        }

        public IEnumerable<DBGroupItem> GetSubGroups(DBLoadParam param)
        {
            if (PrimaryId == null)
                return new List<DBGroupItem>(0);
            return GetReferencing(Table, Table.GroupKey, param).Cast<DBGroupItem>();
        }

        public IEnumerable<T> GetSubGroups<T>(DBLoadParam param) where T : DBGroupItem, new()
        {
            if (PrimaryId == null)
                return new List<T>(0);
            return GetReferencing<T>((DBTable<T>)Table, Table.GroupKey, param);
        }

        public List<DBGroupItem> GetSubGroupFull(bool addCurrent = false)
        {
            var buf = GetSubGroups(DBLoadParam.None);
            var rez = new List<DBGroupItem>();
            if (addCurrent)
                rez.Add(this);
            rez.AddRange(buf);
            foreach (var row in buf)
                rez.AddRange(row.GetSubGroupFull());
            return rez;
        }

        public string GetSubGroupFullIds()
        {
            string rez = "";
            rez = PrimaryId.ToString();
            foreach (var row in GetSubGroupFull())
                rez += "," + row.PrimaryId;
            return rez;
        }

        public IEnumerable<IGroup> GetGroups()
        {
            return GetSubGroups(DBLoadParam.Load);
        }
    }
}

