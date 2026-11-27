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
using DataWF.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Collections.Query;

[Invoker(Instance = true)]
public abstract partial class QItem : DefaultItem, IDisposable, IComparable, IComparable<QItem>, IQItem
{
    protected static IEnumerable<T> GetItemEnumerator<T>(IQItem item, Func<T, bool>? predicate = null)
        where T : IQItem
    {
        if (item is T typed
            && (predicate == null
                || predicate(typed)))
            yield return typed;
        if (item is IQItemList minList)
            foreach (var minItem in minList.GetAllItems<T>(predicate))
                yield return minItem;
    }

    protected int order = -1;
    private string? tableAlias;
    private string? columnAlias;
    private IQItem? holder;
    protected IQTable? qTable;
    private bool refmode;

    public QItem()
    {
    }

    public int Order
    {
        get => order;
        set
        {
            order = value;
            //OnPropertyChanged();
        }
    }

    public virtual bool IsReference
    {
        get => refmode;
        set => refmode = value;
    }

    public string? TableAlias
    {
        get => tableAlias;
        set
        {
            if (tableAlias != value)
            {
                tableAlias = value;
                //OnPropertyChanged();
            }
        }
    }

    public string? ColumnAlias
    {
        get => columnAlias;
        set
        {
            if (columnAlias != value)
            {
                columnAlias = value;
                //OnPropertyChanged(nameof(Prefix));
            }
        }
    }

    [JsonIgnore, XmlIgnore, Browsable(false)]
    public virtual IQItem? Holder
    {
        get => holder;
        set
        {
            if (holder != value)
            {
                holder = value;
                //OnPropertyChanged();
            }
        }
    }

    [JsonIgnore, XmlIgnore, Browsable(false)]
    public virtual IQQuery? Query => Holder?.Query;

    [JsonIgnore, XmlIgnore, Browsable(false)]
    public virtual IModelTable? Table
    {
        get => QTable?.Table;
        set { }
    }

    [JsonIgnore, XmlIgnore, Browsable(false)]
    public virtual IQTable? QTable
    {
        get => qTable ??= Query?.GetTableByAlias(tableAlias) ?? Holder?.QTable;
        set
        {
            qTable = value;
            if (qTable != null)
            {
                TableAlias = qTable.TableAlias;
            }
        }
    }

    [JsonIgnore, XmlIgnore, Browsable(false)]
    public virtual IModelSchema? Schema
    {
        get => Table?.Schema ?? Query?.Schema;
        set { }
    }

    public virtual Type DataType => typeof(object);

    public Type TargetType => typeof(object);

    public bool CanWrite => false;

    public virtual string Name
    {
        get => string.Empty;
        set { }
    }

    public abstract bool IsFilled { get; }

    public abstract string Format(object? context = null);

    public object? GetValue(object? target)
        => target is QTuple turple
            ? GetObjectValue(turple)
            : GetObjectValue<object>(target);

    public virtual object? GetObjectValue(QTuple tuple)
        => GetObjectValue<object>(tuple.Get(QTable));

    public abstract object? GetObjectValue<T>(T item);

    public virtual void Dispose()
    {
    }

    public int CompareTo(object? obj) => CompareTo(obj as QItem);

    public int CompareTo(QItem? other) => order.CompareTo(other?.order ?? 0);

    public override string ToString() => Format();

    protected void SetItem<T>(ref T? item, T? value, int order = -1, [CallerMemberName] string? propertyName = null)
        where T : IQItem
    {
        if (item != null)
        {
            OnRemove(item);
        }
        item = value;
        if (item != null)
        {
            OnAdd(item, order);
        }
    }

    protected void OnAdd(IQItem item, int order)
    {
        OnRemove(item);
        if (item is IQInvoker)
            IsReference = true;
        if (order >= 0)
            item.Order = order;
        item.Holder = this;
    }

    private void OnRemove(IQItem item)
    {
        if (item.Holder is IQItemList itemList && itemList != this)
        {
            itemList.Remove(item);
            item.Holder = null;
        }
    }

    public abstract IQParameterFilter<T> GetFilter<T>(IQItem rightItem);
    public abstract bool Compare<T>(T item);
    public abstract bool Compare(QTuple item);
    public abstract bool Compare<T>(T item, CompareOperator comparer);
    public abstract bool Compare(QTuple item, CompareOperator comparer);
    public abstract bool Compare<T>(T item, IQItem rightItem, CompareOperator comparer);
    public abstract bool Compare(QTuple item, IQItem rightItem, CompareOperator comparer);
}
