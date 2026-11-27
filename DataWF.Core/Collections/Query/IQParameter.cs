using DataWF.Collections.Query.Evaluators;
using DataWF.Comparers;
using DataWF.Invokers;
using System.Collections.Generic;

namespace DataWF.Collections.Query;

public interface IQParameter : IQItemList, IQFrozenable
{
    bool IsContainer { get; }
    bool IsEnabled { get; set; }
    IEnumerable<T> Filter<T>(IEnumerable<T> items);

    bool IsProperty(IInvoker invoker);
    void Replace(IQParameter toRelate, IQParameter forReplace);
}

public interface IQCompareParameter : IQParameter, IQBinaryItem
{
    IQueryInvoker? ParamInvoker { get; set; }
    CompareOperator Comparer { get; set; }
    object ParamValue { get; set; }
    string FormatName { get; set; }
    bool IsGlobal { get; set; }
    bool FormatEmpty { get; set; }
    bool FormatIgnore { get; set; }
    bool AlwaysTrue { get; set; }
    object? Tag { get; set; }

    IQParameter WithTag(object tag);
}
