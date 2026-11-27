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

namespace DataWF.Collections.Query;

public class QMathUnary<V> : QUnaryItem<IQItem<V>>, IQItem<V>
{
    public QMathUnary() { }

    public QMathUnary(MathOperator mathOperator)
    {
        MathOperator = mathOperator;
        Evaluator = mathOperator.GetUnaryEvaluator<V>();
    }

    public MathOperator MathOperator { get; }
    public IMathUnaryEvaluator<V> Evaluator { get; }
    public IUniversalComparer<V> KeyComparer => UniversalComparer<V>.Default;

    public override bool Compare<T>(T item) => throw new System.NotImplementedException();

    public override bool Compare(QTuple item) => throw new System.NotImplementedException();

    public override bool Compare<T>(T item, CompareOperator comparer) => throw new System.NotImplementedException();

    public override bool Compare(QTuple item, CompareOperator comparer) => throw new System.NotImplementedException();

    public override bool Compare<T>(T item, IQItem rightItem, CompareOperator comparer) => throw new System.NotImplementedException();

    public override bool Compare(QTuple item, IQItem rightItem, CompareOperator comparer) => throw new System.NotImplementedException();

    public override string Format(object? context = null) => $" {MathOperator.Format(MathOperator.Type)} {Item.Format(context)}";

    public override IQParameterFilter<T> GetFilter<T>(IQItem rightItem) => throw new System.NotImplementedException();

    public override object? GetObjectValue(QTuple item)=> GetValue(item);

    public override object? GetObjectValue<T>(T item) => GetValue(item);

    public V GetValue<T>(T item)
    {
        var param1 = Item.GetValue(item);
        return Evaluator.Eval(param1);
    }

    public V GetValue(QTuple item)
    {
        var param1 = Item.GetValue(item);
        return Evaluator.Eval(param1);
    }
}

