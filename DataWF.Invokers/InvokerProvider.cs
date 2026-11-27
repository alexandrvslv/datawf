using DataWF.Invokers.Defaults;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DataWF.Invokers;

public static class InvokeProvider
{
    private static readonly ConcurrentDictionary<MetadataToken, IInvoker> cacheInvokers = new(-1, 100);
    private static readonly ConcurrentDictionary<Type, List<IInvoker>> cacheTypeInvokers = new(-1, 100);
    private static readonly ConcurrentDictionary<MetadataToken, IRuntimeConstructor> cacheCtors = new(-1, 100);
    private static readonly ConcurrentDictionary<MetadataToken, Type> genericInvokers = new(-1, 100);

    static InvokeProvider()
    {
        ToStringInvoker<object>.RegisterInvokers();
    }

    public static void DeleteCache()
    {
        cacheInvokers.Clear();
        cacheTypeInvokers.Clear();
        cacheCtors.Clear();
        genericInvokers.Clear();
    }

    public static void DeleteCache(Type type)
    {
        DeleteCache(type, Type.EmptyTypes);

        var props = type.GetProperties(false);
        foreach (var info in props)
            DeleteCache(info);

        var methods = type.GetMethods();
        foreach (var info in methods)
            DeleteCache(info);
    }

    public static void DeleteCache(Type type, params Type[] param)
    {
        var info = type.GetConstructor(param);
        if (info != null)
            DeleteCache(info);
    }

    public static void DeleteCache(MemberInfo info)
    {
        var token = MetadataToken.GetToken(info);
        cacheInvokers.TryRemove(token, out _);
    }

    public static void RegisterGenericInvokers(Type targetType, string name, Type invokerType)
    {
        genericInvokers[MetadataToken.GetToken(targetType, name)] = invokerType;
    }

    public static void RegisterGenericInvokers<T>(string name, Type invokerType)
    {
        genericInvokers[MetadataToken.GetToken<T>(name)] = invokerType;
    }

    public static void RegisterDictionary<T, TKey, TValue>() //where T: IDictionary<TKey, TValue>
        where TKey : notnull
    {
        if (DictionaryMap<TKey, TValue>.Map.TryGetValue(typeof(T), out var action))
            action.Invoke();
        else if (typeof(T).IsAssignableTo(typeof(IInvokerRegister)))
            typeof(T).GetMethod("RegisterInvokers")?.Invoke(null, null);
    }

    public static void RegisterCollection<T, TEntry>()
        where T: IEnumerable<TEntry>
    {
        if (CollectionMap<TEntry>.Map.TryGetValue(typeof(T), out var action))
            action.Invoke();
        else if (typeof(T).IsAssignableTo(typeof(IInvokerRegister)))
            typeof(T).GetMethod("RegisterInvokers")?.Invoke(null, null);
    }

    public static bool RegisterInvoker<T>(IInvoker invoker)
    {
        var token = MetadataToken.GetToken<T>(invoker.Name);
        return RegisterInvoker<T>(token, invoker);
    }

    private static bool RegisterInvoker<T>(MetadataToken token, IInvoker invoker)
    {
        if (cacheInvokers.ContainsKey(token))
            return false;
        cacheInvokers[token] = invoker;
        var invokers = cacheTypeInvokers.GetOrAdd(typeof(T), _ => new List<IInvoker>());
        invokers.Add(invoker);
        return true;
    }

    public static void RegisterConstructor<T>(RuntimeConstructor<T> runtimeConstructor)
    {
        var token = MetadataToken.GetToken<T>(runtimeConstructor.Types);
        cacheCtors[token] = runtimeConstructor;
    }

    public static IRuntimeConstructor<T>? GetConstructor<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        T>(Type[] param)
    {
        var info = typeof(T).GetConstructor(param);
        if (info == null)
            return null;
        return GetConstructor<T>(info);
    }

    public static IRuntimeConstructor<T> GetConstructor<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        T>(ConstructorInfo info)
    {
        var token = MetadataToken.GetToken(info);
        if (cacheCtors.TryGetValue(token, out var invoker))
        {
            if (invoker.Types != token.Name)
            { throw new Exception($"Token {token.GetHashCode()} collision cache: {invoker.Types} - {info}"); }
            return (IRuntimeConstructor<T>)invoker;
        }
        var constructor = new ActionConstructor<T>(info);
        cacheCtors[token] = constructor;
        return constructor;
    }

    public static IRuntimeConstructor? GetConstructor(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        Type type, Type[] param)
    {
        var info = type.GetConstructor(param);
        if (info == null)
            return null;
        return GetConstructor(info);
    }

    public static IRuntimeConstructor GetConstructor(ConstructorInfo info)
    {
        var token = MetadataToken.GetToken(info);
        if (cacheCtors.TryGetValue(token, out var invoker))
        {
            if (invoker.Types != token.Name)
            { throw new Exception($"Token {token.GetHashCode()} collision cache: {invoker.Types} - {info}"); }
            return invoker;
        }
        var constructor = new ActionConstructor(info);
        cacheCtors[token] = constructor;
        return constructor;
    }

    public static IInvoker<T, V> GetRegInvoker<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T, V>
        (string expression)
        where T : IInvokerRegister
    {
        var token = MetadataToken.GetToken<T>(expression);
        if (cacheInvokers.TryGetValue(token, out var invoker))
        {
            return (IInvoker<T, V>)invoker;
        }
        T.RegisterInvokers();
        return GetInvoker<T, V>(expression); // or appropriate IInvoker<T, V> instance if available
    }

    public static IInvoker<T, V> GetInvoker<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T, V>
        (string expression)
    {
        if (expression == null)
            throw new ArgumentNullException(nameof(expression));

        var token = MetadataToken.GetToken<T>(expression);
        if (cacheInvokers.TryGetValue(token, out var invoker))
        {
            return (IInvoker<T, V>)invoker;
        }

        if (expression == nameof(ToString))
        {
            ToStringInvoker<T>.RegisterInvokers();
            return (IInvoker<T, V>)ToStringInvoker<T>.Instance;
        }
        if (expression == "this")
        {
            SelfInvoker<T>.RegisterInvokers();
            return (IInvoker<T, V>)SelfInvoker<T>.Instance;
        }

        var list = typeof(T).GetMemberInfos(expression);
        if (list.Count == 0)
        {
            throw new ArgumentException($"Expression {expression} not related to type {typeof(T).Name}");
        }
        if (list.Count == 1)
        {
            return GetInvoker<T, V>(list[0].Info, list[0].Index);
        }
        return new ComplexInvoker<T, V>(expression, list);
    }

    public static IInvoker GetInvoker<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
        (string expression)
    {
        if (expression == null)
            throw new ArgumentNullException(nameof(expression));

        var token = MetadataToken.GetToken<T>(expression);
        if (cacheInvokers.TryGetValue(token, out var invoker))
        {
            return invoker;
        }

        if (expression == nameof(ToString))
        {
            ToStringInvoker<T>.RegisterInvokers();
            return ToStringInvoker<T>.Instance;
        }
        if (expression == "this")
        {
            SelfInvoker<T>.RegisterInvokers();
            return SelfInvoker<T>.Instance;
        }

        var list = typeof(T).GetMemberInfos(expression);
        if (list.Count == 0)
        {
            throw new ArgumentException($"Expression {expression} not related to type {typeof(T).Name}");
        }
        if (list.Count == 1)
        {
            return GetInvoker(list[0].Info, list[0].Index);
        }
        return null!;
    }

    public static IInvoker<T, V> GetInvoker<T, V>(MemberInfo info, object? index = null)
    {
        var token = MetadataToken.GetToken(info);
        if (cacheInvokers.TryGetValue(token, out var invoker))
        {
            return (IInvoker<T, V>)invoker;
        }
        invoker = NewInvoker<T, V, int>(info, index == null ? default : (int)index);
        RegisterInvoker<T>(token, invoker);
        return (IInvoker<T, V>)invoker;
    }

    public static IInvoker GetInvoker(MemberInfo info, object? index = null)
    {
        bool cache = index == null;
        var token = MetadataToken.GetToken(info);
        if (cache)
        {
            if (cacheInvokers.TryGetValue(token, out var invoker))
            {
                return invoker;
            }
            Type? invokerType;
            if (!genericInvokers.TryGetValue(token, out invokerType))
            {
                var baseInfo = info.GetMemberBaseDefinition();
                var baseToken = MetadataToken.GetToken(baseInfo);
                if (cacheInvokers.TryGetValue(baseToken, out invoker))
                {
                    return invoker;
                }
                if (!genericInvokers.TryGetValue(baseToken, out invokerType))
                {
                    invokerType = GetNestedInvokerType(baseInfo);

                    if (baseInfo.DeclaringType?.IsGenericType is true && !baseInfo.DeclaringType.IsGenericTypeDefinition)
                    {
                        var genericType = baseInfo.DeclaringType.GetGenericTypeDefinition();
                        var genericInfo = genericType.GetMemberInfo(baseInfo.Name, out index, false);
                        invokerType = genericInfo == null ? null : GetNestedInvokerType(genericInfo);
                        if (invokerType != null)
                        {
                            invokerType = invokerType.MakeGenericType(info.DeclaringType!.GetGenericArguments());
                        }
                    }
                }
            }
            if (invokerType != null)
            {
                if (invokerType.IsGenericTypeDefinition)
                {
                    invokerType = invokerType.MakeGenericType(info.DeclaringType!);
                }
                return (IInvoker)Activator.CreateInstance(invokerType)!;
            }
        }
        return NewInvoker(info, (int?)index);
    }

    private static Type? GetNestedInvokerType(MemberInfo info)
    {
        //var infoToken = MetadataToken.GetToken(info);
        if (info.DeclaringType is Type declType)
        {
            foreach (var nestedType in declType.GetNestedTypes())
            {
                if (nestedType.Name.Equals($"{info.Name}Invoker", StringComparison.OrdinalIgnoreCase))
                {
                    return nestedType;
                }
            }
        }
        return null;
    }

    public static IInvoker<T, V> NewInvoker<T, V, I>(MemberInfo info, I? index = default)
    {
        var dType = info.DeclaringType!;
        System.Diagnostics.Debug.WriteLine($"Start Create Invoker Declared:{dType} Reference: {typeof(T)} - {info.Name}");
        var token = MetadataToken.GetToken(info);
        IInvoker<T, V> result;
        switch (info)
        {
            case FieldInfo fInfo:
                result = new FieldInvoker<T, V>(fInfo);
                break;
            case PropertyInfo pInfo:
                var parameters = pInfo.GetIndexParameters();
                if (parameters.Length == 0)
                {
                    result = new PropertyInvoker<T, V>(pInfo);
                }
                else
                {
                    if (index == null)
                        throw new ArgumentNullException(nameof(index), $"Index parameter is required for property {pInfo.Name}.");
                    result = new IndexPropertyInvoker<T, V, I>(pInfo, (I)index);
                }
                break;
            case MethodInfo methodInfo:
                if (string.Equals(info.Name, nameof(ToString), StringComparison.Ordinal))
                {
                    result = (IInvoker<T, V>)new ToStringInvoker<T>();
                }
                else
                {
                    result = new MethodInvoker<T, V>(methodInfo);
                }
                break;
            default:
                throw new NotSupportedException($"MemberInfo type {info.GetType().Name} is not supported for now.");
        }
        return result;
    }

    public static IInvoker NewInvoker(MemberInfo info, object? index = default)
    {
        var dType = info.DeclaringType!;
        System.Diagnostics.Debug.WriteLine($"Invoker {dType} - {info.Name}");
        var token = MetadataToken.GetToken(info);
        IInvoker result;
        switch (info)
        {
            case FieldInfo fInfo:
                result = (IInvoker)Activator.CreateInstance(typeof(FieldInvoker<,>).MakeGenericType(dType, fInfo.FieldType), fInfo)!;
                break;
            case PropertyInfo pInfo:
                var parameters = pInfo.GetIndexParameters();
                if (parameters.Length == 0)
                {
                    result = (IInvoker)Activator.CreateInstance(typeof(PropertyInvoker<,>).MakeGenericType(dType, pInfo.PropertyType), pInfo)!;
                }
                else
                {
                    if (index == null)
                        throw new ArgumentNullException(nameof(index), $"Index parameter is required for property {pInfo.Name}.");
                    result = (IInvoker)Activator.CreateInstance(typeof(IndexPropertyInvoker<,,>).MakeGenericType(dType, pInfo.PropertyType, index.GetType()), pInfo, index)!;
                }
                break;
            case MethodInfo methodInfo:
                if (string.Equals(info.Name, nameof(ToString), StringComparison.Ordinal))
                {
                    result = (IInvoker)Activator.CreateInstance(typeof(ToStringInvoker<>).MakeGenericType(dType))!;
                }
                else
                {
                    result = (IInvoker)Activator.CreateInstance(typeof(MethodInvoker<,>).MakeGenericType(dType, methodInfo.ReturnType), methodInfo)!;
                }
                break;
            default:
                throw new NotSupportedException($"MemberInfo type {info.GetType().Name} is not supported for now.");
        }
        return result;
    }

    //private static void Initialize(MethodInfo getInfo, MethodInfo setInfo, object index = null)
    //{
    //	mInfo = getInfo;
    //	type = getInfo.ReturnType;
    //	mIndex = index;
    //	if (getInfo != null)
    //	{
    //		if (!cFieldGet.TryGetValue(GetToken(getInfo), out getHandler))
    //			cFieldGet[GetToken(getInfo)] = getHandler = GetInvokerGet(getInfo);
    //	}
    //	if (setInfo != null)
    //	{
    //		write = true;
    //		if (!cFieldSet.TryGetValue(GetToken(setInfo), out setHandler))
    //			cFieldSet[GetToken(setInfo)] = setHandler = GetInvokerSet(setInfo);
    //	}
    //}

    //public static int CompareKey(string member, object x, object key, IComparer comparer)
    //{
    //    return CompareKey(Initialize(x.GetType(), member), x, key, comparer);
    //}

    //public static int CompareKey(IInvoker accesor, object x, object key, IComparer comparer)
    //{
    //    return ListHelper.Compare(accesor.GetValue(x), key, comparer);
    //}

    public static T? CreateObject<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        T>()
    {
        return (T?)CreateObject(typeof(T));
    }

    public static object? CreateObject(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        Type type)
    {
        return CreateObject(type, Type.EmptyTypes, null);
    }

    public static object? CreateObject(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        Type type, Type[] ctypes, object[]? cparams)
    {
        var ctorInvoker = GetConstructor(type, ctypes);
        return ctorInvoker?.Create(cparams);
    }

    public static object? Invoke(Type type, string name, object item, params object[] parameters)
    {
        var types = new Type[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            types[i] = parameters[i]?.GetType() ?? typeof(object);
        }

        return Invoke(type.GetMemberInfo(name, out _, false, types) as MethodInfo, item, parameters);
    }

    public static object? Invoke(MethodInfo? info, object item, params object[] pars)
    {
        if (info == null)
            return null;
        return ((IIndexInvoker)GetInvoker(info))?.GetValue(item, pars);
    }

    public static object? GetValue(Type type, string name, object? item)
    {
        return GetValue(type.GetMemberInfo(name, out _, false), item);
    }

    public static object? GetValue(MemberInfo? info, object? item)
    {
        if (info == null)
            return null;
        return GetInvoker(info)?.GetValue(item);
    }

    public static object? GetValue(MemberInfo? info, object item, object index)
    {
        if (info == null)
            return null;
        return ((IIndexInvoker)GetInvoker(info))?.GetValue(item, index);
    }

    public static void SetValue(Type type, string name, object item, object value)
    {
        SetValue(type.GetMemberInfo(name, out _, false), item, value);
    }

    public static void SetValue(MemberInfo? info, object item, object? value)
    {
        if (info == null)
            return;
        var invoker = GetInvoker(info);
        if (invoker.CanWrite)
            invoker.SetValue(item, value);
    }

    public static void SetValue(MemberInfo? info, object item, object? value, object index)
    {
        if (info == null)
            return;
        ((IIndexInvoker)GetInvoker(info))?.SetValue(item, index, value);
    }
}






