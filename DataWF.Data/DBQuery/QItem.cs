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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class QItem : IDisposable, IEntryNotifyPropertyChanged, IComparable, IValued, IInvoker
    {
        protected int order = -1;
        protected string text;
        protected string alias;
        private QItem holder;

        public QItem()
        {
        }

        public QItem(string name)
        {
            this.text = name;
        }

        [JsonIgnore, XmlIgnore]
        public IEnumerable<INotifyListPropertyChanged> Containers => TypeHelper.GetContainers<INotifyListPropertyChanged>(PropertyChanged);

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public IQItemList List => (IQItemList)Containers.FirstOrDefault(p => p is IQItemList);

        public int Order
        {
            get => order;
            set
            {
                order = value;
                OnPropertyChanged(nameof(Order));
            }
        }

        public virtual string Text
        {
            get => text;
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
            get => alias;
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
        public QItem Holder
        {
            get => holder;
            set
            {
                if (holder != value)
                {
                    holder = value;
                    OnPropertyChanged(nameof(Holder));
                }
            }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public virtual IQuery Query => Holder?.Query ?? List?.Query;

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public virtual DBTable Table
        {
            get => Query?.Table;
            set { }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public virtual DBSchema Schema => Table?.Schema;

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public virtual DBSystem DBSystem => Schema?.System ?? DBSystem.Default;

        public Type DataType => typeof(DBItem);

        public Type TargetType => typeof(object);

        public bool CanWrite => false;

        public string Name
        {
            get => ToString();
            set { }
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

        public void SetValue(object target, object value)
        {
            throw new NotSupportedException();
        }

        public object GetValue(object target)
        {
            return GetValue((DBItem)target);
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

        public bool CheckItem(object item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return ListHelper.CheckItem(GetValue(item), typedValue, comparer, comparision);
        }

        public virtual string CreateParameter(IDbCommand command, object value, DBColumn column)
        {
            string name = (Table?.System?.ParameterPrefix ?? "@") + (column?.Name ?? "param");

            //TODO optimise contains/duplicate
            int i = 0;
            string param = name + i;
            while (command.Parameters.Contains(param))
                param = name + ++i;

            var parameter = Table?.System.CreateParameter(command, param, value, column);

            if (parameter == null)
            {
                parameter = command.CreateParameter();
                //parameter.DbType = DbType.String;
                parameter.Direction = ParameterDirection.Input;
                parameter.ParameterName = param;
                parameter.Value = value;
                command.Parameters.Add(parameter);
            }
            return param;
        }

    }
}
