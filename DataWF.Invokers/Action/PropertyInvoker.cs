using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace DataWF.Invokers;

public class PropertyInvoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllProperties)] T, V>
    : ActionInvoker<T, V>
{
    public PropertyInvoker(string propertyName)
        : this(typeof(T).GetProperty(propertyName)!)
    { }

    public PropertyInvoker(PropertyInfo info)
        : base(info.Name, GetExpressionGet(info), info.CanWrite ? GetExpressionSet(info) : null)
    { }

    //https://www.codeproject.com/Articles/584720/Expression-Based-Property-Getters-and-Setters
    private static GetHandler<T, V> GetExpressionGet(PropertyInfo info)
    {
        if (info.GetMethod == null)
            throw new ArgumentNullException(nameof(info), "Property does not have a getter.");
        var param = Expression.Parameter(typeof(T).MakeByRefType());
        return Expression.Lambda<GetHandler<T, V>>(Expression.Property(info.GetMethod.IsStatic ? null : param, info), param).Compile();
    }

    private static SetHandler<T, V>? GetExpressionSet(PropertyInfo info)
    {
        if (info.SetMethod == null)
            return null;
        var param = Expression.Parameter(typeof(T).MakeByRefType());
        var value = Expression.Parameter(typeof(V));
        return Expression.Lambda<SetHandler<T, V>>(
            Expression.Assign(Expression.Property(info.SetMethod.IsStatic ? null : param, info), value),
            param, value).Compile();
    }
}

public class ValueTypePropertyInvoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllProperties)] T, V>
    : PropertyInvoker<T, V>, IValueTypeInvoker<T, V>
    where T : struct
{
    public ValueTypePropertyInvoker(string propertyName) : base(propertyName)
    { }

    public ValueTypePropertyInvoker(PropertyInfo info) : base(info)
    { }
}

