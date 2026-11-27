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
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;


namespace DataWF.Collections.Query;

[Invoker(Instance = true)]
public partial class QCompareParameter : QBinaryItem<IQItem>, IComparable, IQCompareParameter
{
    protected CompareOperator compareOperator = CompareOperator.Undefined;

    public QCompareParameter()
    { }

    public QCompareParameter(IQueryInvoker invoker)
        : this()
    {
        LeftItem = invoker.CreateQInvoker();
    }

    public QCompareParameter(IQueryInvoker invoker, object value)
        : this(invoker, CompareOperator.Equal, value)
    { }

    public QCompareParameter(IQueryInvoker invoker, CompareOperator comparer, object value)
        : this(invoker)
    {
        Comparer = comparer;
        RightItem = QItemExtensions.CreateItem(value, this);
    }

    [XmlIgnore, JsonIgnore]
    public IQInvoker? LeftQInvoker => LeftItem as IQInvoker;

    [XmlIgnore, JsonIgnore]
    public IModelColumn? LeftColumn
    {
        get => (LeftQInvoker as IQColumn)?.Column;
        set => LeftItem = value!.CreateQInvoker();
    }

    [XmlIgnore, JsonIgnore]
    public IQInvoker? RightQInvoker => RightItem as IQInvoker;

    [XmlIgnore, JsonIgnore]
    public IModelColumn? RightColumn
    {
        get => (RightQInvoker as IQColumn)?.Column;
        set => RightItem = value!.CreateQInvoker();
    }

    IQueryInvoker? IQCompareParameter.ParamInvoker
    {
        get => LeftQInvoker?.Invoker ?? RightQInvoker?.Invoker;
        set => LeftColumn = (IModelColumn)value;
    }

    public object? LeftValue
    {
        get => LeftItem?.GetObjectValue((object?)null);
        set => LeftItem = QItemExtensions.CreateItem(value, RightQInvoker?.Invoker ?? LeftQInvoker?.Invoker, Comparer.Type);
    }

    public object? ParamValue
    {
        get => RightItem?.GetObjectValue((object?)null);
        set => RightItem = QItemExtensions.CreateItem(value, LeftQInvoker?.Invoker ?? RightQInvoker?.Invoker, Comparer.Type);
    }

    public CompareOperator Comparer
    {
        get => compareOperator;
        set
        {
            if (!compareOperator.Equals(value))
            {
                compareOperator = value;
                //OnPropertyChanged();
            }
        }
    }

    [XmlIgnore, JsonIgnore]
    public IQParameter? Group => Holder as IQParameter;

    [XmlIgnore, JsonIgnore]
    public virtual string ValueLeftText => LeftQInvoker?.Name;

    [XmlIgnore, JsonIgnore]
    public override bool IsFilled => (LeftItem?.IsFilled ?? false)
        && Comparer != CompareOperator.Undefined
        && ((Comparer.Type & CompareOperatorType.Is) == CompareOperatorType.Is
            || (RightItem?.IsFilled ?? false));

    public bool IsDefault { get; set; }

    public string FormatName { get; set; }

    public ParameterFlags Flags { get; set; }

    public bool IsContainer => false;

    public bool IsEnabled
    {
        get => (Flags & ParameterFlags.Enabled) == ParameterFlags.Enabled;
        set => Flags = value ? Flags | ParameterFlags.Enabled : Flags & ~ParameterFlags.Enabled;
    }

    public bool IsGlobal
    {
        get => (Flags & ParameterFlags.Global) == ParameterFlags.Global;
        set => Flags = value ? Flags | ParameterFlags.Global : Flags & ~ParameterFlags.Global;
    }

    public bool FormatEmpty
    {
        get => (Flags & ParameterFlags.FormatEmpty) == ParameterFlags.FormatEmpty;
        set => Flags = value ? Flags | ParameterFlags.FormatEmpty : Flags & ~ParameterFlags.FormatEmpty;
    }

    public bool FormatIgnore
    {
        get => (Flags & ParameterFlags.FormatIgnore) == ParameterFlags.FormatIgnore;
        set => Flags = value ? Flags | ParameterFlags.FormatIgnore : Flags & ~ParameterFlags.FormatIgnore;
    }

    public bool AlwaysTrue
    {
        get => (Flags & ParameterFlags.AlwaysTrue) == ParameterFlags.AlwaysTrue;
        set => Flags = value ? Flags | ParameterFlags.AlwaysTrue : Flags & ~ParameterFlags.AlwaysTrue;
    }

    public object? Tag { get; set; }

    private void ValueListChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        //OnPropertyChanged(sender == LeftItem ? nameof(LeftItem) : nameof(RightItem));
    }

    public bool IsProperty(IInvoker invoker)
    {
        return (LeftItem is IQInvoker lqColumn && lqColumn.Invoker == invoker) ||
            (RightItem is IQInvoker rqColumn && rqColumn.Invoker == invoker);
    }

    public override object GetObjectValue<T>(T row)
        => Compare(row);
    
    public override object GetObjectValue(QTuple row)
        => Compare(row);

    public override bool Compare<T>(T item)
        => LeftItem?.Compare<T>(item, RightItem, Comparer) ?? true;

    public override bool Compare(QTuple item)
        => LeftItem?.Compare(item, RightItem, Comparer) ?? true;

    public override bool Compare<T>(T item, CompareOperator comparer)
        => comparer.GetEvaluator<bool>().Eval(Compare<T>(item));

    public override bool Compare(QTuple item, CompareOperator comparer)
        => comparer.GetEvaluator<bool>().Eval(Compare(item));

    public override bool Compare<T>(T item, IQItem rightItem, CompareOperator comparer)
        => comparer.GetEvaluator<bool>().Eval(Compare<T>(item), rightItem.Compare(item));

    public override bool Compare(QTuple item, IQItem rightItem, CompareOperator comparer)
        => comparer.GetEvaluator<bool>().Eval(Compare(item), rightItem.Compare(item));

    public IEnumerable<T> Filter<T>(IEnumerable<T> items)
    {
        if (LeftItem == null)
            return items;

        return GetFilter<T>().Filter(items, this);
    }

    public override IQParameterFilter<T> GetFilter<T>(IQItem rightItem) => LeftItem.GetFilter<T>(rightItem);

    public IQParameterFilter<T> GetFilter<T>() => LeftItem.GetFilter<T>(RightItem);

    public void ParametersListChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        //if (e.ListChangedType == ListChangedType.ItemAdded)
        //{
        //    parameters[e.NewIndex].Group = this;
        //}
        //else if (e.ListChangedType == ListChangedType.ItemDeleted)
        //{
        //    if (e.NewIndex >= 0)
        //        parameters[e.NewIndex].Group = null;
        //}
        //else
        //{
        // OnPropertyChanged(nameof(Parameters));
        //}
    }

    public override string ToString()
    {
        return $"{LeftItem} {Comparer} {RightItem}";
    }

    public string FormatValue(IQItem value, object? context = null)
    {
        if (value == null)
            return string.Empty;
        if (value is IQQuery squery && squery.QTable != null)
        {
            if (squery.Columns.Count() == 0 && squery.QTable.Table?.PrimaryKey is { } primaryKey)
                squery.Columns.Add(primaryKey.CreateQInvoker());
            return $"({squery.Format(context)})";
        }
        else if (value is QExpression)
            return $"({value.Format(context)})";
        else
            return value.Format(context);
    }

    public override string Format(object? command = null)
    {
        string leftArg = FormatValue(LeftItem, command);
        if (string.IsNullOrEmpty(leftArg))
        {
            return string.Empty;
        }
        string rightArg = compareOperator.Type == CompareOperatorType.Is ? "null" : FormatValue(RightItem, command);
        return string.IsNullOrEmpty(rightArg) ? string.Empty : string.Format("{0} {1} {2}", leftArg, compareOperator.Format(), rightArg);
    }

    public override void Dispose()
    {
    }

    public override void Add(IQItem value)
    {
        if (value is IQCompareOperator qCompareOperator)
            Comparer = qCompareOperator.CompareOperator;
        else
            base.Add(value);
    }

    public IQParameter WithTag(object tag)
    {
        Tag = tag;
        return this;
    }

    public void Replace(IQParameter toRelate, IQParameter forReplace)
    {
        throw new NotSupportedException();
    }



    public class ColumnNameInvoker : Invoker<QCompareParameter, string?>
    {
        public static readonly ColumnNameInvoker Instance = new();
        public override string Name => "LeftColumn.Name";

        public override bool CanWrite => false;

        public override string? GetValue(in QCompareParameter target) => target.LeftQInvoker?.Invoker.Name;

        public override void SetValue(ref QCompareParameter target, string? value) { }
    }
}

[Flags]
public enum ParameterFlags
{
    None = 0,
    Enabled = 1,
    Global = 2,
    FormatEmpty = 4,
    FormatIgnore = 8,
    AlwaysTrue = 16,
    Frozen = 32
}

