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
using System.Linq;

namespace DataWF.Collections.Query;

public abstract class QBinaryItem<TItem> : QItem, IQBinaryItem<TItem>, IQFrozenable
    where TItem : IQItem
{
    protected TItem? lItem;
    protected TItem? rItem;

    public TItem LeftItem
    {
        get => lItem;
        set => SetItem(ref lItem, value, 0);
    }

    public TItem RightItem
    {
        get => rItem;
        set => SetItem(ref rItem, value, 1);
    }

    public override bool IsFilled => (LeftItem?.IsFilled ?? false)
                                && (RightItem?.IsFilled ?? false);

    public IEnumerable<T> GetAllItems<T>(Func<T, bool>? predicate = null)
        where T : IQItem
    {
        return GetItemEnumerator(LeftItem, predicate)
            .Concat(GetItemEnumerator(RightItem, predicate));
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<TItem> GetEnumerator()
    {
        if (lItem != null) yield return lItem;
        if (rItem != null) yield return rItem;
    }

    public virtual void Add(IQItem value)
    {
        if (lItem == null)
            LeftItem = (TItem)value;
        else if (rItem == null)
            RightItem = (TItem)value;
        else if (RightItem is QArray rList)
            rList.Add(value);
        else if (LeftItem is QArray lList)
            lList.Add(value);
        else
            throw new IndexOutOfRangeException();
    }

    public virtual void Remove(IQItem item)
    {
        if (Equals(LeftItem, item))
            LeftItem = default!;
        if (Equals(RightItem, item))
            RightItem = default!;
        else
            throw new IndexOutOfRangeException();
    }

    public void Replace(IQItem toReplace, IQItem forReplace)
    {
        if (Equals(toReplace, LeftItem))
            LeftItem = (TItem)forReplace;
        else if (Equals(toReplace, RightItem))
            RightItem = (TItem)forReplace;
        else if (RightItem is QArray rList
            && rList.IndeOf(toReplace) is { } rIndex
            && rIndex >= 0)
            rList[rIndex] = forReplace;
        else if (LeftItem is QArray lList
            && lList.IndeOf(toReplace) is { } lIndex
            && lIndex >= 0)
            lList[lIndex] = forReplace;
        else
            throw new KeyNotFoundException(nameof(toReplace));
    }

    private static bool Equals(IQItem a, IQItem b)
    {
        return a == b;
    }

    public bool IsFrozen
        => LeftItem is IQFrozenable lf && lf.IsFrozen
        || RightItem is IQFrozenable rf && rf.IsFrozen;

    public bool IsFrozenable(IQTable table)
        => LeftItem is IQFrozenable lf && lf.IsFrozenable(table)
        || RightItem is IQFrozenable rf && rf.IsFrozenable(table);

    public bool Freezing<R>(R item, IQTable table)
        => (LeftItem is IQFrozenable lf && lf.Freezing(item, table))
        | (RightItem is IQFrozenable rf && rf.Freezing(item, table));

    public void Unfreezing()
    {
        if (LeftItem is IQFrozenable lf) lf.Unfreezing();
        if (RightItem is IQFrozenable rf) rf.Unfreezing();
    }
}