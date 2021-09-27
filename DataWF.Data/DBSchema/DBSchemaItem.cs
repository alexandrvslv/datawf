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
using DataWF.Data;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace DataWF.Data
{
    [InvokerGenerator(Instance = true)]
    public abstract partial class DBSchemaItem : SynchronizedItem, IComparable, ICloneable, IAccessable, IDBSchemaItem
    {
        protected string name;
        protected string oldname;
        private IDBSchema schema;
        protected LocaleItem litem;
        protected AccessValue access;
        private bool isSynch;

        public DBSchemaItem()
        {
            SyncStatus = SynchronizedStatus.Load; 
        }

        public DBSchemaItem(string name)
        {
            if (name != null)
            {
                int index = name.LastIndexOf(".", StringComparison.Ordinal);
                if (index > 0)
                    name = name.Substring(index);
            }
            this.name = name;
        }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public virtual IDBSchema Schema
        {
            get => schema;
            set
            {
                if (schema == value)
                    return;
                schema = value;
                litem = null;
            }
        }

        [Browsable(false), Category("Policy"), XmlIgnore, JsonIgnore]
        public virtual string AccessorName => DisplayName;

        IAccessValue IAccessable.Access
        {
            get => Access;
            set => Access = (AccessValue)value;
        }

        [Browsable(false), Category("Policy"), XmlIgnore, JsonIgnore]
        public virtual AccessValue Access
        {
            get { return access ?? (access = new AccessValue(AccessValue.Provider.GetGroups())); }
            set
            {
                access = value;
                OnPropertyChanged();
            }
        }

        [Browsable(false), Category("Naming"), XmlIgnore, JsonIgnore]
        public virtual string FullName => Name;

        [Browsable(false), Category("Naming"), XmlIgnore, JsonIgnore]
        public LocaleItem LocaleInfo
        {
            get
            {
                var locCategory = GetLocalizeCategory();
                var locName = Name;
                if (string.IsNullOrEmpty(locCategory) || string.IsNullOrEmpty(locName))
                    return null;
                if (litem != null && (!(litem.Category.Name?.Equals(locCategory, StringComparison.Ordinal) ?? false)
                    || !(litem.Name?.Equals(locName, StringComparison.Ordinal) ?? false)))
                    litem = null;
                return litem ?? (litem = Locale.Instance.GetItem(locCategory, locName));
            }
            //set
            //{
            //    if (litem == value)
            //        return;
            //    litem = value;
            //}
        }

        [Category("Naming")]
        public virtual string DisplayName
        {
            get => LocaleInfo?.Value ?? Name;
            set
            {
                if (LocaleInfo != null)
                    LocaleInfo.Value = value;
            }
        }

        [Category("Naming"), Required]
        public virtual string Name
        {
            get => name;
            set
            {
                if (string.Equals(value, name, StringComparison.Ordinal))
                    return;
                if (string.Equals(oldname, value, StringComparison.Ordinal))
                    oldname = null;
                else if (oldname == null)
                    oldname = name;
                name = value;
                if (litem != null)
                {
                    litem.Name = Name;
                }
                OnPropertyChanged(DDLType.Alter);
            }
        }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public string OldName
        {
            get => oldname;
            set => oldname = value;
        }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public virtual bool IsSynch
        {
            get => isSynch;
            set
            {
                if (isSynch == value)
                    return;
                isSynch = value;
            }
        }

        public virtual string GetLocalizeCategory()
        {
            return Schema?.Name ?? Name;
        }

        public abstract string FormatSql(DDLType ddlType, bool dependency = false);

        public void OnPropertyChanged(DDLType type = DDLType.None, [CallerMemberName] string propertyName = "")
        {
            base.OnPropertyChanged(propertyName);
            if (type != DDLType.None)
            {
                ((DBSchema)Schema)?.OnChanged(this, type);
            }
        }

        public override string ToString()
        {
            return LocaleInfo?.Value ?? name;
        }

        #region IComparable Members

        public virtual int CompareTo(object obj)
        {
            return string.Compare(name, ((DBSchemaItem)obj).name, StringComparison.Ordinal);
        }

        #endregion

        #region ICloneable Members

        public abstract object Clone();

        #endregion
    }
}
