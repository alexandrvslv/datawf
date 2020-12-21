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
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

[assembly: Invoker(typeof(QParam), nameof(QParam.Group), typeof(QParam.GroupInvoker))]
[assembly: Invoker(typeof(QParam), "Column.Name", typeof(QParam.ColumnNameInvoker))]
namespace DataWF.Data
{
    public class QParam : QItem, IComparable, IGroup, IQItemList
    {
        public static QItem Fabric(object value, DBColumn column)
        {
            switch (value)
            {
                case QItem qItem:
                    return qItem;
                case DBColumn dBColumn:
                    return new QColumn(dBColumn);
                case DateInterval dateInterval:
                    if (dateInterval.IsEqual())
                    {
                        return new QBetween(dateInterval.Min.Date, dateInterval.Min.Date.AddDays(1), column);
                    }
                    else
                    {
                        return new QBetween(dateInterval.Min, dateInterval.Max, column);
                    }
                case IInvoker invoker:
                    return new QReflection(invoker);
                case string text:
                    return new QValue(text, column);
                case IEnumerable enumerable:
                    return new QEnum(enumerable, column);
                default:
                    return new QValue(value, column);
            }
        }

        public static bool CheckItem(DBItem item, string column, object val, CompareType comparer)
        {
            object val1 = null;
            DBColumn dbColumn = item.Table.ParseColumn(column);
            if (dbColumn == null)
            {
                val1 = EmitInvoker.GetValue(typeof(DBItem), column, item);
                return CheckItem(item, val1, val, comparer);
            }
            else
            {
                return dbColumn.CheckItem(item, val, comparer);
            }
        }

        public static bool CheckItem(DBItem item, IEnumerable<QParam> parameters)
        {
            bool first = true;
            bool result = true;
            foreach (var param in parameters)
            {
                if (!first && !result && param.Logic.Type == LogicTypes.And)
                    break;
                bool check = param.CheckItem(item);

                if (first)
                {
                    result = check;
                    first = false;
                }
                else if (param.Logic.Type == LogicTypes.Or)
                {
                    result |= param.Logic.Not ? !check : check;
                }
                else if (param.Logic.Type == LogicTypes.And)
                {
                    result &= param.Logic.Not ? !check : check;
                }
            }
            return result;
        }

        public static bool CheckItem(DBItem item, object val1, object val2, CompareType comparer)
        {
            if (item == null)
                return false;
            if (val1 == null)
                return comparer.Type == CompareTypes.Is ? !comparer.Not : val2 == null;
            else if (val2 == null)
                return comparer.Type == CompareTypes.Is ? comparer.Not : false;
            if (val1 is QQuery query1)
                val1 = item.Table.SelectValues(item, query1, comparer);
            if (val2 is QQuery query2)
                val2 = item.Table.SelectValues(item, query2, comparer);
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
                    var r = val2 is Regex ? (Regex)val2 : Helper.BuildLike(val2.ToString());
                    return r.IsMatch(val1.ToString()) ? !comparer.Not : comparer.Not;
                case CompareTypes.In:
                    if (val2 is string)
                        val2 = val2.ToString().Split(QQuery.CommaSeparator, StringSplitOptions.RemoveEmptyEntries);
                    var list = val2 as IEnumerable;
                    if (list != null)
                    {
                        foreach (object s in list)
                        {
                            object comp = s;
                            if (comp is QItem)
                                comp = ((QItem)comp).GetValue(item);
                            if (comp is string)
                                comp = ((string)comp).Trim(' ', '\'');
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

        protected QItemList<QItem> items;
        protected CompareType comparer = CompareType.Undefined;
        protected LogicType logic = LogicType.And;
        private bool expand = true;

        public QParam() : base()
        {
            items = new QItemList<QItem>(this, 2);
            items.CollectionChanged += ParametersListChanged;
        }

        public QParam(DBColumn column) : this()
        {
            SetValue(new QColumn(column));
        }

        public QParam(DBColumn column, object value) : this(LogicType.And, column, CompareType.Equal, value)
        {
        }

        public QParam(DBColumn column, CompareType comparer, object value) : this(LogicType.And, column, comparer, value)
        {
        }

        public QParam(LogicType logicType, DBColumn column, CompareType comparer, object value) : this(column)
        {
            Logic = logicType;
            Comparer = comparer;
            SetValue(Fabric(value, column));
        }

        public QParam(DBTable table, string viewFilter) : this()
        {
            using (var query = new QQuery(viewFilter, table))
            {
                Parameters.AddRange(query.Parameters.ToArray());
            }
        }

        [XmlIgnore, JsonIgnore]
        public QColumn LeftQColumn => LeftItem as QColumn;

        [XmlIgnore, JsonIgnore]
        public DBColumn LeftColumn
        {
            get => LeftQColumn?.Column;
            set
            {
                if (LeftQColumn != null)
                {
                    LeftQColumn.Column = value;
                }
                else
                {
                    LeftItem = new QColumn(value);
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public QColumn RightQColumn => RightItem as QColumn;

        [XmlIgnore, JsonIgnore]
        public DBColumn RightColumn
        {
            get => RightQColumn?.Column;
            set
            {
                if (RightQColumn != null)
                {
                    RightQColumn.Column = value;
                }
                else
                {
                    RightItem = new QColumn(value);
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public object LeftValue
        {
            get => LeftItem?.GetValue(null);
            set
            {
                if (LeftItem is QValue qValue)
                {
                    qValue.Value = value;
                    OnPropertyChanged(nameof(LeftItem));
                    return;
                }
                if (comparer.Type == CompareTypes.In && value is string str)
                    value = str.Split(',');
                LeftItem = Fabric(value, LeftColumn);
            }
        }

        [XmlIgnore, JsonIgnore]
        public object RightValue
        {
            get => RightItem?.GetValue(null);
            set
            {
                if (RightItem is QValue qValue)
                {
                    qValue.Value = value;
                    OnPropertyChanged(nameof(RightItem));
                    return;
                }
                if (comparer.Type == CompareTypes.In && value is string str)
                    value = str.Split(',');
                RightItem = Fabric(value, LeftColumn);
            }
        }

        public bool LeftIsColumn => LeftItem?.GetType() == typeof(QColumn);

        public bool RightIsColumn => RightItem?.GetType() == typeof(QColumn);

        public QItem LeftItem
        {
            get => items.Count > 0 ? items[0] : null;
            set
            {
                if (LeftItem != value)
                {
                    if (LeftItem is QQuery oldQuery)
                        oldQuery.Parameters.CollectionChanged -= ValueListChanged;

                    items.Insert(0, value);

                    if (value is QQuery newQuery)
                        newQuery.Parameters.CollectionChanged += ValueListChanged;

                    OnPropertyChanged();
                }
            }
        }

        public QItem RightItem
        {
            get => items.Count > 1 ? items[1] : null;
            set
            {
                if (RightItem != value)
                {
                    if (RightItem is QQuery oldQuery)
                        oldQuery.Parameters.CollectionChanged -= ValueListChanged;

                    items.Insert(1, value);

                    if (value is QQuery newQuery)
                        newQuery.Parameters.CollectionChanged += ValueListChanged;
                }
                OnPropertyChanged();
            }
        }

        public CompareType Comparer
        {
            get => comparer;
            set
            {
                if (!comparer.Equals(value))
                {
                    comparer = value;
                    OnPropertyChanged();
                }
            }
        }

        public LogicType Logic
        {
            get => logic;
            set
            {
                if (!logic.Equals(value))
                {
                    logic = value;
                    CheckGroupLogic();
                    OnPropertyChanged();
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public QParam Group
        {
            get => List?.Container as QParam;
            set
            {
                if (value != Group && value.Group != this && value != this)
                {
                    value.Parameters.Add(this);
                    OnPropertyChanged();
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public string ValueLeftText => LeftQColumn?.FullName;

        [XmlIgnore, JsonIgnore]
        public bool IsCompaund => items.Count > 0 && items[0] is QParam;

        public QItemList<QItem> Parameters
        {
            get => items;
        }

        [XmlIgnore, JsonIgnore]
        public bool IsExpanded => GroupHelper.GetAllParentExpand(this);

        IGroup IGroup.Group
        {
            get => Group;
            set => Group = (QParam)value;
        }

        public bool Expand
        {
            get => expand;
            set
            {
                if (expand != value)
                {
                    expand = value;
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public IQItemList Container => throw new NotImplementedException();

        public bool IsDefault { get; set; }

        public void CheckGroupLogic()
        {
            if (Group != null)
            {
                if (Group.Parameters.IsFirst(this))
                    Group.Logic = this.Logic;
            }
        }

        private void ValueListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(sender == LeftItem ? nameof(LeftItem) : nameof(RightItem));
        }

        public bool IsColumn(DBColumn column)
        {
            return (LeftIsColumn && ((QColumn)LeftItem).Column == column) ||
                (RightIsColumn && ((QColumn)RightItem).Column == column);
        }

        public bool CheckItem(DBItem item)
        {
            bool result = false;
            if (!IsCompaund)
            {
                if (LeftItem == null || RightItem == null)
                {
                    result = true;
                }
                else if (LeftIsColumn)
                {
                    result = LeftColumn.CheckItem(item, RightItem.GetValue(item), Comparer);
                }
                else if (RightIsColumn)
                {
                    result = RightColumn.CheckItem(item, LeftItem.GetValue(item), Comparer);
                }
                else
                {
                    result = CheckItem(item, LeftItem.GetValue(item), RightItem.GetValue(item), Comparer);
                }
            }
            else
            {
                result = CheckItem(item, Parameters.OfType<QParam>());
            }
            return result;
        }



        public void ParametersListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //if (e.ListChangedType == ListChangedType.ItemAdded)
            //{
            //    parameters[e.NewIndex].Group = this;
            //}
            //else if (e.ListChangedType == ListChangedType.ItemDeleted)
            //{
            //    if (e.NewIndex >= 0)
            //        parameters[e.NewIndex].Group = null;
            //}
            //else
            //{
            OnPropertyChanged(nameof(Parameters));
            //}
        }

        public override string ToString()
        {
            if (IsCompaund)
            {
                return $"({string.Join(" ", items.Select(p => p.ToString()))})";
            }
            return $"{Logic} {LeftItem} {Comparer} {RightItem}";
        }

        public string FormatValue(QItem Value, IDbCommand command = null)
        {
            if (Value == null)
                return string.Empty;
            if (Value is QQuery squery && squery.Table != null)
            {
                if (squery.Columns.Count == 0)
                    squery.Columns.Add(new QColumn(squery.Table.PrimaryKey));
                return "(" + squery.Format(command) + ")";
            }
            else if (Value is QExpression)
                return "(" + Value.Format(command) + ")";
            else
                return Value.Format(command);
        }

        public override string Format(IDbCommand command = null)
        {
            if (IsCompaund)
            {
                string buf = string.Empty;
                foreach (QParam param in Parameters)
                {
                    string subparam = param.Format(command);
                    if (subparam.Length != 0)
                    {
                        buf += (buf.Length > 0 ? param.Logic.Format() + " " : string.Empty) + subparam + " ";
                    }
                }
                if (buf.Length > 0)
                    buf = "(" + buf + ")";
                return buf;
            }
            string leftArg = FormatValue(LeftItem, command);
            if (string.IsNullOrEmpty(leftArg))
            {
                return string.Empty;
            }
            string rightArg = FormatValue(RightItem, command);
            return string.IsNullOrEmpty(rightArg) ? string.Empty : string.Format("{0} {1} {2}", leftArg, comparer.Format(), rightArg);
        }

        public override void Dispose()
        {
            items.Dispose();
            base.Dispose();
        }

        public void SetValue(QItem value)
        {
            if (LeftItem == null)
                LeftItem = value;
            else if (RightItem == null)
                RightItem = value;
            else if (RightItem is QBetween between)
            {
                if (between.Min == null)
                    between.Min = value;
                else if (between.Max == null)
                    between.Max = value;
            }
        }

        public void Delete(QItem item)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IGroup> GetGroups()
        {
            if (IsCompaund)
            {
                foreach (IGroup item in items)
                    yield return item;

            }
            yield break;
        }

        public override object GetValue(DBItem row)
        {
            return row;
        }

        public class GroupInvoker : Invoker<QParam, QParam>
        {
            public static readonly GroupInvoker Instance = new GroupInvoker();
            public override string Name => nameof(QParam.Group);

            public override bool CanWrite => true;

            public override QParam GetValue(QParam target) => target.Group;

            public override void SetValue(QParam target, QParam value) => target.Group = value;
        }

        public class ColumnNameInvoker : Invoker<QParam, string>
        {
            public static readonly ColumnNameInvoker Instance = new ColumnNameInvoker();
            public override string Name => "LeftColumn.Name";

            public override bool CanWrite => false;

            public override string GetValue(QParam target) => target.LeftColumn?.Name;

            public override void SetValue(QParam target, string value) { }
        }
    }

}
