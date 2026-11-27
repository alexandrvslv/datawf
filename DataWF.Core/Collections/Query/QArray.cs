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
using System;
using System.Collections;
using System.Linq;
using System.Text;
using DataWF.Invokers;

namespace DataWF.Collections.Query;

public class QArray : QItemList<QItem>
{
    protected IQueryInvoker? invoker;
    public QArray() { }
    public QArray(IEnumerable list, IQueryInvoker? invoker = null, CompareOperatorType compareOperator = default) : base()
    {
        Property = invoker;
        foreach (var item in list)
        {
            if (item is IInvoker
                || item is QItem qItem && qItem.IsReference)
                IsReference = true;

            var value = item;
            if (item is string stringed)
                value = stringed.Trim('\'');

            Add(QItemExtensions.CreateItem(value, invoker, compareOperator));
        }
    }

    public IQueryInvoker? Property
    {
        get => invoker;
        set
        {
            if (invoker != value)
            {
                invoker = value;
                //foreach (var item in items)
                //{
                //    if (item is QValue qValue
                //        && qValue.Column == null)
                //        qValue.Column = property;
                //}
            }
        }
    }

    //public override IQItemList Container
    //{
    //    get => base.Container;
    //    set
    //    {
    //        base.Container = value;
    //        if (Query != null)
    //        {
    //            foreach (var item in items)
    //            {
    //                if (item is IQInvoker column
    //                    && column.QTable == null
    //                    && column.Table != null)
    //                {
    //                    column.QTable = Query.GetTable(column.Table);
    //                }
    //            }
    //        }
    //    }
    //}

    public override string Format(object? context = null) => Format(", ", context);

    public string Format(string separator, object? context = null)
    {
        var rez = new StringBuilder('(');
        foreach (QItem item in items)
        {
            rez.Append(item.Format(context));
            rez.Append(separator);
        }
        if (rez.Length > (separator.Length + 1))
        {
            rez.Length -= separator.Length;
            rez.Append(')');
            return rez.ToString();
        }
        return string.Empty;
    }

}
