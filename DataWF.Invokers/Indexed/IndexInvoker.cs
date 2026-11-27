namespace DataWF.Invokers;

public abstract class IndexInvoker<T, V, K> : Invoker<T, V>, IIndexInvoker<T, V, K>, IInvoker<V>
{
    public IndexInvoker()
    {
        Index = default!;
    }

    public K Index { get; set; }

    public abstract V GetValue(scoped in T target, K index);

    public abstract void SetValue(scoped ref T target, K index, V value);

    public override V GetValue(scoped in T target) => GetValue(in target, Index);

    public override void SetValue(scoped ref T target, V value) => SetValue(ref target, Index, value);
    
}
