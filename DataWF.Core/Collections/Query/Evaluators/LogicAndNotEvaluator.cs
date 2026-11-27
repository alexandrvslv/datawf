using System;
using System.Collections.Generic;
using System.Linq;
using DataWF.Comparers;

namespace DataWF.Collections.Query.Evaluators;

public class LogicAndNotEvaluator : BaseCompareEvaluator<bool>
{
    public static readonly LogicAndNotEvaluator Instance = new();

    public override bool Eval(bool param1, IUniversalComparer<bool> comparer) => throw new NotSupportedException();

    public override bool Eval(bool param1, bool param2, IUniversalComparer<bool> comparer) => param1 && !param2;

    public override bool Eval(bool param1, object param2, IUniversalComparer<bool> comparer) => param1 && !System.Convert.ToBoolean(param2);

    public override IFilteredCollection<T> Eval<T>(Indexes.IIndexGetter<T, bool> param1, bool param2) => throw new NotImplementedException();

    public override IFilteredCollection<T> Eval<T>(Indexes.IIndexGetter<T, bool> param1, object param2) => throw new NotImplementedException();

    public override IFilteredCollection<T> Eval<T>(Indexes.InvokerIndex<T, bool> param1, bool param2) => throw new NotImplementedException();

    public override IFilteredCollection<T> Eval<T>(Indexes.InvokerIndex<T, bool> param1, object param2) => throw new NotImplementedException();

    public IEnumerable<T> Eval<T>(IEnumerable<T> items, IQParameter lparam, IQParameter rparam)
        => lparam.Filter<T>(items).Intersect(items.Except(rparam.Filter<T>(items)));

}

