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
using DataWF.Models;
using System;
using System.Runtime.CompilerServices;

namespace DataWF.Collections.Query;

public interface IQTuple : ITuple
{
    T Get<T>(int index);
}
public struct ETuple<TL, TR> : IQTuple
{
    public TL Left;
    public TR Right;

    public ETuple(TL left, TR right)
    {
        Left = left;
        Right = right;
    }

    public object this[int index] => Get<object>(index);

    public int Length => Left == null ? 0
        : Right == null ? 1
        : Right is ITuple rTuple ? 1 + rTuple.Length
        : 2;

    public TT Get<TT>(int index)
    {
        if (index == 0) return Left is TT tLeft ? tLeft : default(TT);
        if (Right is IQTuple rTuple) return rTuple.Get<TT>((index - 1));
        if (index == 1) return Right is TT tRight ? tRight : default(TT);

        throw new IndexOutOfRangeException();
    }

    public void Set<T>(int index, T item)
    {
        if (index == 0 && item is TL tLeft)
            Left = tLeft;
        if (index == 1 && item is TR tRight)
            Right = tRight;
    }
}
public struct QTuple
{
    public QTuple(object leftItem)
    {
        Item0 = leftItem;
        Item1 = null;
        Item2 = null;
        Item3 = null;
        Item4 = null;
        Item5 = null;
        Item6 = null;
        Item7 = null;
        Count = 1;
    }

    public object Item0;
    public object? Item1;
    public object? Item2;
    public object? Item3;
    public object? Item4;
    public object? Item5;
    public object? Item6;
    public object? Item7;
    public int Count;

    public object this[int order]
    {
        get => Get(order);
        set => Set(order, value);
    }

    public object Get(IQTable qTable) => Get(qTable.Order) ?? Get(qTable.Table);

    public void Set(IQTable table, object? item) => Set(table.Order, item);

    public object? Get(IModelTable table)
    {
        if (Item0 is IModelTableContent modelContent && modelContent.Table == table) return Item0;
        if (Item1 is IModelTableContent modelContent1 && modelContent1.Table == table) return Item1;
        if (Item2 is IModelTableContent modelContent2 && modelContent2.Table == table) return Item2;
        if (Item3 is IModelTableContent modelContent3 && modelContent3.Table == table) return Item3;
        if (Item4 is IModelTableContent modelContent4 && modelContent4.Table == table) return Item4;
        if (Item5 is IModelTableContent modelContent5 && modelContent5.Table == table) return Item5;
        if (Item6 is IModelTableContent modelContent6 && modelContent6.Table == table) return Item6;
        if (Item7 is IModelTableContent modelContent7 && modelContent7.Table == table) return Item7;
        return null;
    }

    private void Set(int order, object? value)
    {
        if (order < 0)
            throw new ArgumentOutOfRangeException(nameof(order));
        switch (order)
        {
            case 0: Item0 = value; break;
            case 1: Item1 = value; break;
            case 2: Item2 = value; break;
            case 3: Item3 = value; break;
            case 4: Item4 = value; break;
            case 5: Item5 = value; break;
            case 6: Item6 = value; break;
            case 7: Item7 = value; break;
            default: throw new IndexOutOfRangeException();
        }
        Count = Math.Max(Count, order + 1);
    }

    private object? Get(int order)
    {
        if (order < 0)
            throw new ArgumentOutOfRangeException(nameof(order));
        switch (order)
        {
            case 0: return Item0;
            case 1: return Item1;
            case 2: return Item2;
            case 3: return Item3;
            case 4: return Item4;
            case 5: return Item5;
            case 6: return Item6;
            case 7: return Item7;
            default: throw new IndexOutOfRangeException();
        }
    }

    public QTuple Clone()
    {
        var newTuple = new QTuple();
        for (int i = 0; i < Count; i++)
            newTuple.Set(i, Get(i));
        return newTuple;
    }
}
