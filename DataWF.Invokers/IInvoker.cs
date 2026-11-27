using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using DataWF.Comparers;

namespace DataWF.Invokers;

public partial interface IInvoker
{
    string Name { get; }
    Type DataType { get; }
    Type TargetType { get; }
    bool CanWrite { get; }
    void SetValue(object? target, object? value);
    object? GetValue(object? target);

    void Copy(object fromItem, object toItem);
    void Copy(object fromItem, IInvoker fromInvoker, object toItem);
    IComparer CreateComparer(ListSortDirection direction = ListSortDirection.Ascending);
}

public interface IInvokerGetter<out V> : IInvoker
{
    new V GetValue(object target);
}

public interface IInvokerSetter<in V> : IInvoker
{
    void SetValue(object target, V value);
}

public interface IInvoker<V> : IInvokerGetter<V>, IInvokerSetter<V>
{
}

public interface IInvokerGetter<in T, out V> : IInvokerGetter<V>
{
    V GetValue(T target);
}

public interface IInvokerSetter<in T, in V> : IInvokerSetter<V>
{
    void SetValue(T target, V value);
}

public interface IInvoker<T, V> : IInvokerGetter<T, V>, IInvokerSetter<T, V>, IInvoker<V>, IInvoker
{
    V GetValue(scoped in T target);

    void SetValue(scoped ref T target, V value);

    V IInvokerGetter<T, V>.GetValue(T target) => GetValue(in target);

    V IInvokerGetter<V>.GetValue(object? target)
    {
        var t = (T)target!;
        return GetValue(in t);
    }

    object? IInvoker.GetValue(object? target)
    {
        var t = (T)target!;
        return GetValue(in t);
    }

    void IInvokerSetter<T, V>.SetValue(T target, V value) => SetValue(ref target, value);

    void IInvokerSetter<V>.SetValue(object? target, V value)
    {
        var t = (T)target!;
        SetValue(ref t, value);
    }

    void IInvoker.SetValue(object? target, object? value)
    {
        var t = (T)target!;
        SetValue(ref t, (V)value!);
    }

    void Copy(scoped in T fromItem, scoped ref T toItem) => SetValue(ref toItem, GetValue(in fromItem));

    void Copy(object fromItem, IInvoker fromInvoker, scoped ref T toItem) => SetValue(ref toItem, ((IInvokerGetter<V>)fromInvoker).GetValue(fromItem));

    void Copy<F>(scoped in F fromItem, IInvokerGetter<F, V> fromInvoker, scoped ref T toItem) => SetValue(ref toItem, fromInvoker.GetValue(fromItem));

    void IInvoker.Copy(object fromItem, object toItem) => SetValue(toItem, GetValue(fromItem));

    void IInvoker.Copy(object fromItem, IInvoker fromInvoker, object toItem) => SetValue(toItem, ((IInvokerGetter<V>)fromInvoker).GetValue(fromItem));

    new IComparer<T> CreateComparer(ListSortDirection direction = ListSortDirection.Ascending)
        => new InvokerComparer<T, V>(this, direction);

    IComparer IInvoker.CreateComparer(ListSortDirection direction)
        => (IComparer)CreateComparer(direction);
    
}
