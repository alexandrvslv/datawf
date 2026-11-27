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
using DataWF.Invokers;
using System;

namespace DataWF.Collections.Query;

public class QInvoker<V> : QValue<V>, IQInvoker<V>
{
    [NonSerialized()]
    private IQueryInvoker<V> invoker;

    public QInvoker()
    {
        IsReference = true;
    }

    public QInvoker(IQueryInvoker<V> invoker) : this()
    {
        Invoker = invoker;
    }

    public override string Name
    {
        get => Invoker?.Name;
        set => invoker = (IQueryInvoker<V>)InvokeProvider.GetInvoker(TargetType, value);
    }

    public IQueryInvoker<V> Invoker
    {
        get => invoker;
        set
        {
            if (invoker != value)
            {
                invoker = value;
                //OnPropertyChanged();
            }
        }
    }

    public override bool IsFilled => Invoker != null;

    public bool IsFrozen { get; set; }

    public override V GetValue(QTuple item) => IsFrozen ? Value : Invoker.GetValue(item.Get(QTable));

    public override V GetValue<T>(T item) => IsFrozen ? Value : ((IInvokerGetter<T, V>)Invoker).GetValue(item);

    public override string Format(object? context = null)
    {
        return context != null ? string.Empty : Name;
    }
    public bool IsFrozenable(IQTable table)
    {
        return table == QTable;
    }
    public bool Freezing<T>(T item, IQTable table)
    {
        if (IsFrozenable(table) && Invoker is IInvokerGetter<T, V> typedInvoker)
        {
            Value = typedInvoker.GetValue(item);
            return IsFrozen = true;
        }
        return false;
    }

    public void Unfreezing()
    {
        Value = default!;
        IsFrozen = false;
    }

    public override IQParameterFilter<T> GetFilter<T>(IQItem rightItem)
    {
        return typeof(T) == typeof(QTuple) ? (IQParameterFilter<T>)GetTupleFilter(rightItem)
            : IsFrozen ? base.GetFilter<T>(rightItem)
            : rightItem is IQIndex<V> qIndex && !qIndex.IsFrozen ? CompareQInvokerTypedQItemFilter<T, V>.Instance
            : rightItem is IQInvoker<V> qInvoker && !qInvoker.IsFrozen ? CompareQInvokerTypedQItemFilter<T, V>.Instance
            : rightItem is IQValue<V> ? CompareQInvokerTypedConstFilter<T, V>.Instance
            : rightItem is IQValue ? CompareQInvokerObjectConstFilter<T, V>.Instance
            : rightItem is IQItem<V> ? CompareQInvokerTypedQItemFilter<T, V>.Instance
            : CompareQInvokerObjectQItemFilter<T, V>.Instance;
    }

    protected override IQParameterFilter<QTuple> GetTupleFilter(IQItem rightItem) =>
        IsFrozen ? base.GetTupleFilter(rightItem)
            : rightItem is IQIndex<V> qIndex && !qIndex.IsFrozen ? CompareQInvokerTypedQItemTupleFilter<V>.Instance
            : rightItem is IQInvoker<V> qInvoker && !qInvoker.IsFrozen ? CompareQInvokerTypedQItemTupleFilter<V>.Instance
            : rightItem is IQValue<V> ? CompareQInvokerTypedConstTupleFilter<V>.Instance
            : rightItem is IQValue ? CompareQInvokerObjectConstTupleFilter<V>.Instance
            : rightItem is IQItem<V> ? CompareQInvokerTypedQItemTupleFilter<V>.Instance
            : CompareQInvokerObjectQItemTupleFilter<V>.Instance;


}