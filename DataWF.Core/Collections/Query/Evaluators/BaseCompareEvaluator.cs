using DataWF.Collections.Indexes;
using DataWF.Comparers;

namespace DataWF.Collections.Query.Evaluators;

public abstract class BaseCompareEvaluator<V> : ICompareEvaluator<V>
{
    public IUniversalComparer<V> Comparer { get; set; } = UniversalComparer<V>.Default;

    public virtual bool Eval(V param1, V param2) => Eval(param1, param2, Comparer);
    public virtual bool Eval(V param1, object param2) => Eval(param1, param2, Comparer);
    public abstract bool Eval(V param1, IUniversalComparer<V> comparer);
    public abstract bool Eval(V param1, V param2, IUniversalComparer<V> comparer);
    public abstract bool Eval(V param1, object param2, IUniversalComparer<V> comparer);
    public abstract IFilteredCollection<T> Eval<T>(IIndexGetter<T, V> param1, V param2);
    public abstract IFilteredCollection<T> Eval<T>(IIndexGetter<T, V> param1, object param2);
    public abstract IFilteredCollection<T> Eval<T>(InvokerIndex<T, V> param1, V param2);
    public abstract IFilteredCollection<T> Eval<T>(InvokerIndex<T, V> param1, object param2);
}

