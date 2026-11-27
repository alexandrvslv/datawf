using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace DataWF.Invokers;

public class IndexPropertyInvoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllProperties
    | DynamicallyAccessedMemberTypes.AllMethods)]
T, V, K>
    : ActionIndexInvoker<T, V, K>
{
    public IndexPropertyInvoker(string name)
        : this(typeof(T).GetPropertyInfo(name, out var index)
            ?? throw new ArgumentNullException(nameof(name), "Property not found."), index)
    {
    }
    public IndexPropertyInvoker(PropertyInfo info, object? index)
        : this(info, index is null ? default! : (K)index)
    {
    }

    public IndexPropertyInvoker(PropertyInfo info, K index)
        : base(info.Name, GetExpressionGet(info), info.CanWrite ? GetExpressionSet(info) : null)
    {
        Index = index;
    }

    public static GetIndexHandler<T, K, V> GetExpressionGet(PropertyInfo info)
    {
        if (info.GetMethod == null)
            throw new ArgumentNullException(nameof(info), "Property does not have a getter.");

        var target = Expression.Parameter(typeof(T).MakeByRefType(), "target");
        var index = Expression.Parameter(typeof(K), "index");
        var property = Expression.Property(info.GetMethod.IsStatic ? null : target, info, index);

        return Expression.Lambda<GetIndexHandler<T, K, V>>(property, target, index).Compile();
    }

    public static SetIndexHandler<T, K, V>? GetExpressionSet(PropertyInfo info)
    {
        if (info.SetMethod == null)
            return null;

        var target = Expression.Parameter(typeof(T).MakeByRefType(), "target");
        var index = Expression.Parameter(typeof(K), "index");
        var value = Expression.Parameter(typeof(V), "value");
        var property = Expression.Property(info.SetMethod.IsStatic ? null : target, info, index);

        return Expression.Lambda<SetIndexHandler<T, K, V>>(Expression.Assign(property, value), target, index, value).Compile();
    }
}

