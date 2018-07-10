/*
 QParam.cs
 
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
using DataWF.Common;
using System.Collections.Generic;
using System.Data;
using System.Collections;
using System.Text;
using System.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;
using System.Collections.Specialized;

namespace DataWF.Data
{
    public class QParam : QItem, IComparable, IGroup, IQItemList
    {
        public static QItem Fabric(object value, DBColumn column)
        {
            if (value is QItem)
            {
                return (QItem)value;
            }
            else if (value is DBColumn)
            {
                return new QColumn((DBColumn)value);
            }
            else if (value is DateInterval)
            {
                if (((DateInterval)value).IsEqual())
                {
                    return new QBetween(((DateInterval)value).Min.Date, ((DateInterval)value).Min.Date.AddDays(1), column);
                }
                else
                {
                    return new QBetween(((DateInterval)value).Min, ((DateInterval)value).Max, column);
                }
            }
            else if (value is IList)
            {
                return new QEnum((IList)value, column);
            }
            else
            {
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

        public QParam(LogicType logicType, DBColumn column, CompareType compareType, object value) : this(column)
        {
            Logic = logicType;
            Comparer = compareType;
            SetValue(Fabric(value, column));
        }

        public QParam(DBTable table, string viewFilter) : this()
        {
            using (var query = new QQuery(viewFilter, table))
            {
                Parameters.AddRange(query.Parameters.ToArray());
            }
        }

        public void CheckGroupLogic()
        {
            if (Group != null)
            {
                if (Group.Parameters.IsFirst(this))
                    Group.Logic = this.Logic;
            }
        }

        public QColumn QColumn
        {
            get { return ValueLeft as QColumn; }
        }

        public DBColumn Column
        {
            get { return QColumn?.Column; }
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

        public object Value
        {
            get { return ValueRight?.GetValue(null); }
            set
            {
                if (ValueRight is QValue)
                {
                    ((QValue)ValueRight).Value = value;
                    OnPropertyChanged(nameof(ValueRight));
                    return;
                }
                if (comparer.Type == CompareTypes.In && value is string)
                    value = ((string)value).Split(',');
                ValueRight = Fabric(value, Column);
            }
        }

        public QItem ValueLeft
        {
            get { return values.Count > 0 ? values[0] : null; }
            set
            {
                if (ValueLeft != value)
                {
                    if (ValueLeft is QQuery)
                        ((QQuery)ValueLeft).Parameters.CollectionChanged -= ValueListChanged;

                    values.Insert(0, value);

                    if (value is QQuery)
                        ((QQuery)value).Parameters.CollectionChanged += ValueListChanged;

                    OnPropertyChanged(nameof(ValueLeft));
                }
            }
        }

        public QItem ValueRight
        {
            get { return values.Count > 1 ? values[1] : null; }
            set
            {
                if (ValueRight != value)
                {
                    if (ValueRight is QQuery)
                        ((QQuery)ValueRight).Parameters.CollectionChanged -= ValueListChanged;

                    values.Insert(1, value);

                    if (value is QQuery)
                        ((QQuery)value).Parameters.CollectionChanged += ValueListChanged;
                }
                OnPropertyChanged(nameof(ValueRight));
            }
        }

        private void ValueListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(sender == ValueLeft ? nameof(ValueLeft) : nameof(ValueRight));
        }

        public CompareType Comparer
        {
            get { return comparer; }
            set
            {
                if (!comparer.Equals(value))
                {
                    comparer = value;
                    OnPropertyChanged(nameof(Comparer));
                }
            }
        }

        public bool IsColumn(DBColumn column)
        {
            return (ValueLeft is QColumn && ((QColumn)ValueLeft).Column == column) ||
                (ValueRight is QColumn && ((QColumn)ValueRight).Column == column);
        }

        public LogicType Logic
        {
            get { return logic; }
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
            get { return List?.Owner as QParam; }
            set
            {
                if (value != Group && value.Group != this && value != this)
                {
                    value.Parameters.Add(this);
                    OnPropertyChanged("Group");
                }
            }
        }

        public string Value1Name
        {
            get { return QColumn?.FullName; }
        }

        public bool IsCompaund
        {
            get { return parameters != null && parameters.Count > 0; }
        }

        public QItemList<QParam> Parameters
        {
            get
            {
                if (parameters == null)
                {
                    parameters = new QItemList<QParam>(this);
                    parameters.CollectionChanged += ParametersListChanged;
                }
                return parameters;
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
            OnPropertyChanged(nameof(Parameters));
            //}
        }

        public override string ToString()
        {
            return ValueLeft == null ? Text : ValueLeft.ToString();
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
            else if (ValueRight is QBetween)
            {
                QBetween between = (QBetween)ValueRight;
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

        public bool IsExpanded
        {
            get { return GroupHelper.GetAllParentExpand(this); }
        }

        IGroup IGroup.Group
        {
            get { return Group; }
            set { Group = (QParam)value; }
        }

        public bool Expand
        {
            get { return expand; }
            set { expand = value; }
        }

        public IQItemList Owner => throw new NotImplementedException();

        public bool IsDefault { get; set; }
    }

}
