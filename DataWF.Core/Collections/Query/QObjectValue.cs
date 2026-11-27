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
using System;

namespace DataWF.Collections.Query;

public class QObjectValue : QItem, IQValue
{
    public QObjectValue() { }

    public QObjectValue(object value) => Value = value;

    public object Value { get; set; }

    public override bool IsFilled => true;

    public override object GetObjectValue<T>(T row) => Value;
    public override object GetObjectValue(QTuple row) => Value;

    public override string Format(object? context = null) => Helper.TextDisplayFormat(Value, null);

    public override IQParameterFilter<T> GetFilter<T>(IQItem rightItem)
        => rightItem.GetFilter<T>(this);

    public override bool Compare<T>(T item)
        => throw new NotImplementedException();

    public override bool Compare(QTuple item)
        => throw new NotImplementedException();

    public override bool Compare<T>(T item, CompareOperator comparer)
        => throw new NotImplementedException();

    public override bool Compare(QTuple item, CompareOperator comparer)
        => throw new NotImplementedException();
    
    public override bool Compare<T>(T item, IQItem rightItem, CompareOperator comparer)
        => rightItem.Compare(item, this, comparer);

    public override bool Compare(QTuple item, IQItem rightItem, CompareOperator comparer)
        => rightItem.Compare(item, this, comparer);
}
