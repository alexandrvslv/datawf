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
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [InvokerGenerator(Instance = true)]
    public abstract partial class QItem : IDisposable, IComparable, IComparable<QItem>, IQItem
    {
        public static QItem Fabric(object value, DBColumn column)
        {
            switch (value)
            {
                case QItem qItem:
                    return qItem;
                case DBColumn dBColumn:
                    return new QColumn(dBColumn);
                case IInvoker invoker:
                    return new QInvoker(invoker);
                case IBetween betweenValue:
                    return new QBetween(betweenValue.MinValue(), betweenValue.MaxValue(), column);
                case string text:
                    return new QValue(text, column);
                case byte[] bytes:
                    return new QValue(bytes, column);
                case IEnumerable enumerable:
                    return new QArray(enumerable, column);
                default:
                    return new QValue(value, column);
            }
        }

        protected int order = -1;
        private string tableAlias;
        private string columnAlias;
        private QItem holder;
        private IQItemList container;
        protected QTable qTable;
        private bool refmode;

        public QItem()
        {
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public virtual IQItemList Container
        {
            get => container;
            set => container = value;
        }

        public int Order
        {
            get => order;
            set
            {
                order = value;
                //OnPropertyChanged();
            }
        }

        public virtual bool IsReference
        {
            get => refmode;
            set => refmode = value;
        }

        public string TableAlias
        {
            get => tableAlias;
            set
            {
                if (tableAlias != value)
                {
                    tableAlias = value;
                    //OnPropertyChanged();
                }
            }
        }

        public string ColumnAlias
        {
            get => columnAlias;
            set
            {
                if (columnAlias != value)
                {
                    columnAlias = value;
                    //OnPropertyChanged(nameof(Prefix));
                }
            }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public virtual QItem Holder
        {
            get => holder;
            set
            {
                if (holder != value)
                {
                    holder = value;
                    //OnPropertyChanged();
                }
            }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public virtual IQuery Query => Holder?.Query ?? Container?.Query;

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public virtual IDBTable Table
        {
            get => QTable?.Table;
            set { }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public virtual QTable QTable
        {
            get => qTable ??= Query?.GetTableByAlias(tableAlias);
            set
            {
                qTable = value;
                if (qTable != null)
                {
                    TableAlias = qTable.TableAlias;
                }
            }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public virtual IDBSchema Schema
        {
            get => Query.Schema ?? Table?.Schema;
            set { }
        }

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public virtual DBSystem System => Schema?.System ?? DBSystem.Default;

        public Type DataType => typeof(object);

        public Type TargetType => typeof(DBItem);

        public bool CanWrite => false;

        public virtual string Name
        {
            get => string.Empty;
            set { }
        }

        public abstract string Format(IDbCommand command = null);

        public virtual object GetValue<T>()
        {
            var value = GetValue((DBItem)null);
            if (value == null || value is DBColumn)
                return value;
            if (value is QItem qItem)
                value = qItem.GetValue<T>();
            return value;
        }

        public object GetValue(object target)
        {
            if (target is DBItem dbItem)
                return GetValue(dbItem);
            if (target is DBTuple turple)
                return GetValue(turple);
            throw new ArgumentOutOfRangeException(nameof(target));
        }

        public object GetValue(DBTuple tuple) => GetValue(tuple.Get(QTable));

        public abstract object GetValue(DBItem row);

        public virtual void SetValue(object target, object value)
        {
            throw new NotSupportedException();
        }

        public virtual void Dispose()
        {
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as QItem);
        }

        public int CompareTo(QItem other)
        {
            return order.CompareTo(other?.order ?? 0);
        }

        public override string ToString()
        {
            return Format();
        }

        public bool CheckItem(object item, object typedValue, CompareType comparer, IComparer comparision = null)
        {
            if (item is DBItem dBItem)
                return CheckItem(dBItem, typedValue, comparer);
            if (item is DBTuple dBTuple)
                return CheckItem(dBTuple, typedValue, comparer);
            throw new ArgumentOutOfRangeException(nameof(item));
        }

        public bool CheckItem(DBTuple item, object typedValue, CompareType comparer)
        {
            return CheckItem(item.Get(QTable), typedValue, comparer);
        }

        public virtual bool CheckItem(DBItem item, object val2, CompareType comparer)
        {
            var val1 = GetValue(item);
            if (item == null)
                return false; //comparer.Type == CompareTypes.Is && !comparer.Not;
            if (val1 == null)
                return comparer.Type == CompareTypes.Is ? !comparer.Not : val2 == null;
            else if (val2 == null)
                return comparer.Type == CompareTypes.Is ? comparer.Not : false;

            if (val1 is Enum)
                val1 = (int)val1;
            if (val2 is Enum)
                val2 = (int)val1;

            switch (comparer.Type)
            {
                //case CompareTypes.Is:
                //    return val1.Equals(DBNull.Value) ? !comparer.Not : comparer.Not;
                case CompareTypes.Equal:
                    return ListHelper.Equal(val1, val2) ? !comparer.Not : comparer.Not;
                case CompareTypes.Like:
                    var r = val2 is Regex regexValue ? regexValue : Helper.BuildLike(val2.ToString());
                    return r.IsMatch(val1.ToString()) ? !comparer.Not : comparer.Not;
                case CompareTypes.In:
                    var list = val2 as IEnumerable;
                    if (list != null)
                    {
                        foreach (object s in list)
                        {
                            object comp = s;
                            if (comp is QItem qItem)
                                comp = qItem.GetValue(item);
                            if (comp.Equals(val1) && !comparer.Not)
                                return true;
                        }
                    }
                    return comparer.Not;
                case CompareTypes.Between:
                    var between = val2 as QBetween;
                    if (between == null)
                        throw new Exception($"Expect QBetween but Get {(val2 == null ? "null" : val2.GetType().FullName)}");
                    return ListHelper.Compare(val1, between.Min.GetValue(item), (IComparer)null) >= 0
                                     && ListHelper.Compare(val1, between.Max.GetValue(item), (IComparer)null) <= 0;
                default:
                    bool f = false;
                    int rez = ListHelper.Compare(val1, val2, (IComparer)null);
                    switch (comparer.Type)
                    {
                        case CompareTypes.Greater:
                            f = rez > 0;
                            break;
                        case CompareTypes.GreaterOrEqual:
                            f = rez >= 0;
                            break;
                        case CompareTypes.Less:
                            f = rez < 0;
                            break;
                        case CompareTypes.LessOrEqual:
                            f = rez <= 0;
                            break;
                        default:
                            break;
                    }
                    return f;
            }
        }

        public virtual string CreateCommandParameter(IDbCommand command, object value, DBColumn column)
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
