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
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DataWF.Collections.Query;

public class QItemList<TItem> : QItem, IQItemList<TItem>
    where TItem : IQItem
{
    protected SelectableList<TItem> items;
    public QItemList(int capacity = 2)
    {
        items = new SelectableList<TItem>(capacity);
        items.ApplySort(new InvokerComparer<TItem, int>(IQItem.OrderInvoker.Instance, ListSortDirection.Ascending));
    }

    public QItemList(IEnumerable<TItem> items) : this()
    {
        this.items.AddRangeInternal(items, false);
    }

    public QItemList(IQItem holder) : this()
    {
        Holder = holder;
    }

    public QItemList(IQItem holder, int capacity) : this(capacity)
    {
        Holder = holder;
    }

    public override bool IsFilled => items.Count > 0 && items.All(x => x.IsFilled);

    public override IQQuery Query => Holder.Query;

    public int Count => items.Count;

    public IQItem this[int index]
    {
        get => items[index];
        set
        {
            var oldItem = items[index];
            SetItem(ref oldItem, (TItem)value, index + 1);
            items[index] = (TItem)value;
        }
    }

    public int IndeOf(IQItem item)
        => items.IndexOf(item);

    public virtual void Add(TItem item)
    {
        base.OnAdd(item, items.Count);
        items.Add(item);
    }

    public void Insert(int index, TItem item)
    {
        base.OnAdd(item, index + 1);
        items.Insert(index, item);
    }

    public void Add(IQItem item) => Add((TItem)item);

    public void Remove(IQItem item) => items.Remove((TItem)item);

    public void AddRange(IEnumerable<TItem> value) => items.AddRange(value);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => GetEnumerator();

    public OneArrayPointerEnumerator<TItem> GetEnumerator() => items.GetEnumerator();

    public void Clear() => items.Clear();

    public override string Format(object? context = null)
        => string.Join(',', items.Select(x => x.Format(context)));

    public override object GetObjectValue<T>(T item)
        => items.Select(p => p.GetObjectValue(item));
    
    public override object GetObjectValue(QTuple item)
        => items.Select(p => p.GetObjectValue(item));    

    public IEnumerable<T> GetAllItems<T>(Func<T, bool>? predicate = null) where T : IQItem
    {
        foreach (var item in items)
        {
            foreach (var subItem in GetItemEnumerator(item, predicate))
            {
                yield return subItem;
            }
        }
    }

    public override IQParameterFilter<T> GetFilter<T>(IQItem rightItem) => throw new NotImplementedException();

    public override bool Compare<T>(T item)
    {
        bool result = true;
        foreach(var qItem in items)
        {
            result &= qItem.Compare(item);
        }
        return result;
    }

    public override bool Compare(QTuple item)
    {
        bool result = true;
        foreach(var qItem in items)
        {
            result &= qItem.Compare(item);
        }
        return result;
    }

    public override bool Compare<T>(T item, CompareOperator comparer)
    {
        bool result = true;
        foreach(var qItem in items)
        {
            result &= qItem.Compare(item, comparer);
        }
        return result;
    }

    public override bool Compare(QTuple item, CompareOperator comparer)
    {
        bool result = true;
        foreach(var qItem in items)
        {
            result &= qItem.Compare(item, comparer);
        }
        return result;
    }

    public override bool Compare<T>(T item, IQItem rightItem, CompareOperator comparer)
    {
        bool result = true;
        foreach(var qItem in items)
        {
            result &= qItem.Compare(item, rightItem, comparer);
        }
        return result;
    }

    public override bool Compare(QTuple item, IQItem rightItem, CompareOperator comparer)
    {
        bool result = true;
        foreach(var qItem in items)
        {
            result &= qItem.Compare(item, rightItem, comparer);
        }
        return result;
    }
}
