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

[assembly: Invoker(typeof(QParam), "Column.Name", typeof(QParam.ColumnNameInvoker))]
namespace DataWF.Data
{
    [InvokerGenerator(Instance = true)]
    public partial class QParam : QItem, IComparable, IGroup, IQItemList
    {
        //public static bool CheckItem(DBItem item, string column, object val, CompareType comparer)
        //{
        //    object val1 = null;
        //    DBColumn dbColumn = item.Table.GetColumn(column);
        //    if (dbColumn == null)
        //    {
        //        val1 = EmitInvoker.GetValue(typeof(DBItem), column, item);
        //        return CheckItem(item, val1, val, comparer);
        //    }
        //    else
        //    {
        //        return dbColumn.CheckItem(item, val, comparer);
        //    }
        //}

        public static bool CheckItem(DBItem item, IEnumerable<QParam> parameters)
        {
            bool first = true;
            bool result = true;
            foreach (var param in parameters)
            {
                if (!first && !result && param.Logic.Type == LogicTypes.And)
                    break;
                bool check = param.CheckItem(item, null, param.Comparer);

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

        protected QItemList<QItem> items;
        protected CompareType comparer = CompareType.Undefined;
        protected LogicType logic = LogicType.Undefined;
        private bool expand = true;
        private bool? isCompaund = null;

        public QParam() : base()
        {
            items = new QItemList<QItem>(this, 2);
            //items.CollectionChanged += ParametersListChanged;
        }

        public QParam(DBColumn column) : this()
        {
            Add(new QColumn(column));
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
            Add(Fabric(value, column));
        }

        public QParam(DBTable table, string viewFilter) : this()
        {
            var query = table.Query<DBItem>(viewFilter);
            Parameters.AddRange(query.Parameters.ToArray());
        }

        [XmlIgnore, JsonIgnore]
        public QColumn LeftQColumn => LeftItem as QColumn;

        [XmlIgnore, JsonIgnore]
        public bool IsNotExpression { get; set; }

        public void SetRightValue(object value)
        {
            RightItem = QItem.Fabric(value, LeftColumn);
        }

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

        public QItem LeftItem
        {
            get => items.Count > 0 ? items[0] : null;
            set
            {
                if (LeftItem != value)
                {
                    //if (LeftItem is IQuery oldQuery)
                    //    oldQuery.Parameters.CollectionChanged -= ValueListChanged;

                    items.Insert(0, value);

                    //if (value is IQuery newQuery)
                    //    newQuery.Parameters.CollectionChanged += ValueListChanged;

                    //OnPropertyChanged();
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
                    //if (RightItem is QQuery oldQuery)
                    //    oldQuery.Parameters.CollectionChanged -= ValueListChanged;

                    items.Insert(1, value);

                    //if (value is QQuery newQuery)
                    //    newQuery.Parameters.CollectionChanged += ValueListChanged;
                }
                //OnPropertyChanged();
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
                    //OnPropertyChanged();
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
                    //OnPropertyChanged();
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public IQItem Owner => Container?.Owner;

        [XmlIgnore, JsonIgnore]
        public QParam Group
        {
            get => Container?.Owner as QParam;
            set
            {
                if (value != Group && value.Group != this && value != this)
                {
                    value.Parameters.Add(this);
                    //OnPropertyChanged();
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public string ValueLeftText => LeftQColumn?.FullName;

        [XmlIgnore, JsonIgnore]
        public bool IsCompaund
        {
            get => isCompaund ?? (items.Count > 0 && items[0] is QParam);
            set => isCompaund = value;
        }

        [XmlIgnore, JsonIgnore]
        public bool IsFilled => (RightItem != null && Comparer.Type != CompareTypes.Between)
            || (RightItem is QBetween qBetween && qBetween.Max != null);

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
            //OnPropertyChanged(sender == LeftItem ? nameof(LeftItem) : nameof(RightItem));
        }

        public bool IsColumn(DBColumn column)
        {
            return (LeftItem is QColumn lqColumn && lqColumn.Column == column) ||
                (RightItem is QColumn rqColumn && rqColumn.Column == column);
        }

        public override bool CheckItem(DBItem item, object val2, CompareType comparer)
        {
            bool result = false;

            if (!IsCompaund)
            {
                if (LeftItem == null || RightItem == null)
                {
                    result = true;
                }
                else if (!LeftItem.IsReference)
                {
                    result = RightItem.CheckItem(item, LeftItem.GetValue<DBItem>(), Comparer);
                }
                else if (!RightItem.IsReference)
                {
                    result = LeftItem.CheckItem(item, RightItem.GetValue<DBItem>(), Comparer);
                }
                else
                {
                    result = LeftItem.CheckItem(item, RightItem.GetValue(item), Comparer);
                }
            }
            else
            {
                result = CheckItem(item, Parameters.OfType<QParam>());
            }
            return result;

        }

        public bool CheckItem(DBItem item)
        {
            return CheckItem(item, null, Comparer);
        }

        public IEnumerable<T> Search<T>(IEnumerable<T> list) where T : DBItem
        {
            if (!LeftItem.IsReference)
            {
                var tempLeft = LeftItem.GetValue<T>();
                foreach (var item in list)
                {
                    if (RightItem.CheckItem(item, tempLeft, Comparer))
                        yield return item;
                }
            }
            else if (!RightItem.IsReference)
            {
                var tempRight = RightItem.GetValue<T>();
                foreach (var item in list)
                {
                    if (LeftItem.CheckItem(item, tempRight, Comparer))
                        yield return item;
                }
            }
            else
            {
                foreach (var item in list)
                {
                    if (LeftItem.CheckItem(item, RightItem.GetValue(item), Comparer))
                        yield return item;
                }
            }
        }

        public IEnumerable<DBTuple> Search<T>(IEnumerable<DBTuple> list) where T : DBItem
        {
            if (!LeftItem.IsReference)
            {
                var tempLeft = LeftItem.GetValue<T>();
                foreach (var item in list)
                {
                    if (RightItem.CheckItem(item, tempLeft, Comparer))
                        yield return item;
                }
            }
            else if (!RightItem.IsReference)
            {
                var tempRight = RightItem.GetValue<T>();
                foreach (var item in list)
                {
                    if (LeftItem.CheckItem(item, tempRight, Comparer))
                        yield return item;
                }
            }
            else
            {
                foreach (var item in list)
                {
                    if (LeftItem.CheckItem(item, RightItem.GetValue(item), Comparer))
                        yield return item;
                }
            }
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
            // OnPropertyChanged(nameof(Parameters));
            //}
        }

        public override string ToString()
        {
            if (IsCompaund)
            {
                return $"{Logic} ({string.Join(" ", items.Select(p => p.ToString()))})";
            }
            return $"{Logic} {LeftItem} {Comparer} {RightItem}";
        }

        public string FormatValue(QItem Value, IDbCommand command = null)
        {
            if (Value == null)
                return string.Empty;
            if (Value is IQuery squery && squery.Table != null)
            {
                if (squery.Columns.Count == 0)
                    squery.Column(squery.Table.PrimaryKey);
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
            string rightArg = comparer.Type == CompareTypes.Is ? "null" : FormatValue(RightItem, command);
            return string.IsNullOrEmpty(rightArg) ? string.Empty : string.Format("{0} {1} {2}", leftArg, comparer.Format(), rightArg);
        }

        public override void Dispose()
        {
            items.Dispose();
            base.Dispose();
        }

        public void Add(QItem value)
        {
            if (value is QParam param)
            {
                Parameters.Add(param);
            }
            else if (LeftItem == null)
            {
                LeftItem = value;
            }
            else if (RightItem == null)
            {
                RightItem = value;
            }
            else if (RightItem is IQItemList list)
            {
                list.Add(value);                
            }
        }
        
        public void Delete(QItem item)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IT> GetAllQItems<IT>() where IT : IQItem
        {
            return items.GetAllQItems<IT>();
        }

        public IEnumerable<IGroup> GetGroups()
        {
            if (IsCompaund)
            {
                foreach (IGroup item in items.OfType<IGroup>())
                    yield return item;
            }
            yield break;
        }

        public override object GetValue(DBItem row)
        {
            return row;
        }

        public QParam And(QParam qParam)
        {
            qParam.Logic = LogicType.And;
            Parameters.Add(qParam);
            return this;
        }

        public QParam And(Action<QParam> parameterGroup)
        {
            var qParam = new QParam() { Logic = LogicType.And };
            Parameters.Add(qParam);
            parameterGroup(qParam);
            return this;
        }

        public QParam And(IInvoker invoker, object param, QBuildParam buildParam = QBuildParam.None)
        {
            Parameters.Add(Query.CreateParam(LogicType.And, invoker, param, buildParam));
            return this;
        }

        public QParam And(IInvoker invoker, CompareType compare, object param)
        {
            Parameters.Add(Query.CreateParam(LogicType.And, invoker, compare, param));
            return this;
        }

        public QParam Or(QParam qParam)
        {
            qParam.Logic = LogicType.Or;
            Parameters.Add(qParam);
            return this;
        }

        public QParam Or(Action<QParam> parameterGroup)
        {
            var qParam = new QParam() { Logic = LogicType.Or };
            Parameters.Add(qParam);
            parameterGroup(qParam);
            return this;
        }

        public QParam Or(IInvoker invoker, object param, QBuildParam buildParam = QBuildParam.None)
        {
            Parameters.Add(Query.CreateParam(LogicType.Or, invoker, param, buildParam));
            return this;
        }

        public QParam Or(IInvoker invoker, CompareType compare, object param)
        {
            Parameters.Add(Query.CreateParam(LogicType.Or, invoker, compare, param));
            return this;
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
