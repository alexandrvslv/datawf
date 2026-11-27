using DataWF.Comparers;
using DataWF.Collections.Indexes;
using System;

namespace DataWF.Collections.Query.Evaluators;

public class CompareIsTrueEvaluator<V> : BaseCompareEvaluator<V>
{
    public static CompareIsTrueEvaluator<V> Instance = new CompareIsTrueEvaluator<V>();

    public override bool Eval(V param1, IUniversalComparer<V> comparer) => param1 is bool boolValue
        ? boolValue
        : Convert.ToBoolean(param1);

    public override bool Eval(V param1, V param2, IUniversalComparer<V> comparer) => param1 is bool boolValue
        ? boolValue
        : Convert.ToBoolean(param1);

    public override bool Eval(V param1, object param2, IUniversalComparer<V> comparer) => param1 is bool boolValue
        ? boolValue
        : Convert.ToBoolean(param1);

    public override IFilteredCollection<T> Eval<T>(IIndexGetter<T, V> param1, V param2) => param1.Select(this, param2);

    public override IFilteredCollection<T> Eval<T>(IIndexGetter<T, V> param1, object param2) => param1.Select(this, (V)param2);

    public override IFilteredCollection<T> Eval<T>(InvokerIndex<T, V> param1, V param2) => param1.Select(this, param2);

    public override IFilteredCollection<T> Eval<T>(InvokerIndex<T, V> param1, object param2) => param1.Select(this, (V)param2);
}

