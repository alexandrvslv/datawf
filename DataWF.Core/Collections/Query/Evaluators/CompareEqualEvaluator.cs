using DataWF.Collections.Indexes;
using DataWF.Comparers;

namespace DataWF.Collections.Query.Evaluators;

public class CompareEqualEvaluator<V> : BaseCompareEvaluator<V>
{
    public static readonly CompareEqualEvaluator<V> Instance = new();
    public override bool Eval(V param1, IUniversalComparer<V> comparer) => comparer.Equals(param1, default(V));

    public override bool Eval(V param1, V param2, IUniversalComparer<V> comparer) => comparer.Equals(param1, param2);

    public override bool Eval(V param1, object param2, IUniversalComparer<V> comparer) => comparer.Equals(param1, param2);

    public override IFilteredCollection<T> Eval<T>(IIndexGetter<T, V> param1, V param2) => param1.Select(param2);

    public override IFilteredCollection<T> Eval<T>(IIndexGetter<T, V> param1, object param2) => param1.Select((V)param2);

    public override IFilteredCollection<T> Eval<T>(InvokerIndex<T, V> param1, V param2) => param1.Select(param2);

    public override IFilteredCollection<T> Eval<T>(InvokerIndex<T, V> param1, object param2) => param1.Select((V)param2);

}

