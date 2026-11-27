using System.Runtime.CompilerServices;

namespace DataWF.Invokers.Extensions;

public static class InvokerExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V GetValue<T, V>(this T target, IInvoker<T, V> invoker)
        => invoker.GetValue(in target);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V? GetValueNullable<T, V>(this T? target, IInvoker<T, V> invoker)
        => target == null ? default : invoker.GetValue(in target);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetValue<T, V>(this T target, IInvoker<T, V> invoker, V value)
        => invoker.SetValue(ref target, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static V GetValue<T, V, I>(this T target, IIndexInvoker<T, V, I> invoker, I index)
        => invoker.GetValue(in target, index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetValue<T, V, I>(this T target, IIndexInvoker<T, V, I> invoker, I index, V value)
    => invoker.SetValue(ref target, index, value);
}

