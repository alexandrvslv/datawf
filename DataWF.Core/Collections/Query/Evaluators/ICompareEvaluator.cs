using DataWF.Collections.Indexes;
using DataWF.Comparers;

namespace DataWF.Collections.Query.Evaluators;

public interface ICompareEvaluator<V>: IEvaluator<V, V, bool>, IEvaluator<V, V, IUniversalComparer<V>, bool>
{
    bool Eval(V param1, IUniversalComparer<V> comparer);
    new bool Eval(V param1, V param2, IUniversalComparer<V> comparer);
    bool Eval(V param1, object param2, IUniversalComparer<V> comparer);


    IFilteredCollection<T> Eval<T>(IIndexGetter<T, V> param1, V param2);
    IFilteredCollection<T> Eval<T>(IIndexGetter<T, V> param1, object param2);

    IFilteredCollection<T> Eval<T>(InvokerIndex<T, V> param1, V param2);
    IFilteredCollection<T> Eval<T>(InvokerIndex<T, V> param1, object param2);

}


