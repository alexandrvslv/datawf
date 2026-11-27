using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using DataWF.Invokers;

namespace DataWF.Comparers;

public abstract class UniversalComparer<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.Interfaces |
        DynamicallyAccessedMemberTypes.PublicProperties |
        DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T> : IUniversalComparer<T>
{
    public static readonly UniversalComparer<T> Default = GetComparer();

    private static UniversalComparer<T> GetComparer()
    {
        var type = typeof(T);

        if (type == typeof(string))
            return (UniversalComparer<T>)(object)StringUniversalComparer.Instance;

        if (type == typeof(int))
            return (UniversalComparer<T>)(object)Int32UniversalComparer.Instance;

        if (type == typeof(int?))
            return (UniversalComparer<T>)(object)Int32NUniversalComparer.Instance;

        if (type == typeof(uint))
            return (UniversalComparer<T>)(object)UInt32UniversalComparer.Instance;

        if (type == typeof(uint?))
            return (UniversalComparer<T>)(object)UInt32NUniversalComparer.Instance;

        // if (type == typeof(DateTime))
        //     return (UniversalComparer<T>)(object)DateTimePartComparer.Default;

        // if (type == typeof(DateTime?))
        //     return (UniversalComparer<T>)(object)NDateTimePartComparer.Default;

        if (type == typeof(byte[]))
            return (UniversalComparer<T>)(object)ByteArrayComparer.Instance;

        if (Nullable.GetUnderlyingType(type) is { } underlyingType
        && ImplementsEquatableAndComparable(underlyingType))
        {
            var implNullableType = typeof(ImplementedNullableUniversalComparer<>).MakeGenericType(underlyingType);
            return (UniversalComparer<T>)implNullableType.GetField("Instance")!.GetValue(null)!;
        }

        if (type.IsAssignableTo(typeof(IEquatable<T>)) && type.IsAssignableTo(typeof(IComparable<T>)))
        {
            var implType = typeof(ImplementedUniversalComparer<>).MakeGenericType(type);
            return (UniversalComparer<T>)implType.GetField("Instance")!.GetValue(null)!;
        }

        if (type.IsAssignableTo(typeof(IEnumerable)))
            return CreateSequenceComparer(type);

        return DefaultUniversalComparer<T>.Instance;
    }

    private static bool ImplementsEquatableAndComparable(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        bool hasEquatable = false;
        bool hasComparable = false;

        foreach (var iface in type.GetInterfaces())
        {
            if (!iface.IsGenericType) continue;

            var def = iface.GetGenericTypeDefinition();
            if (def == typeof(IEquatable<>) && iface.GetGenericArguments()[0] == type)
                hasEquatable = true;
            else if (def == typeof(IComparable<>) && iface.GetGenericArguments()[0] == type)
                hasComparable = true;

            if (hasEquatable && hasComparable)
                return true;
        }

        return false;
    }

    private static UniversalComparer<T> CreateSequenceComparer(Type type)
    {
        return (UniversalComparer<T>)InvokeProvider.CreateObject(typeof(SequenceComparer<,>).MakeGenericType(type, type.GetItemType()))!;
    }

    public abstract bool DefaultEquality { get; }
    public abstract bool DefaultOrder { get; }

    public virtual string Name
    {
        get => typeof(T).Name;
        set { }
    }

    public int Compare(object? x, object? y)
    {
        if (x is null) return y is null ? 0 : -1;
        if (y is null) return 1;
        return Compare((T)x, (T)y);
    }

    public abstract int Compare(T? x, T? y);

    bool IEqualityComparer.Equals(object? x, object? y)
    {
        if (x is null) return y is null;
        if (y is null) return false;
        return Equals((T)x, (T)y);
    }

    public abstract bool Equals(T? x, T? y);

    public virtual bool Equals(T? x, object? y)
        => y switch
        {
            T t => Equals(x, t),
            IEquatable<T> e => e.Equals(x),
            _ => object.Equals(x, y)
        };

    public int GetHashCode(object obj) => GetHashCode((T)obj);

    public abstract int GetHashCode([DisallowNull] T obj);
}

public sealed class InterfaceUniversalComparer<T> : UniversalComparer<T>
{
    private IEqualityComparer<T> equalityComparer;
    private IComparer<T> comparer;

    public InterfaceUniversalComparer(IEqualityComparer<T> equalityComparer, IComparer<T> comparer)
    {
        this.equalityComparer = equalityComparer;
        this.comparer = comparer;
    }

    public override bool DefaultEquality => equalityComparer == EqualityComparer<T>.Default;

    public override bool DefaultOrder => comparer == Comparer<T>.Default;

    public override int Compare(T? x, T? y) => comparer.Compare(x, y);

    public override bool Equals(T? x, T? y) => equalityComparer.Equals(x, y);

    public override int GetHashCode([DisallowNull] T obj) => equalityComparer.GetHashCode(obj);

}

public sealed class DefaultUniversalComparer<T> : UniversalComparer<T>
{
    public static readonly DefaultUniversalComparer<T> Instance = new();

    public override bool DefaultEquality => true;

    public override bool DefaultOrder => true;

    public override int Compare(T? x, T? y) => Comparer<T>.Default.Compare(x, y);

    public override bool Equals(T? x, T? y) => EqualityComparer<T>.Default.Equals(x, y);

    public override int GetHashCode([DisallowNull] T obj) => EqualityComparer<T>.Default.GetHashCode(obj);

}

public sealed class ImplementedUniversalComparer<T> : UniversalComparer<T>
    where T : IEquatable<T>, IComparable<T>
{
    public static readonly ImplementedUniversalComparer<T> Instance = new();

    public override bool DefaultEquality => true;

    public override bool DefaultOrder => true;

    public override int Compare(T? x, T? y)
    {
        if (x is null) return y is null ? 0 : -1;
        if (y is null) return 1;
        return x.CompareTo(y);
    }

    public override bool Equals(T? x, T? y)
    {
        if (x is null) return y is null;
        if (y is null) return false;
        return x.Equals(y);
    }

    public override int GetHashCode([DisallowNull] T obj) => obj.GetHashCode();

}

public sealed class ImplementedNullableUniversalComparer<T> : UniversalComparer<T?>
    where T : struct, IEquatable<T>, IComparable<T>
{
    public static readonly ImplementedNullableUniversalComparer<T> Instance = new();

    public override bool DefaultEquality => true;

    public override bool DefaultOrder => true;

    public override int Compare(T? x, T? y)
    {
        if (x is null) return y is null ? 0 : -1;
        if (y is null) return 1;
        return x.GetValueOrDefault().CompareTo(y.GetValueOrDefault());
    }

    public override bool Equals(T? x, T? y)
    {
        if (x is null) return y is null;
        if (y is null) return false;
        return x.GetValueOrDefault().Equals(y.GetValueOrDefault());
    }

    public override int GetHashCode([DisallowNull] T? obj) => obj.GetValueOrDefault().GetHashCode();

}