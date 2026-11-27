using System;
using System.Reflection;

namespace DataWF.Invokers;

public class ReflectionFieldInvoker<T, V> : IInvoker<T, V>
{
    private readonly FieldInfo fieldInfo;

    public ReflectionFieldInvoker(string fieldName)
        : this(typeof(T).GetField(fieldName)!)
    { }

    public ReflectionFieldInvoker(FieldInfo fieldInfo)
    {
        this.fieldInfo = fieldInfo;
    }

    public Type DataType => fieldInfo.FieldType;

    public Type TargetType => fieldInfo.DeclaringType!;

    public bool CanWrite => !fieldInfo.IsInitOnly;

    public string Name
    {
        get => fieldInfo.Name;
        set { }
    }

    public object? GetValue(object? target) => fieldInfo.GetValue(target);

    public V GetValue(T target) => (V)GetValue((object?)target)!;

    public V GetValue(scoped in T target) => (V)GetValue((object?)target)!;

    public void SetValue(object? target, object? value) => fieldInfo.SetValue(target, value);

    public void SetValue(T target, V value) => SetValue((object?)target, (object?)value);

    public void SetValue(scoped ref T target, V value) => fieldInfo.SetValue(target, value);

    public void SetValue(object target, V value) => fieldInfo.SetValue(target, value);

    V IInvokerGetter<V>.GetValue(object target) => (V)fieldInfo.GetValue(target)!;
}
