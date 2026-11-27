using System.Runtime.CompilerServices;

namespace DataWF.Invokers;

public interface IValueTypeIndexInvoker<T, V, K> : IIndexInvoker<T, V, K>
    where T : struct
{
    object? IIndexInvoker.GetValue(object? target, object index)
        => GetValue(in Unsafe.Unbox<T>(target!), (K)index);

    object? IInvoker.GetValue(object? target)
        => GetValue(in Unsafe.Unbox<T>(target!), Index);
    V IInvokerGetter<V>.GetValue(object? target)
        => GetValue(in Unsafe.Unbox<T>(target!), Index);

    void IIndexInvoker.SetValue(object? target, object index, object? value)
        => SetValue(ref Unsafe.Unbox<T>(target!), (K)index, (V)value!);

    void IInvoker.SetValue(object? target, object? value)
        => SetValue(ref Unsafe.Unbox<T>(target!), Index, (V)value!);

    void IInvokerSetter<V>.SetValue(object? target, V value)
        => SetValue(ref Unsafe.Unbox<T>(target!), Index, value);
}
