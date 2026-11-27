using System.Linq.Expressions;
using System.Reflection;

namespace DataWF.Invokers;

public class MethodInvoker<T, V>(MethodInfo infoGet)
    : ActionIndexInvoker<T, V, object[]>(infoGet.Name, GetExpressionCall(infoGet))
{

    public V Invoke(scoped in T target, params object[] parameters) => GetAction(in target, parameters);

    public object? Invoke(object target, params object[] parameters)
    {
        var t = (T)target;
        return Invoke(in t, parameters);
    }

    private static GetIndexHandler<T, object[], V> GetExpressionCall(MethodInfo info)
    {
        var param = Expression.Parameter(typeof(T).MakeByRefType());
        var ps = info.GetParameters();

        var args = new Expression[ps.Length];
        var param2 = Expression.Parameter(typeof(object[]));
        for (int i = 0; i < ps.Length; i++)
        {
            args[i] = Expression.Convert(Expression.ArrayIndex(param2, Expression.Constant(i)), ps[i].ParameterType);
        }
        return Expression.Lambda<GetIndexHandler<T, object[], V>>(Expression.Call(info.IsStatic ? null : param, info, args), param, param2).Compile();
    }

}
