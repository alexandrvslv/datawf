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

namespace DataWF.Data
{
    public class QParam : QItem, IComparable, IGroup
    {
        public static QItem Fabric(object value)
        {
            if (value is QItem)
                return (QItem)value;
            else if (value is DBColumn)
                return new QColumn((DBColumn)value);
            else
                return new QValue(value.ToString());
        }

        [NonSerialized()]
        protected QParam _group = null;
        protected QItem value1;
        protected QItem value2;
        protected CompareType comparer = CompareType.Undefined;
        protected LogicType logic = LogicType.And;
        protected QItemList<QParam> parameters;
        [DefaultValue(true)]
        private bool expand = true;

        public QParam()
            : base()
        {
        }

        public QParam(string col)
        {
            value1 = new QColumn(col);
        }

        public QParam(LogicType logicType, string col, CompareType compareType, object value)
            : this(col)
        {
            this.logic = logicType;
            this.comparer = compareType;
            this.value2 = Fabric(value);
        }

        public void CheckGroupLogic()
        {
            if (Group != null)
            {
                if (Group.Parameters.IsFirst(this))
                    Group.Logic = this.Logic;
            }
        }

        public DBColumn Column
        {
            get { return value1 is QColumn ? ((QColumn)value1).Column : null; }
            set
            {
                if (value1 is QColumn)
                    ((QColumn)value1).Column = value;
                else
                    ValueLeft = new QColumn(value);
            }
        }

        public object Value
        {
            get { return value2 == null ? null : value2.GetValue(null); }
            set
            {
                if (value is QItem)
                    ValueRight = (QItem)value;
                else if (value is DateInterval)
                {
                    if (((DateInterval)value).IsEqual())
                    {
                        //if(Value1 is QColumn)
                        //     Value1 = new QFunc( QFunctionType.convert, QType.Date, 
                        ValueRight = new QBetween(((DateInterval)value).Min.Date, ((DateInterval)value).Min.Date.AddDays(1), Column);
                    }
                    else
                        ValueRight = new QBetween(((DateInterval)value).Min, ((DateInterval)value).Max, Column);
                }
                else if (comparer.Type == CompareTypes.In)
                {
                    if (value is string)
                        ValueRight = new QEnum(((string)value).Split(','), Column);
                    else if (value is IList)
                        ValueRight = new QEnum((IList)value, Column);
                }
                else if (value2 is QValue)
                {
                    ((QValue)value2).Value = value;
                    OnPropertyChanged("Value2");
                }
                else
                    ValueRight = new QValue(value, Column);
            }
        }

        public QItem ValueLeft
        {
            get { return value1; }
            set
            {
                if (this.value1 != value)
                {
                    if (this.value1 is QQuery)
                        ((QQuery)this.value1).Parameters.ListChanged -= ValueListChanged;

                    this.value1 = value;

                    if (this.value1 is QQuery)
                        ((QQuery)this.value1).Parameters.ListChanged += ValueListChanged;

                    OnPropertyChanged("Value1");
                }
            }
        }

        public QItem ValueRight
        {
            get { return value2; }
            set
            {
                if (this.value2 != value)
                {
                    if (this.value2 is QQuery)
                        ((QQuery)this.value2).Parameters.ListChanged -= ValueListChanged;

                    this.value2 = value;

                    if (this.value2 is QQuery)
                        ((QQuery)this.value2).Parameters.ListChanged += ValueListChanged;
                }
                OnPropertyChanged("Value2");
            }
        }

        private void ValueListChanged(object sender, ListChangedEventArgs e)
        {
            OnPropertyChanged(sender == value1 ? "Value1" : "Value2");
        }

        public CompareType Comparer
        {
            get { return comparer; }
            set
            {
                if (!comparer.Equals(value))
                {
                    comparer = value;
                    OnPropertyChanged("Comparer");
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

        public QParam Group
        {
            get { return _group; }
            set
            {
                if (value != _group && value.Group != this && value != this)
                {
                    _group = value;
                    OnPropertyChanged("Group");
                }
            }
        }

        public string Value1Name
        {
            get { return ((QColumn)ValueLeft).FullName; }
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
                    parameters = new QItemList<QParam>(query);
                    parameters.ListChanged += ParametersListChanged;
                }
                return parameters;
            }
        }

        public void ParametersListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemAdded)
            {
                parameters[e.NewIndex].Group = this;
            }
            else if (e.ListChangedType == ListChangedType.ItemDeleted)
            {
                if (e.NewIndex >= 0)
                    parameters[e.NewIndex].Group = null;
            }
            else
                OnPropertyChanged("Parameters");
        }

        public override string ToString()
        {
            return ValueLeft == null ? Text : ValueLeft.ToString();
        }

        //public string FormatText()
        //{
        //    if (Column == null)
        //        return text;
        //    string buf = this.Table.ToString() + "." +
        //        this.Column.ToString() + " ";
        //    buf += comparer.ToString() + " ";
        //    if (Value is QQuery)
        //    {
        //        QQuery exp = (QQuery)Value;
        //        if (exp != null)
        //            buf += exp.ToText();
        //        else
        //            buf += " ";
        //    }
        //    else
        //        buf += this.Value.ToString();

        //    return buf;
        //}



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
            //if (cs != null)
            //{
            //    object Value = this.Value;

            //    if (cs.DataType == typeof(DateTime) && Value != null)
            //    {
            //        DateInterval interval = null;

            //        if (Value is DateTime)
            //            interval = new DateInterval((DateTime)Value);
            //        else if (Value is DateInterval)
            //            interval = new DateInterval(((DateInterval)Value).Min, ((DateInterval)Value).Max);
            //        else
            //            interval = new DateInterval((DateTime)DBService.ParceValue(cs, Value.ToString()));

            //        if (interval.IsEqual() && comparer != CompareType.Between)
            //        {
            //            if (comparer == CompareType.Equal)
            //                buf = string.Format("convert(date, {0}) = {1}", cs.Code, FormatValue(interval.Min, command));
            //            else
            //                buf = string.Format("{0} {1} {2}", cs.Code, QQuery.CompareCode(Comparer), FormatValue(interval.Min, command));
            //        }
            //        else
            //        {
            //            interval.Max = interval.Max.AddDays(1);
            //            buf = string.Format("({0} >= {1} and {0} < {2})", cs.Code, FormatValue(interval.Min, command, 1), FormatValue(interval.Max, command, 2));
            //        }
            //    }
            //    else
            //    {
            //        buf = string.Format("{0} {1} {2}", cs.Code, QQuery.CompareCode(Comparer), FormatValue(Value, command));
            //    }
            //}
            //return buf;
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
    }

}
