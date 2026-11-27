using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DataWF.Invokers;

public class ComplexInvoker<T, V> : ActionInvoker<T, V>
{
    public ComplexInvoker(string property, List<MemberInvokeInfo> list)
        : base(property, GetExpressionGet(property, list), GetExpressionSet(property, list))
    {

    }

    public ComplexInvoker(string property)
        : this(property, typeof(T).GetMemberInfos(property))
    {
    }

    public static GetHandler<T, V> GetExpressionGet(string name, List<MemberInvokeInfo> list)
    {
        var type = typeof(T);
        Expression? body = null;
        var target = Expression.Parameter(typeof(T).MakeByRefType(), "target");
        for (int i = 0; i < list.Count; i++)
        {
            var info = list[i];
            var param = body ?? target;
            Expression call = Call(param, info, i);
            body = CreateNullPropagationExpression(body ?? param, call, info.Info);
        }
        return Expression.Lambda<GetHandler<T, V>>(body!, target).Compile();
    }

    private static Expression Call(Expression param, MemberInvokeInfo info, int i)
    {
        if (info.Info is PropertyInfo propertyInfo)
        {
            if (info.Index != null)
            {
                var index = Expression.Constant(info.Index, info.Index.GetType());
                return Expression.Property(param, propertyInfo, index);
            }
            else
            {
                return Expression.Property(param, propertyInfo);
            }
        }
        else if (info.Info is FieldInfo fieldInfo)
        {
            return Expression.Field(param, fieldInfo);
        }
        else if (info.Info is MethodInfo methodInfo)
        {
            return Expression.Call(param, methodInfo);
        }
        throw new NotSupportedException();
    }

    //https://stackoverflow.com/a/39617419/4682355
    public static Expression CreateNullPropagationExpression(Expression o, Expression call, MemberInfo memberInfo)
    {
        Expression propertyAccess = call;

        var propertyType = memberInfo.GetMemberType();

        if (propertyType.IsValueType && !propertyType.IsNullable())
            return propertyAccess;

        var nullResult = Expression.Default(propertyAccess.Type);

        var condition = Expression.Equal(o, Expression.Constant(null, o.Type));

        return Expression.Condition(condition, nullResult, propertyAccess);
    }

    public static SetHandler<T, V>? GetExpressionSet(string name, List<MemberInvokeInfo> list)
    {
        var last = list.Last();
        var first = list.First();
        var target = Expression.Parameter(typeof(T).MakeByRefType(), "target");
        var value = Expression.Parameter(typeof(V), "value");
        var returnLabel = Expression.Label();
        var locals = new ParameterExpression[list.Count - 1];
        var labels = new LabelTarget[list.Count];
        var body = new List<Expression>();
        for (int i = 0; i < list.Count - 1; i++)
        {
            var item = list[i];
            locals[i] = Expression.Variable(item.Info.GetMemberType(), item.Info.Name.ToLower());
        }

        for (int i = 0; i < list.Count; i++)
        {
            labels[i] = Expression.Label();
        }
        int j = 0;
        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            var type = item.Info.GetMemberType();
            if (i < list.Count - 1)
            {
                var call = Call(i == 0 ? target : locals[i - 1], item, i);
                body.Add(Expression.Assign(locals[i], call));

                if (!type.IsValueType || type.IsNullable())
                {
                    body.Add(Expression.IfThenElse(Expression.Equal(locals[i], Expression.Constant(null, type)),
                        Expression.Return(returnLabel),
                        Expression.Label(labels[j++])));
                }
            }
            else
            {
                for (int r = list.Count - 1; r >= 0; r--)
                {
                    item = list[r];
                    type = item.Info.GetMemberType();
                    if (type.IsValueType || r == list.Count - 1)
                    {
                        var param = r == 0 ? target : locals[r - 1];
                        var setValue = r == list.Count - 1 ? value : locals[r];

                        if (item.Info is PropertyInfo pInfo && !pInfo.CanWrite)
                        {
                            return null;
                        }
                        else
                        {
                            var call = Call(param, item, r);
                            body.Add(Expression.Assign(call, setValue));
                        }
                    }
                }
            }
        }
        if (j > 0)
        {
            body.Add(Expression.Label(returnLabel));
        }

        return Expression.Lambda<SetHandler<T, V>>(Expression.Block(locals, body), target, value).Compile();
    }
}

