using System;

namespace DataWF.Invokers;

public class ActivatorConstructor<T> : RuntimeConstructor<T>
{
    public static ActivatorConstructor<T> Instance { get; } = new();
    public override string Types => "GenericActivator";

    public override T Create()
        => (T)System.Activator.CreateInstance<T>()!;

    public override T Create(params object[]? args)
        => (T)System.Activator.CreateInstance(typeof(T), args)!;
}

public class ActivatorConstructor : IRuntimeConstructor
{
    public ActivatorConstructor(Type type)
    {
        Type = type;
    }

    public Type Type { get; }

    public string Types => "Activator";

    public object? Create()
        => System.Activator.CreateInstance(Type)!;

    public object? Create(params object[]? args)
        => System.Activator.CreateInstance(Type, args)!;

}