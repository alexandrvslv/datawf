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
using DataWF.Collections.Indexes;
using DataWF.Collections.Query.Evaluators;
using DataWF.Invokers;
using System.Collections.Generic;

namespace DataWF.Collections.Query;

public class QIndex<V> : QInvoker<V>, IQIndex<V>
{
    public QIndex() { }

    public QIndex(IIndex listIndex)
        : base((IQueryInvoker<V>)listIndex.Invoker)
    {
        Index = listIndex;
    }

    public IIndex Index { get; set; }

    public override IQParameterFilter<T> GetFilter<T>(IQItem rightItem)
    {
        return typeof(T) == typeof(QTuple) ? (IQParameterFilter<T>)GetTupleFilter(rightItem)
            : IsFrozen ? base.GetFilter<T>(rightItem)
            : rightItem is IQIndex<V> rqIndex && !rqIndex.IsFrozen ? CompareQIndexTypedQItemFilter<T, V>.Instance
            : rightItem is IQInvoker<V> rqInvoker && !rqInvoker.IsFrozen ? CompareQIndexTypedQItemFilter<T, V>.Instance
            : rightItem is IQValue<V> ? CompareQIndexTypedConstFilter<T, V>.Instance
            : rightItem is IQValue ? CompareQIndexObjectConstFilter<T, V>.Instance
            : rightItem is IQItem<V> ? CompareQIndexTypedQItemFilter<T, V>.Instance
            : CompareQIndexObjectQItemFilter<T, V>.Instance;
    }

    protected override IQParameterFilter<QTuple> GetTupleFilter(IQItem rightItem) =>
        IsFrozen ? base.GetTupleFilter(rightItem)
            : rightItem is IQIndex<V> rqIndex && !rqIndex.IsFrozen ? CompareQIndexTypedQItemTupleFilter<V>.Instance
            : rightItem is IQInvoker<V> rqInvoker && !rqInvoker.IsFrozen ? CompareQIndexTypedQItemTupleFilter<V>.Instance
            : rightItem is IQValue<V> ? CompareQIndexTypedConstTupleFilter<V>.Instance
            : rightItem is IQValue ? CompareQIndexObjectConstTupleFilter<V>.Instance
            : rightItem is IQItem<V> ? CompareQIndexTypedQItemTupleFilter<V>.Instance
            : CompareQIndexObjectQItemTupleFilter<V>.Instance;
}