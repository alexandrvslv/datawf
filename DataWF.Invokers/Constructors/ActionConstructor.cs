using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DataWF.Invokers;

public class ActionConstructor<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllConstructors)]T> : RuntimeConstructor<T>
{
    private string _types;

    public ActionConstructor(params Type[] argument)
        : this(typeof(T).GetConstructor(argument) ?? throw new Exception("No such ctor"))
    { }

    public ActionConstructor(ConstructorInfo info)
        : base()
    {
        _types = string.Join(string.Empty, info.GetParameters().Select(p => p.ParameterType.FormatDisplay()));
        CreateAction = WrapConstructor(info);
        CreateParameterlessAction = WrapParameterLessConstructor(info);
    }

    public ActionConstructor(Func<object[]?, T> createAction, Func<T> createParameterlessAction)
    {
        _types = string.Empty;
        CreateAction = createAction;
        CreateParameterlessAction = createParameterlessAction;
    }

    public override string Types => _types;

    public Func<object[]?, T> CreateAction { get; private set; }
    public Func<T> CreateParameterlessAction { get; private set; }

    public override T Create() => CreateParameterlessAction();

    public override T Create(params object[]? args) => CreateAction(args);

    public static Func<object[]?, T> WrapConstructor(ConstructorInfo info)
    {
        var ps = info.GetParameters();
        if (ps.Length == 0)
        {
            return Expression.Lambda<Func<object[]?, T>>(Expression.New(info), Expression.Parameter(typeof(object[]))).Compile();
        }
        var args = new Expression[ps.Length];
        var param = Expression.Parameter(typeof(object[]));
        for (int i = 0; i < ps.Length; i++)
        {
            args[i] = Expression.Convert(Expression.ArrayIndex(param, Expression.Constant(i)), ps[i].ParameterType);
        }
        return Expression.Lambda<Func<object[]?, T>>(Expression.New(info, args), param).Compile();
    }

    public static Func<T> WrapParameterLessConstructor(ConstructorInfo info)
    {

        var ps = info.GetParameters();
        if (ps.Length == 0)
        {
            return Expression.Lambda<Func<T>>(Expression.New(info)).Compile();
        }
        var args = new Expression[ps.Length];
        for (int i = 0; i < ps.Length; i++)
        {
            args[i] = Expression.Default(ps[i].ParameterType);
        }
        return Expression.Lambda<Func<T>>(Expression.New(info, args)).Compile();
    }

}

public class ActionConstructor : IRuntimeConstructor
{
    private string _types;

    public ActionConstructor(ConstructorInfo info)
    {
        _types = string.Join(string.Empty, info.GetParameters().Select(p => p.ParameterType.FormatDisplay()));
        CreateAction = WrapConstructor(info);
        CreateParameterlessAction = WrapParameterLessConstructor(info);
    }

    public ActionConstructor(Type type, params Type[] argument)
        : this(type.GetConstructor(argument) ?? throw new Exception("No such ctor"))
    { }

    public ActionConstructor(Func<object[]?, object> createAction, Func<object> createParameterlessAction)
    {
        _types = string.Join(string.Empty, createAction.Method.GetParameters().Select(p => p.ParameterType.FormatDisplay()));
        CreateAction = createAction;
        CreateParameterlessAction = createParameterlessAction;
    }

    public string Types => _types;


    public Func<object[]?, object> CreateAction { get; private set; }
    public Func<object> CreateParameterlessAction { get; private set; }

    public object Create() => CreateAction(Array.Empty<object>());

    public object Create(params object[]? args) => CreateAction(args);

    public static Func<object[]?, object> WrapConstructor(ConstructorInfo info)
    {
        var ps = info.GetParameters();
        if (ps.Length == 0)
        {
            return Expression.Lambda<Func<object[]?, object>>(Expression.New(info), Expression.Parameter(typeof(object[]))).Compile();
        }
        var args = new Expression[ps.Length];
        var param = Expression.Parameter(typeof(object[]));
        for (int i = 0; i < ps.Length; i++)
        {
            args[i] = Expression.Convert(Expression.ArrayIndex(param, Expression.Constant(i)), ps[i].ParameterType);
        }
        return Expression.Lambda<Func<object[]?, object>>(Expression.New(info, args), param).Compile();
    }

    public static Func<object> WrapParameterLessConstructor(ConstructorInfo info)
    {

        var ps = info.GetParameters();
        if (ps.Length == 0)
        {
            return Expression.Lambda<Func<object>>(Expression.New(info)).Compile();
        }
        var args = new Expression[ps.Length];
        for (int i = 0; i < ps.Length; i++)
        {
            args[i] = Expression.Default(ps[i].ParameterType);
        }
        return Expression.Lambda<Func<object>>(Expression.New(info, args)).Compile();
    }
}
