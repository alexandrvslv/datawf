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
using DataWF.Invokers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Collections.Query;

public class QOrder : QItem
{
    protected ListSortDirection direction = ListSortDirection.Ascending;
    private IQItem item;

    public QOrder()
        : base()
    {
    }

    public QOrder(IQueryInvoker invoker)
        : this(invoker.CreateQInvoker())
    {
    }

    public QOrder(IQItem column)
    {
        Item = column;
    }

    public ListSortDirection Direction
    {
        get => direction;
        set
        {
            direction = value;
            //OnPropertyChanged();
        }
    }

    public IQItem Item
    {
        get => item;
        set
        {
            if (item != value)
            {
                item = value;
                if (item != null)
                {
                    item.Holder = this;
                }
                //OnPropertyChanged();
            }
        }
    }

    public override object GetObjectValue<T>(T row)
    {
        return item.GetValue(row);
    }

    public override string Format(object? context = null)
    {
        return Item is IQInvoker
            ? string.Format("{0} {1}", Item.Format(context), direction == ListSortDirection.Descending ? "desc" : "asc")
            : string.Empty;
    }

    public virtual IComparer CreateComparer()
    {
        if (Item is IQInvoker reflection)
            return reflection.Invoker.CreateComparer(Direction);
        return null;
    }
}

