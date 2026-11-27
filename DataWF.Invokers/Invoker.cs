using System;

namespace DataWF.Invokers;

public abstract class Invoker<T, V> : IInvoker<T, V>, IInvoker<V>
{
    public abstract string Name { get; }

    public virtual Type DataType => typeof(V);

    public virtual Type TargetType => typeof(T);

    public abstract bool CanWrite { get; }

    public abstract V GetValue(scoped in T target);

    public abstract void SetValue(scoped ref T target, V value);

    public override string ToString() => $"{typeof(T).Name}.{Name} {typeof(V).Name}";
}
