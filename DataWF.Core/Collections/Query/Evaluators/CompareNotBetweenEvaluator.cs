using DataWF.Comparers;
using DataWF.Collections.Indexes;
using System;
using System.Collections.Generic;

namespace DataWF.Collections.Query.Evaluators;

public class CompareNotBetweenEvaluator<V> : BaseCompareEvaluator<V>
{
    public static CompareNotBetweenEvaluator<V> Instance = new();

    public override bool Eval(V param1, IUniversalComparer<V> comparer) => throw new NotSupportedException();

    public override bool Eval(V param1, V param2, IUniversalComparer<V> comparer) => throw new NotSupportedException();

    public override bool Eval(V param1, object param2, IUniversalComparer<V> comparer)
    {
        if (param2 is IBetween<V> between)
            return comparer.Compare(param1, between.GetMin()) < 0
                    || comparer.Compare(param1, between.GetMax()) > 0;
        return comparer.Compare(param1, (V)((IBetween)param2).GetMin()) < 0
                    && comparer.Compare(param1, (V)((IBetween)param2).GetMax()) > 0;
    }

    public override IFilteredCollection<T> Eval<T>(IIndexGetter<T, V> param1, V param2) => throw new NotSupportedException();

    public override IFilteredCollection<T> Eval<T>(IIndexGetter<T, V> param1, object param2) => param1.Select(this, param2);

    public override IFilteredCollection<T> Eval<T>(InvokerIndex<T, V> param1, V param2) => throw new NotSupportedException();

    public override IFilteredCollection<T> Eval<T>(InvokerIndex<T, V> param1, object param2) => param1.Select(this, param2);
}


