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
                case IEnumerable enumerable:
                    return new QEnum(enumerable, column);
                default:
                    return new QValue(value, column);
            }
        }

        protected QItemList<QItem> values;
        protected CompareType comparer = CompareType.Undefined;
        protected LogicType logic = LogicType.And;
        protected QItemList<QParam> parameters;
        private bool expand = true;

        public QParam() : base()
        {
            values = new QItemList<QItem>(this);
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
        public QColumn QColumn => ValueLeft as QColumn;

        [XmlIgnore, JsonIgnore]
        public DBColumn Column
        {
            get => QColumn?.Column;
            set
            {
                if (QColumn != null)
                {
                    QColumn.Column = value;
                }
                else
                {
                    ValueLeft = new QColumn(value);
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public object Value
        {
            get => ValueRight?.GetValue(null);
            set
            {
                if (ValueRight is QValue qValue)
                {
                    qValue.Value = value;
                    OnPropertyChanged(nameof(ValueRight));
                    return;
                }
                if (comparer.Type == CompareTypes.In && value is string str)
                    value = str.Split(',');
                ValueRight = Fabric(value, Column);
            }
        }

        public QItem ValueLeft
        {
            get => values.Count > 0 ? values[0] : null;
            set
            {
                if (ValueLeft != value)
                {
                    if (ValueLeft is QQuery oldQuery)
                        oldQuery.Parameters.CollectionChanged -= ValueListChanged;

                    values.Insert(0, value);

                    if (value is QQuery newQuery)
                        newQuery.Parameters.CollectionChanged += ValueListChanged;

                    OnPropertyChanged(nameof(ValueLeft));
                }
            }
        }

        public QItem ValueRight
        {
            get => values.Count > 1 ? values[1] : null;
            set
            {
                if (ValueRight != value)
                {
                    if (ValueRight is QQuery oldQuery)
                        oldQuery.Parameters.CollectionChanged -= ValueListChanged;

                    values.Insert(1, value);

                    if (value is QQuery newQuery)
                        newQuery.Parameters.CollectionChanged += ValueListChanged;
                }
                OnPropertyChanged(nameof(ValueRight));
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
                    OnPropertyChanged(nameof(Comparer));
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
                    OnPropertyChanged("Logic");
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public QParam Group
        {
            get => List?.Owner as QParam;
            set
            {
                if (value != Group && value.Group != this && value != this)
                {
                    value.Parameters.Add(this);
                    OnPropertyChanged("Group");
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public string ValueLeftText => QColumn?.FullName;

        [XmlIgnore, JsonIgnore]
        public bool IsCompaund => parameters != null && parameters.Count > 0;

        public QItemList<QParam> Parameters
        {
            get
            {
                if (parameters == null)
                {
                    Parameters = new QItemList<QParam>(this);
                }
                return parameters;
            }
            set
            {
                parameters = value;
                if (parameters != null)
                {
                    parameters.CollectionChanged += ParametersListChanged;
                }
            }
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
            set => expand = value;
        }

        [XmlIgnore, JsonIgnore]
        public IQItemList Owner => throw new NotImplementedException();

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
            OnPropertyChanged(sender == ValueLeft ? nameof(ValueLeft) : nameof(ValueRight));
        }

        public bool IsColumn(DBColumn column)
        {
            return (ValueLeft is QColumn && ((QColumn)ValueLeft).Column == column) ||
                (ValueRight is QColumn && ((QColumn)ValueRight).Column == column);
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
            if (parameters != null && parameters.Count > 0)
            {
                return $"({string.Join(" ", parameters.Select(p => p.ToString()))})";
            }
            return $"{Logic} {ValueLeft} {Comparer} {ValueRight}";
        }

        public string FormatValue(QItem Value, IDbCommand command = null)
        {
            if (Value == null)
                return string.Empty;
            if (Value is QQuery && ((QQuery)Value).Table != null)
            {
                QQuery squery = (QQuery)Value;
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
            string v1 = FormatValue(ValueLeft, command);
            if (v1.Length == 0)
            {
                return string.Empty;
            }
            string v2 = FormatValue(ValueRight, command);
            return v2.Length == 0 ? string.Empty : string.Format("{0} {1} {2}", v1, comparer.Format(), v2);
        }

        public override void Dispose()
        {
            //_params.Clear();
            //_params = null;
            base.Dispose();
        }

        public void SetValue(QItem value)
        {
            if (ValueLeft == null)
                ValueLeft = value;
            else if (ValueRight == null)
                ValueRight = value;
            else if (ValueRight is QBetween between)
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
            return parameters;
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
            public override string Name => "Column.Name";

            public override bool CanWrite => false;

            public override string GetValue(QParam target) => target.Column?.Name;

            public override void SetValue(QParam target, string value) { }
        }
    }

}
