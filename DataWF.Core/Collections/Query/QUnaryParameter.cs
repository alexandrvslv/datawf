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
using DataWF.Models;

namespace DataWF.Collections.Query;

public partial class QUnaryParameter : QUnaryItem<IQItem>
{
    private CompareOperator compareOperator;

    public IQInvoker? QProperty => Item as IQInvoker;

    public IModelColumn? Property
    {
        get => QProperty?.Invoker as IModelColumn;
        set
        {
            Item = value.CreateQInvoker();
        }
    }

    public CompareOperator CompareOperator
    {
        get => compareOperator;
        set
        {
            if (!compareOperator.Equals(value))
            {
                compareOperator = value;
                //OnPropertyChanged();
            }
        }
    }

    public override object? GetObjectValue<T>(T item) => Compare(item);

    public override object? GetObjectValue(QTuple item) => Compare(item);

    public override bool Compare<T>(T item)
        => Item.Compare(item, CompareOperator);

    public override bool Compare(QTuple item)
        => Item.Compare(item, CompareOperator);

    public override bool Compare<T>(T item, CompareOperator comparer)
        => comparer.GetEvaluator<bool>().Eval(Compare(item));

    public override bool Compare(QTuple item, CompareOperator comparer)
        => Compare(item);

    public override bool Compare<T>(T item, IQItem rightItem, CompareOperator comparer)
        => comparer.GetEvaluator<bool>().Eval(Compare(item), rightItem.Compare(item));

    public override bool Compare(QTuple item, IQItem rightItem, CompareOperator comparer)
        => comparer.GetEvaluator<bool>().Eval(Compare(item), rightItem.Compare(item));

    public override string Format(object? context = null)
        => CompareOperator.Format() + " " + Item.Format(context);

    public override IQParameterFilter<T> GetFilter<T>(IQItem rightItem)
        => Item.GetFilter(rightItem);

}
