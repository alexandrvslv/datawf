using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace DataWF.Invokers;

public class FieldInvoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllFields)] T, V>
    : ActionInvoker<T, V>
{
    public FieldInvoker(string fieldName)
        : this(typeof(T).GetField(fieldName)!)
    { }

    public FieldInvoker(FieldInfo info)
        : base(info.Name, GetExpressionGet(info), GetExpressionSet(info))
    { }

    public static GetHandler<T, V> GetExpressionGet(FieldInfo info)
    {
        var param = Expression.Parameter(typeof(T).MakeByRefType(), "target");
        var property = Expression.Field(info.IsStatic ? null : param, info);
        return Expression.Lambda<GetHandler<T, V>>(property, param).Compile();
    }

    public static SetHandler<T, V>? GetExpressionSet(FieldInfo info)
    {
        if (info.IsInitOnly)
        {
            return null;
        }

        var param = Expression.Parameter(typeof(T).MakeByRefType(), "target");
        var value = Expression.Parameter(typeof(V), "value");
        var proeprty = Expression.Field(info.IsStatic ? null : param, info);

        return Expression.Lambda<SetHandler<T, V>>(Expression.Assign(proeprty, value), param, value).Compile();
    }
}
