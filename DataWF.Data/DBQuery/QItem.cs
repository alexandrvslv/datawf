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
using System;
using System.ComponentModel;
using DataWF.Data;
using DataWF.Common;
using System.Data;

namespace DataWF.Data
{
    public class QItem : IDisposable, INotifyPropertyChanged, IComparable
    {
        [NonSerialized]
        protected IQItemList _list;
        [NonSerialized()]
        protected IQuery query;
        [DefaultValue(-1)]
        protected int order = -1;
        [DefaultValue(null)]
        protected string text;
        [DefaultValue(null)]
        protected string alias;

        public QItem()
        {
        }

        public QItem(string name)
        {
            this.text = name;
        }

        [Browsable(false)]
        public IQItemList List
        {
            get { return _list; }
            set { _list = value; }
        }

        public int Order
        {
            get { return order; }
            set { order = value; }
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

        [Browsable(false)]
        public IQuery Query
        {
            get { return query; }
            set
            {
                if (query != value)
                {
                    query = value;
                }
            }
        }

        [Browsable(false)]
        public virtual DBTable Table
        {
            get { return query == null ? null : query.Table; }
            set { }
        }

        public virtual string Format(IDbCommand command = null)
        {
            return text;
        }

        public virtual object GetValue(DBItem row = null)
        {
            return text;
        }

        public virtual void Dispose()
        {
            query = null;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string pname)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(pname));
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
