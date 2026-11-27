using System;
using System.Reflection;

namespace DataWF.Invokers;

public class ReflectionPropertyInvoker<T, V> : IInvoker<T, V>
{
    private readonly PropertyInfo property;

    public ReflectionPropertyInvoker(string propertyName)
        : this(typeof(T).GetProperty(propertyName)!)
    { }
    public ReflectionPropertyInvoker(PropertyInfo propertyInfo)
    {
        this.property = propertyInfo;
    }

    public Type DataType => property.PropertyType;

    public Type TargetType => property.DeclaringType!;

    public bool CanWrite => property.CanWrite;

    public string Name
    {
        get => property.Name;
        set { }
    }

    public object? GetValue(object? target) => property.GetValue(target, null);

    public V GetValue(T target) => (V)property.GetValue(target, null)!;

    public V GetValue(scoped in T target) => (V)property.GetValue(target, null)!;

    public void SetValue(object? target, object? value) => property.SetValue(target, value, null);

    public void SetValue(T target, V value) => property.SetValue(target, value, null);

    public void SetValue(scoped ref T target, V value) => property.SetValue(target, value, null);

    public void SetValue(object target, V value) => property.SetValue(target, value, null);

    V IInvokerGetter<V>.GetValue(object target) => (V)property.GetValue(target, null)!;
}
