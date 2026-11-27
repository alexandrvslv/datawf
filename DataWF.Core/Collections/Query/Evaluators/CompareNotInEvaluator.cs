using DataWF.Comparers;
using DataWF.Collections.Indexes;
using DataWF.Invokers;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections.Query.Evaluators;

public class CompareNotInEvaluator<V> : BaseCompareEvaluator<V>
{
    public static CompareNotInEvaluator<V> Instance = new();

    public override bool Eval(V param1, IUniversalComparer<V> comparer) => throw new NotSupportedException();

    public override bool Eval(V param1, V param2, IUniversalComparer<V> comparer) => !comparer.Equals(param1, param2);

    public override bool Eval(V param1, object param2, IUniversalComparer<V> comparer)
    {
        if (param2 is IEnumerable<V> typedEnumerable)
        {
            foreach (var item in typedEnumerable)
            {
                if (comparer.Equals(param1, item))
                    return false;
            }
        }
        else
        {
            foreach (var item in (IEnumerable)param2)
            {
                var comp = default(V);
                if (item is IQValue<V> valued)
                    comp = valued.Value;
                else if (item is IInvokerGetter<V> valuedGetter)
                    comp = valuedGetter.GetValue(null);
                else
                    comp = (V)item;

                if (comparer.Equals(param1, comp))
                    return false;
            }
        }
        return true;
    }

    public override IFilteredCollection<T> Eval<T>(IIndexGetter<T, V> param1, V param2) => param1.Select(CompareNotEqualEvaluator<V>.Instance, param2);

    public override IFilteredCollection<T> Eval<T>(IIndexGetter<T, V> param1, object param2) => param1.Select(this, param2);

    public override IFilteredCollection<T> Eval<T>(InvokerIndex<T, V> param1, V param2) => param1.Select(CompareNotEqualEvaluator<V>.Instance, param2);

    public override IFilteredCollection<T> Eval<T>(InvokerIndex<T, V> param1, object param2) => param1.Select(this, param2);
}


