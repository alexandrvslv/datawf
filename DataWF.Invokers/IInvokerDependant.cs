namespace DataWF.Invokers;

public interface IInvokerDependant
{
    IInvoker Invoker { get; }
}

public interface IInvokerGetterDependant<in T, V> : IInvokerDependant
{
    new IInvokerGetter<T, V> Invoker { get; }
    IInvoker IInvokerDependant.Invoker => Invoker;
}

public interface IInvokerDependant<T, V> : IInvokerGetterDependant<T, V>, IInvokerDependant
{
    new IInvoker<T, V> Invoker { get; }

    IInvokerGetter<T, V> IInvokerGetterDependant<T, V>.Invoker => Invoker;

}
