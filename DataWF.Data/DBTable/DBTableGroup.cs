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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBTableGroup : DBSchemaItem, IComparable, IGroup
    {
        protected DBTableGroup group;
        protected string groupName;
        protected bool expand = true;

        public DBTableGroup()
            : base()
        {
        }

        public DBTableGroup(string code)
            : base(code)
        {
        }

        [XmlIgnore, JsonIgnore, Category("Naming")]
        public override string FullName
        {
            get { return $"{Schema?.Name}.{Name}"; }
        }

        [Browsable(false)]
        public string GroupName
        {
            get { return groupName; }
            set
            {
                if (groupName == value)
                    return;
                groupName = value;
                OnPropertyChanged(nameof(GroupName));
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public bool IsExpanded
        {
            get { return GroupHelper.IsExpand(this); }
        }

        [XmlIgnore, JsonIgnore, Category("Group")]
        public DBTableGroup Group
        {
            get { return group ?? (group = Schema?.TableGroups[groupName]); }
            set
            {
                if (value == null || (value.Group != this && value != this))
                {
                    groupName = value?.name;
                    group = value;
                    OnPropertyChanged(nameof(Group));
                }
            }
        }

        [Browsable(false), Category("Group")]
        public IEnumerable<DBTableGroup> GetChilds()
        {
            return Schema?.TableGroups.GetByGroup(Name);
        }

        public IEnumerable<IGroup> GetGroups()
        {
            return GetChilds(); ;
        }

        public IEnumerable<DBTable> GetTables()
        {
            return Schema?.Tables.GetByGroup(Name);
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

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public bool Expand
        {
            get { return expand; }
            set { expand = value; }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public bool IsCompaund
        {
            get { return GetTables().Any(); }
        }
        #endregion

        [Invoker(typeof(DBTableGroup), nameof(GroupName))]
        public class GroupNameInvoker : Invoker<DBTableGroup, string>
        {
            public static readonly GroupNameInvoker Instance = new GroupNameInvoker();
            public override string Name => nameof(DBTableGroup.GroupName);

            public override bool CanWrite => true;

            public override string GetValue(DBTableGroup target) => target.GroupName;

            public override void SetValue(DBTableGroup target, string value) => target.GroupName = value;
        }

        [Invoker(typeof(DBTableGroup), nameof(DBTableGroup.Expand))]
        public class ExpandInvoker : Invoker<DBTableGroup, bool>
        {
            public static readonly ExpandInvoker Instance = new ExpandInvoker();
            public override string Name => nameof(DBTable.IsCaching);

            public override bool CanWrite => true;

            public override bool GetValue(DBTableGroup target) => target.Expand;

            public override void SetValue(DBTableGroup target, bool value) => target.Expand = value;
        }       
    }
}

