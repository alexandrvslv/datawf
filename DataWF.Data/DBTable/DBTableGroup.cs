/*
 DBTableGroup.cs
 
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
using System.Linq;

namespace DataWF.Data
{
    public class DBTableGroup : DBSchemaItem, IComparable, IGroup
    {
        [NonSerialized()]
        protected DBTableGroup _cache;
        protected string parent;
        [NonSerialized()]
        protected bool expand = true;

        public DBTableGroup()
            : base()
        {
        }

        public DBTableGroup(string code)
            : base(code)
        {
        }

        [Category("Naming")]
        public override string FullName
        {
            get
            {
                return string.Format("{0}.{1}",
                Schema == null ? string.Empty : Schema.Name,
                name);
            }
        }

        [Browsable(false)]
        public string GroupName
        {
            get { return parent; }
            set
            {
                if (parent == value)
                    return;
                parent = value;
                OnPropertyChanged(nameof(GroupName), false);
            }
        }

        [Browsable(false)]
        public bool IsExpanded
        {
            get { return GroupHelper.IsExpand(this); }
        }

        [Category("Group")]
        public DBTableGroup Group
        {
            get
            {
                if (parent == null)
                    return null;
                if (_cache == null)
                    _cache = Schema == null ? null : Schema.TableGroups[parent];
                return _cache;
            }
            set
            {
                if (value == null || (value.Group != this && value != this))
                {
                    parent = value == null ? null : value.name;
                    _cache = value;
                    OnPropertyChanged(nameof(Group), false);
                }
            }
        }

        [Browsable(false), Category("Group")]
        public IEnumerable<DBTableGroup> Childs
        {
            get
            {
                if (Schema == null)
                    yield break;
                foreach (DBTableGroup group in Schema.TableGroups)
                {
                    if (group.Group == this)
                        yield return group;
                }
            }
        }

        public IEnumerable<DBTable> GetTables()
        {
            if (Schema == null)
                yield break;
            foreach (DBTable table in Schema.Tables)
            {
                if (table.Group == this)
                    yield return table;
            }
        }

        #region IComparable Members

        int IComparable.CompareTo(object obj)
        {
            DBTableGroup objC = obj as DBTableGroup;
            return string.Compare(this.name, objC.name);
        }

        public override string FormatSql(DDLType ddlType)
        {
            return null;
        }

        public override object Clone()
        {
            return new DBTableGroup()
            {
                Name = Name,
                GroupName = GroupName
            };
        }

        #endregion

        #region IGroupable implementation
        IGroup IGroup.Group
        {
            get { return Group; }
            set { Group = value as DBTableGroup; }
        }

        [Browsable(false)]
        public bool Expand
        {
            get { return expand; }
            set { expand = value; }
        }

        [Browsable(false)]
        public bool IsCompaund
        {
            get { return GetTables().Any(); }
        }
        #endregion
    }
}

