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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Collections.Query;

public class QTable<TItem> : QItem, IQTable<TItem>
{
    protected JoinOperator join = JoinOperator.Undefined;
    protected IEnumerable<TItem> source;
    private IQParameter? onParam;

    public QTable()
    {
        source = Enumerable.Empty<TItem>();
    }

    public QTable(IEnumerable<TItem> source, string? alias = null)
    {
        this.source = source;
        TableAlias = alias;
    }

    public override string Name
    {
        get => Table?.Name ?? typeof(TItem).Name;
        set { }
    }

    public JoinOperator Join
    {
        get => join;
        set
        {
            if (join != value)
            {
                join = value;
                //OnPropertyChanged();
            }
        }
    }

    public new IModelTable<TItem>? Table
    {
        get => source as IModelTable<TItem>;
        set
        {
            if (Source != value)
            {
                Source = value;
                //OnPropertyChanged();
            }
        }
    }

    public IEnumerable<TItem> Source
    {
        get => source;
        set
        {
            if (source != value)
            {
                source = value;
                //OnPropertyChanged();
            }
        }
    }

    public IQParameter? On
    {
        get => onParam;
        set => SetItem(ref onParam, value, 0);
    }

    public bool HasParameters => GetParameters().Any();

    IEnumerable IQTable.Source
    {
        get => Source;
        set => Source = (IEnumerable<TItem>)value;
    }

    public override string Format(object? context = null)
    {
        return $"{Join.Format()} {TableAlias ?? Table?.Name} {(On != null ? $" on {On.Format(context)}" : string.Empty)}";
    }

    public override object GetObjectValue<IT>(IT row)
    {
        return Name;
    }

    public IEnumerable<IQCompareParameter> GetParameters()
    {
        return Query?.GetAllParameters(p => p.GetAllItems<IQInvoker>(c => c.QTable == this).Any()).ToList() ?? [];
    }
}
