
namespace DataWF.Invokers;

public interface IIndexInvoker : IInvoker
{
    object? Index { get; set; }

    object? GetValue(object? target, object index);

    void SetValue(object? target, object index, object? value);
}

public interface IIndexInvoker<T, V, K> : IIndexInvoker, IInvoker<T, V>
{
    new K Index { get; set; }

    object? IIndexInvoker.Index
    {
        get => Index;
        set => Index = value is null ? default! : (K)value;
    }

    V GetValue(scoped in T target, K index);

    V GetValue(T target, K index) => GetValue(in target, index);

    object? IIndexInvoker.GetValue(object? target, object index)
    {
        var t = (T)target!;
        return GetValue(in t, (K)index);
    }

    V IInvoker<T, V>.GetValue(scoped in T target) => GetValue(in target, Index);

    V IInvokerGetter<T, V>.GetValue(T target) => GetValue(in target, Index);

    V IInvokerGetter<V>.GetValue(object? target)
    {
        var t = (T)target!;
        return GetValue(in t, Index);
    }

    object? IInvoker.GetValue(object? target)
    {
        var t = (T)target!;
        return GetValue(in t, Index);
    }

    void SetValue(scoped ref T target, K index, V value);

    void SetValue(T target, K index, V value) => SetValue(ref target, index, value);

    void IInvoker<T, V>.SetValue(scoped ref T target, V value) => SetValue(ref target, Index, value);

    void IInvoker.SetValue(object? target, object? value)
    {
        var t = (T)target!;
        SetValue(ref t, Index, (V)value!);
    }

    void IIndexInvoker.SetValue(object? target, object index, object? value)
    {
        var t = (T)target!;
        SetValue(ref t, (K)index, (V)value!);
    }
}

