using DataWF.Collections.Indexes;
using DataWF.Comparers;
using System;

namespace DataWF.Collections.Query.Evaluators;

public class CompareBetweenEvaluator<V> : BaseCompareEvaluator<V>
{
    public static CompareBetweenEvaluator<V> Instance = new();

    public override bool Eval(V param1, IUniversalComparer<V> comparer) => throw new NotSupportedException();

    public override bool Eval(V param1, V param2, IUniversalComparer<V> comparer) => throw new NotSupportedException();

    public override bool Eval(V param1, object param2, IUniversalComparer<V> comparer)
    {
        return param2 switch
        {
            IBetween<V> between => Eval(param1, between, comparer),
            IBetween betweenO => Eval(param1, betweenO, comparer),
            _ => throw new NotSupportedException(),
        };
    }

    public static bool Eval(V param1, IBetween between, IUniversalComparer<V> comparer)
    {
        return comparer.Compare(param1, (V)between.GetMin()) >= 0
            && comparer.Compare(param1, (V)between.GetMax()) <= 0;
    }

    public static bool Eval(V param1, IBetween<V> between, IUniversalComparer<V> comparer)
    {
        return comparer.Compare(param1, between.Min) >= 0
            && comparer.Compare(param1, between.Max) <= 0;
    }

    public override IFilteredCollection<T> Eval<T>(IIndexGetter<T, V> param1, V param2) => throw new NotSupportedException();

    public override IFilteredCollection<T> Eval<T>(IIndexGetter<T, V> param1, object param2) => param1.Select(this, param2);

    public override IFilteredCollection<T> Eval<T>(InvokerIndex<T, V> param1, V param2) => throw new NotSupportedException();

    public override IFilteredCollection<T> Eval<T>(InvokerIndex<T, V> param1, object param2) => param1.Select(this, param2);
}

