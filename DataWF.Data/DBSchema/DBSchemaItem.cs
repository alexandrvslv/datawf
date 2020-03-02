/*
 DBSchemaItem.cs
 
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
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;

namespace DataWF.Data
{
    public abstract class DBSchemaItem : IContainerNotifyPropertyChanged, IComparable, ICloneable, IAccessable, IDBSchemaItem
    {
        protected string name;
        protected string oldname;
        protected DBSchema schema;
        protected LocaleItem litem;
        protected AccessValue access;
        private bool isSynchronized;

        public DBSchemaItem()
        {
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
        public IEnumerable<INotifyListPropertyChanged> Containers => TypeHelper.GetContainers(PropertyChanged);

        [Browsable(false), XmlIgnore, JsonIgnore]
        public virtual DBSchema Schema
        {
            get { return schema; }
            set
            {
                if (schema == value)
                    return;
                schema = value;
                litem = null;
            }
        }

        IAccessValue IAccessable.Access { get => Access; set => Access = (AccessValue)value; }

        [Browsable(false), Category("Policy"), XmlIgnore, JsonIgnore]
        public virtual AccessValue Access
        {
            get { return access ?? (access = new AccessValue(AccessValue.Groups)); }
            set { access = value; }
        }

        [Browsable(false), Category("Naming"), XmlIgnore, JsonIgnore]
        public virtual string FullName
        {
            get { return Name; }
        }

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

        [Category("Naming"), XmlIgnore]
        public string DisplayName
        {
            get { return LocaleInfo?.Value ?? Name; }
            set
            {
                if (LocaleInfo != null)
                    LocaleInfo.Value = value;
            }
        }

        [Category("Naming"), Required]
        public virtual string Name
        {
            get { return name; }
            set
            {
                if (value == name)
                    return;
                if (oldname == value)
                    oldname = null;
                else if (oldname == null)
                    oldname = name;
                name = value;
                if (litem != null)
                {
                    litem.Name = Name;
                }
                OnPropertyChanged(nameof(Name), DDLType.Alter);
            }
        }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public string OldName
        {
            get { return oldname; }
            set { oldname = value; }
        }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public virtual bool IsSynchronized
        {
            get { return isSynchronized; }
            set
            {
                if (isSynchronized == value)
                    return;
                isSynchronized = value;
            }
        }

        public virtual string GetLocalizeCategory()
        {
            return Schema?.Name ?? Name;
        }

        public abstract string FormatSql(DDLType ddlType);

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName, DDLType type = DDLType.Default)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (type != DDLType.Default)
                DBService.OnDBSchemaChanged(this, type);
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

        [Invoker(typeof(DBSchemaItem), nameof(DBSchemaItem.Name))]
        public class NameInvoker<T> : Invoker<T, string> where T : DBSchemaItem
        {
            public static readonly NameInvoker<T> Instance = new NameInvoker<T>();

            public override string Name => nameof(DBSchemaItem.Name);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.Name;

            public override void SetValue(T target, string value) => target.Name = value;
        }

        [Invoker(typeof(DBSchemaItem), nameof(DBSchemaItem.DisplayName))]
        public class DisplayNameInvoker<T> : Invoker<T, string> where T : DBSchemaItem
        {
            public static readonly DisplayNameInvoker<T> Instance = new DisplayNameInvoker<T>();

            public override string Name => nameof(DBSchemaItem.DisplayName);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.DisplayName;

            public override void SetValue(T target, string value) => target.DisplayName = value;
        }

        [Invoker(typeof(DBSchemaItem), nameof(DBSchemaItem.OldName))]
        public class OldNameInvoker<T> : Invoker<T, string> where T : DBSchemaItem
        {
            public static readonly OldNameInvoker<T> Instance = new OldNameInvoker<T>();

            public override string Name => nameof(DBSchemaItem.OldName);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.OldName;

            public override void SetValue(T target, string value) => target.OldName = value;
        }
    }
}
