using System.Runtime.CompilerServices;

namespace DataWF.Invokers;

public interface IValueTypeInvoker<T, V> : IInvoker<T, V>//, IInvoker<V>
    where T : struct
{
    object? IInvoker.GetValue(object? target) => GetValue(in Unsafe.Unbox<T>(target!));

    V IInvokerGetter<V>.GetValue(object? target) => GetValue(in Unsafe.Unbox<T>(target!));

    void IInvoker.SetValue(object? target, object? value) => SetValue(ref Unsafe.Unbox<T>(target!), (V)value!);

    void IInvokerSetter<V>.SetValue(object? target, V value) => SetValue(ref Unsafe.Unbox<T>(target!), value);

    void IInvoker.Copy(object fromItem, object toItem)
            => SetValue(ref Unsafe.Unbox<T>(toItem), GetValue(in Unsafe.Unbox<T>(fromItem)));

    void IInvoker.Copy(object fromItem, IInvoker fromInvoker, object toItem)
        => SetValue(ref Unsafe.Unbox<T>(toItem), ((IInvokerGetter<V>)fromInvoker).GetValue(fromItem));
}

