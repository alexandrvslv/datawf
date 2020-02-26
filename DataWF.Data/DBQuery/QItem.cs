/*
 QItem.cs
 
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
using System.Data;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class QItem : IDisposable, IContainerNotifyPropertyChanged, IComparable, IValued
    {
        protected int order = -1;
        protected string text;
        protected string alias;

        public QItem()
        {
        }

        public QItem(string name)
        {
            this.text = name;
        }

        [JsonIgnore, XmlIgnore]
        public IEnumerable<INotifyListPropertyChanged> Containers => TypeHelper.GetContainers(PropertyChanged);

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public IQItemList List
        {
            get { return (IQItemList)Containers.FirstOrDefault(p => p is IQItemList); }
        }

        public int Order
        {
            get { return order; }
            set
            {
                order = value;
                OnPropertyChanged(nameof(Order));
            }
        }

        public virtual string Text
        {
            get { return text; }
            set
            {
                if (text == value)
                    return;
                text = value;
                OnPropertyChanged(nameof(Text));
            }
        }

        public string Alias
        {
            get { return alias; }
            set
            {
                if (alias != value)
                {
                    alias = value;
                    OnPropertyChanged(nameof(Alias));
                }
            }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public virtual IQuery Query
        {
            get { return List?.Query; }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public virtual DBTable Table
        {
            get { return Query?.Table; }
            set { }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public virtual DBSchema Schema
        {
            get { return Table?.Schema; }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public virtual DBSystem DBSystem
        {
            get { return Schema?.System ?? DBSystem.Default; }
        }

        public virtual string Format(IDbCommand command = null)
        {
            return text;
        }

        public object GetValue()
        {
            return GetValue(null);
        }

        public virtual object GetValue(DBItem row)
        {
            return text;
        }

        public virtual void Dispose()
        {
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public int CompareTo(object obj)
        {
            return order.CompareTo(((QItem)obj).order);
        }

        public override string ToString()
        {
            return Format();
        }


    }
}
