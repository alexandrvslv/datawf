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
using DataWF.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;


namespace DataWF.Collections.Query;

public partial class QParamList : QItem, IQItemList<IQParameter>, IQParameter, IGroup
{
    private bool expand = true;
    private bool? isCompaund;
    private IQParameter? qParameter;
    public QParamList() { }

    public QParamList(IQParameter? parameter)
    {
        Parameter = parameter;
    }

    public QParamList(IQItem owner) : base(owner)
    {
        //Indexes.Add(groupInvoker);
    }

    public QParamList(IModelTable table, string viewFilter) : this()
    {
        var query = table.Where(viewFilter);
        qParameter = new QParamList(query.Parameters.Parameter);
    }

    public IQParameter? Parameter
    {
        get => qParameter;
        set => SetItem(ref qParameter, value, 1);
    }

    public bool Expand
    {
        get => expand;
        set
        {
            if (expand != value)
            {
                expand = value;
            }
        }
    }

    [XmlIgnore, JsonIgnore]
    public IQParameter? Group
    {
        get => Holder as IQParameter;
        set
        {
            if (value != Group && value.Group != this && value != this)
            {
                value.Add(this);
                //OnPropertyChanged();
            }
        }
    }

    IGroup IGroup.Group
    {
        get => Group;
        set => Group = (QParamList)value;
    }

    [XmlIgnore, JsonIgnore]
    public bool IsExpanded => GroupHelper.IsAllGroupsExpand(this);

    [XmlIgnore, JsonIgnore]
    public bool IsCompound
    {
        get => isCompaund ?? (items.Count > 0 && items[0] is QCompareParameter);
        set => isCompaund = value;
    }

    public IEnumerable<IGroup> GetGroups()
    {
        foreach (var item in items.OfType<IGroup>())
            yield return item;
    }

    public override bool Compare<T>(T item)
        => qParameter?.Compare(item) ?? true;

    public override bool Compare(QTuple item)
        => qParameter?.Compare(item) ?? true;

     public override bool Compare<T>(T item, CompareOperator compare)
        => qParameter?.Compare(item, compare) ?? true;

    public override bool Compare(QTuple item, CompareOperator compare)
        => qParameter?.Compare(item, compare) ?? true;

    public override bool Compare<T>(T item, IQItem rItem, CompareOperator compare)
        => qParameter?.Compare(item, rItem, compare) ?? true;

    public override bool Compare(QTuple item, IQItem rItem, CompareOperator compare)
        => qParameter?.Compare(item, rItem, compare) ?? true;

    public IEnumerable<T> Filter<T>(IEnumerable<T> items)
        => qParameter?.Filter(items) ?? items;

    public IEnumerable<QTuple> Filter(IEnumerable<QTuple> items)
        => qParameter?.Filter(items) ?? items;

    public override string ToString() => $"{Logic} ({string.Join(" ", items.Select(p => p.ToString()))})";

    public override string Format(object? context = null)
    {
        string buf = string.Empty;
        foreach (QCompareParameter param in items)
        {
            string subparam = param.Format(context);
            if (subparam.Length != 0)
            {
                buf += (buf.Length > 0 ? param.Logic.Format() + " " : string.Empty) + subparam + " ";
            }
        }
        if (buf.Length > 0)
            buf = "(" + buf + ")";
        return buf;
    }

    public void Add(IQItem item)
    {
        if (item is IQParameter qParameter)
            Add(qParameter, CompareOperator.AndAlso);
        else
            throw new NotSupportedException(item?.GetType().Name ?? "null");
    }

    public void Remove(IQItem item)
    {
        if (item is IQParameter qParameter)
            Remove(qParameter);
        else
            throw new NotSupportedException(item?.GetType().Name ?? "null");
    }

    public void Add(IQParameter qParam, CompareOperator compareOperator)
    {
        if (qParameter == null)
        {
            Parameter = qParam;
        }
        else if (qParameter is QCompareParameter compParam
            && (compParam.LeftItem == null || compParam.RightItem == null))
        {
            compParam.Comparer = compareOperator;
            if (compParam.LeftItem == null)
                compParam.LeftItem = qParam;
            else if (compParam.RightItem == null)
                compParam.RightItem = compParam;
        }
        else
        {
            Parameter = new QCompareParameter()
            {
                LeftItem = qParameter,
                Comparer = compareOperator,
                RightItem = qParam,
            };
        }
    }
    
    public void Remove(IQParameter qParam)
    {
        if(qParameter == qParam)
            Parameter = null;        
    }
}

public static class QParameterExtensions
{
    public static QParamList And(this QParamList parameters, QCompareParameter qParam)
    {
        parameters.Add(qParam, CompareOperator.AndAlso);
        return parameters;
    }

    public static QParamList And(this QParamList parameters, Action<QCompareParameter> parameterGroup)
    {
        var qParam = new QCompareParameter() { Comparer = CompareOperator.AndAlso };
        parameters.Add(qParam);
        parameterGroup(qParam);
        return parameters;
    }

    public static QParamList And(this QParamList parameters, IQueryInvoker invoker, object param, QBuildParam buildParam = QBuildParam.None)
    {
        parameters.Add(parameters.Query.CreateParam(invoker, param, buildParam), CompareOperator.AndAlso);
        return parameters;
    }

    public static QParamList And(this QParamList parameters, IQueryInvoker invoker, CompareOperator compare, object param)
    {
        parameters.Add(parameters.Query.CreateParam(invoker, compare, param), CompareOperator.AndAlso);
        return parameters;
    }

    public static QParamList Or(this QParamList parameters, QCompareParameter qParam)
    {
        parameters.Add(qParam, CompareOperator.OrElse);
        return parameters;
    }

    public static QParamList Or(this QParamList parameters, Action<QCompareParameter> parameterGroup)
    {
        var qParam = new QCompareParameter() { Comparer = CompareOperator.Or };
        parameters.Add(qParam);
        parameterGroup(qParam);
        return parameters;
    }

    public static QParamList Or(this QParamList parameters, IQueryInvoker invoker, object param, QBuildParam buildParam = QBuildParam.None)
    {
        parameters.Add(parameters.Query.CreateParam(invoker, param, buildParam), CompareOperator.OrElse);
        return parameters;
    }

    public static QParamList Or(this QParamList parameters, IQueryInvoker invoker, CompareOperator compare, object param)
    {
        parameters.Add(parameters.Query.CreateParam(invoker, compare, param), CompareOperator.OrElse);
        return parameters;
    }
}

