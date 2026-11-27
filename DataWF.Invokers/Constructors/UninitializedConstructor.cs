using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DataWF.Invokers;

public class UninitializedConstructor<[
    DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
    T> : RuntimeConstructor<T>
{
    public static UninitializedConstructor<T> Instance { get; } = new();
    public override string Types => "Uninitialized";

    public override T Create()
        => (T)RuntimeHelpers.GetUninitializedObject(typeof(T))!;

    public override T Create(params object[]? args)
        => (T)RuntimeHelpers.GetUninitializedObject(typeof(T))!;
}
