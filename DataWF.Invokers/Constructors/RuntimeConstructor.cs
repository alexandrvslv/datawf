namespace DataWF.Invokers;

public abstract class RuntimeConstructor<T> : IRuntimeConstructor<T>
{
    public abstract string Types { get; }

    public abstract T Create();

    public abstract T Create(params object[]? args);

    object? IRuntimeConstructor.Create() => Create();

    object? IRuntimeConstructor.Create(params object[]? args) => Create(args);
}
