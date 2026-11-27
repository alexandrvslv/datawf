using DataWF.Collections.Indexes;
using DataWF.Comparers;

namespace DataWF.Collections.Query.Evaluators;

public class CompareIsNullEvaluator<V> : BaseCompareEvaluator<V>
{
    public static CompareIsNullEvaluator<V> Instance = new();

    public override bool Eval(V param1, IUniversalComparer<V> comparer) => comparer.Equals(param1, default(V));

    public override bool Eval(V param1, V param2, IUniversalComparer<V> comparer) => comparer.Equals(param1, default(V));

    public override bool Eval(V param1, object param2, IUniversalComparer<V> comparer) => comparer.Equals(param1, default(V));

    public override IFilteredCollection<T> Eval<T>(IIndexGetter<T, V> param1, V param2) => param1.Select(default(V));

    public override IFilteredCollection<T> Eval<T>(IIndexGetter<T, V> param1, object param2) => param1.Select(default(V));

    public override IFilteredCollection<T> Eval<T>(InvokerIndex<T, V> param1, V param2) => param1.Select(default(V));

    public override IFilteredCollection<T> Eval<T>(InvokerIndex<T, V> param1, object param2) => param1.Select(default(V));
}

