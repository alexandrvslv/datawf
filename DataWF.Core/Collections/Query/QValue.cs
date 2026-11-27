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
using DataWF.Collections.Query.Evaluators;
using DataWF.Comparers;
using DataWF.Serialization;
using System;

namespace DataWF.Collections.Query;

public class QValue<V> : QItem, IQValue<V>
{
    private IUniversalComparer<V> universalComparer;

    public QValue()
    {
        universalComparer = UniversalComparer<V>.Default;
    }

    public QValue(V typedValue) : this()
    {
        Value = typedValue;
    }

    public IUniversalComparer<V> KeyComparer => universalComparer;

    public V Value { get; set; }

    public override bool IsFilled => true;

    public override Type DataType => typeof(V);

    public override object? GetObjectValue<T>(T item) => GetValue(item);

    public override object? GetObjectValue(QTuple item) => GetValue(item);

    public virtual V GetValue<T>(T item) => Value;

    public virtual V GetValue(QTuple item) => Value;

    public override string Format(object? context = null)
        => SerializeProvider.GetSerializer<V>()!.FormatQuery(Value, null);

    public override bool Compare<T>(T item) => true;

    public override bool Compare(QTuple item) => true;
    
    public override bool Compare<T>(T item, CompareOperator comparer)
    {
        var leftParam = GetValue(item);
        return comparer.GetEvaluator<V>().Eval(leftParam, universalComparer);
    }

    public override bool Compare(QTuple item, CompareOperator comparer)
    {
        var leftParam = GetValue(item);
        return comparer.GetEvaluator<V>().Eval(leftParam, universalComparer);
    }

    public override bool Compare<T>(T item, IQItem rightItem, CompareOperator comparer)
    {
        var leftParam = GetValue(item);
        return rightItem is IQItem<V> rightTypedItem
            ? comparer.GetEvaluator<V>().Eval(leftParam, rightTypedItem.GetValue(item), universalComparer)
            : comparer.GetEvaluator<V>().Eval(leftParam, rightItem.GetObjectValue(item)!, universalComparer);
    }

    public override bool Compare(QTuple item, IQItem rightItem, CompareOperator comparer)
    {
        var leftParam = GetValue(item);
        return rightItem is IQItem<V> rightTypedItem
            ? comparer.GetEvaluator<V>().Eval(leftParam, rightTypedItem.GetValue(item), universalComparer)
            : comparer.GetEvaluator<V>().Eval(leftParam, rightItem.GetObjectValue(item)!, universalComparer);
    }

    public override IQParameterFilter<T> GetFilter<T>(IQItem rightItem) =>
        typeof(T) == typeof(QTuple) ? (IQParameterFilter<T>)GetTupleFilter(rightItem)
            : rightItem is IQIndex<V> qIndex && !qIndex.IsFrozen ? CompareQConstTypedQItemFilter<T, V>.Instance
            : rightItem is IQInvoker<V> qInvoker && !qInvoker.IsFrozen ? CompareQConstTypedQItemFilter<T, V>.Instance
            : rightItem is IQValue<V> ? CompareQConstTypedConstFilter<T, V>.Instance
            : rightItem is IQValue ? CompareQConstObjectConstFilter<T, V>.Instance
            : rightItem is IQItem<V> ? CompareQConstTypedQItemFilter<T, V>.Instance
            : CompareQConstObjectQItemFilter<T, V>.Instance;

    protected virtual IQParameterFilter<QTuple> GetTupleFilter(IQItem rightItem) =>
        rightItem is IQIndex<V> qIndex && !qIndex.IsFrozen ? CompareQConstTypedQItemTupleFilter<V>.Instance
            : rightItem is IQInvoker<V> qInvoker && !qInvoker.IsFrozen ? CompareQConstTypedQItemTupleFilter<V>.Instance
            : rightItem is IQValue<V> ? CompareQConstTypedConstTupleFilter<V>.Instance
            : rightItem is IQValue ? CompareQConstObjectConstTupleFilter<V>.Instance
            : rightItem is IQItem<V> ? CompareQConstTypedQItemTupleFilter<V>.Instance
            : CompareQConstObjectQItemTupleFilter<V>.Instance;

    
}