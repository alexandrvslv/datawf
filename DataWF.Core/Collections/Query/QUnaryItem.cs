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
using System.Collections.Generic;

namespace DataWF.Collections.Query;

public abstract class QUnaryItem<TItem> : QItem, IQItemList
    where TItem : IQItem
{
    protected TItem item;

    public QUnaryItem() { }

    public TItem Item
    {
        get => item;
        set => SetItem(ref item, value, 0);
    }

    public override bool IsFilled => item?.IsFilled ?? false;

    public override object? GetObjectValue(QTuple item) => Item.GetObjectValue(item);

    public override object? GetObjectValue<T>(T item) => Item.GetObjectValue(item);

    public virtual IEnumerable<T> GetAllItems<T>(Func<T, bool>? predicate = null) where T : IQItem
    {
        return QItem.GetItemEnumerator(Item, predicate);
    }

    public virtual IEnumerator GetEnumerator()
    {
        if (item != null) yield return item;
    }

    public virtual void Add(IQItem value)
    {
        if (item == null)
            Item = (TItem)value;
        else
            throw new IndexOutOfRangeException();
    }

    public virtual void Remove(IQItem item)
    {
        if ((IQItem)this.item == item)
            Item = default!;
        else
            throw new IndexOutOfRangeException();
    }

}