using DataWF.Comparers;
using DataWF.Collections.Indexes;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Collections.Query.Evaluators;

public class CompareLessOrEqualEvaluator<V> : BaseCompareEvaluator<V>
{
    public static CompareLessOrEqualEvaluator<V> Instance = new();

    public override bool Eval(V param1, IUniversalComparer<V> comparer) => Eval(param1, default(V)!, comparer);

    public override bool Eval(V param1, V param2, IUniversalComparer<V> comparer) => comparer.Compare(param1, param2) <= 0;

    public override bool Eval(V param1, object param2, IUniversalComparer<V> comparer) => Eval(param1, (V)param2, comparer);

    public override IFilteredCollection<T> Eval<T>(IIndexGetter<T, V> param1, V param2) => param1.Select(this, param2);

    public override IFilteredCollection<T> Eval<T>(IIndexGetter<T, V> param1, object param2) => param1.Select(this, (V)param2);

    public override IFilteredCollection<T> Eval<T>(InvokerIndex<T, V> param1, V param2) => param1.Select(this, param2);

    public override IFilteredCollection<T> Eval<T>(InvokerIndex<T, V> param1, object param2) => param1.Select(this, (V)param2);
}


