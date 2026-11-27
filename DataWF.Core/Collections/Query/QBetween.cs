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
using DataWF.Models;
using DataWF.Invokers;
using System.Data;
using DataWF.Collections.Query.Evaluators;

namespace DataWF.Collections.Query;

public class QBetween<V> : QBinaryItem<IQItem>, IQBetween<V>, IQValue
{
    private IBetween<V>? value;

    public QBetween() { }

    public QBetween(IBetween<V> value, IQueryInvoker<V> invoker)
    {
        Value = value;
        LeftItem = invoker.CreateQValue(value.Min);
        RightItem = invoker.CreateQValue(value.Max);
    }

    public QBetween(V min, V max, IQueryInvoker<V> invoker)
    {
        Value = new Between<V>(min, max);
        LeftItem = invoker.CreateQValue(min);
        RightItem = invoker.CreateQValue(max);
    }

    public QBetween(object min, object max, IQueryInvoker<V> property)
    {
        LeftItem = QItemExtensions.CreateItem(min, property, CompareOperatorType.GreaterOrEqual);
        RightItem = QItemExtensions.CreateItem(max, property, CompareOperatorType.LessOrEqual);
    }

    public IBetween<V> Value
    {
        get => value ?? this;
        set => this.value = value;
    }

    object IQValue.Value
    {
        get => Value;
    }

    public override object GetObjectValue<T>(T row) => Value ?? this;

    public override string ToString() => base.ToString();// string.Format("{0} {1}", value1, value2);

    public override string Format(object? context = null)
    {
        var f1 = LeftItem?.Format(context);
        var f2 = RightItem?.Format(context);
        if (string.IsNullOrEmpty(f1) || string.IsNullOrEmpty(f2))
            return string.Empty;

        var system = Query?.Schema?.System;
        if (system == DBSystem.MSSql
            || system == DBSystem.Postgres
            || system == DBSystem.SQLite)
            return f1 + " and " + f2;
        else
            return string.Format("({0}, {1})", f1, f2);
    }

    public V Min => value?.Min ?? ((IQItem<V>)LeftItem).GetValue<object>(null);

    public V Max => value?.Max ?? ((IQItem<V>)RightItem).GetValue<object>(null);

    public object GetMin(object? context = null) => LeftItem.GetObjectValue(context)!;

    public object GetMax(object? context = null) => RightItem.GetObjectValue(context)!;

    public override IQParameterFilter<T> GetFilter<T>(IQItem rightItem)
    {
        throw new System.NotImplementedException();
    }

    public override bool Compare<T>(T item)
    {
        throw new System.NotImplementedException();
    }

    public override bool Compare(QTuple item)
    {
        throw new System.NotImplementedException();
    }

    public override bool Compare<T>(T item, CompareOperator comparer)
    {
        throw new System.NotImplementedException();
    }

    public override bool Compare(QTuple item, CompareOperator comparer)
    {
        throw new System.NotImplementedException();
    }

    public override bool Compare<T>(T item, IQItem rightItem, CompareOperator comparer)
    {
        throw new System.NotImplementedException();
    }

    public override bool Compare(QTuple item, IQItem rightItem, CompareOperator comparer)
    {
        throw new System.NotImplementedException();
    }
}