using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace DataWF.Invokers;

public static class TypeExtensions
{
    private static readonly Type[] typeArrayOfSingleInt = [typeof(int)];
    private static readonly Dictionary<string, MemberInfo> casheMemberNames = new(200, StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> cacheTypeProperties = new(2, 200);

    public static Type GetItemType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties
            | DynamicallyAccessedMemberTypes.Interfaces)]
            this Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return type.GetGenericArguments()[0];
        if (type.IsSZArray)
            return type.GetElementType()!;
        foreach (var inter in type.GetInterfaces())
        {
            if (inter.IsGenericType && inter.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return inter.GetGenericArguments()[0];
            }
        }
        if (type.IsGenericType)
        {
            return type.GetGenericArguments().First();
        }
        if (type.BaseType?.IsGenericType ?? false)
        {
            return type.BaseType.GetGenericArguments().First();
        }
        if (type.IsArray)
        {
            return type.GetElementType() ?? typeof(object);
        }
        return type.GetProperty("Item", typeArrayOfSingleInt)?.PropertyType ?? typeof(object);
    }

    public static Type GetMemberType(this MemberInfo info)
    {
        return info switch
        {
            FieldInfo fieldInfo => fieldInfo.FieldType,
            PropertyInfo propertyInfo => propertyInfo.PropertyType,
            MethodInfo methodInfo => methodInfo.ReturnType,
            _ => info.ReflectedType ?? typeof(object)
        };
    }

    //https://startbigthinksmall.wordpress.com/2008/12/10/retrieving-the-base-definition-for-a-propertyinfo-net-reflection-mess/
    public static MemberInfo GetMemberBaseDefinition(this MemberInfo memberInfo)
    {
        if (memberInfo is MethodInfo methodInfo)
        {
            return methodInfo.GetBaseDefinition();
        }
        else if (memberInfo is PropertyInfo propertyInfo)
        {
            var method = propertyInfo.GetGetMethod(true) ?? propertyInfo.GetSetMethod(true);
            if (method == null)
                return propertyInfo;

            var baseMethod = method.GetBaseDefinition();

            var arguments = propertyInfo.GetIndexParameters().Select(p => p.ParameterType).ToArray();

            return baseMethod.DeclaringType?.GetProperty(propertyInfo.Name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null, propertyInfo.PropertyType, arguments, null) ?? memberInfo;
        }
        else
        {
            return memberInfo;
        }
    }

    public static PropertyInfo[] GetTypeProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties
            | DynamicallyAccessedMemberTypes.NonPublicProperties)]
            this Type type)
    {
        if (!cacheTypeProperties.TryGetValue(type, out var flist))
        {
            flist = type.GetProperties(false);
            cacheTypeProperties[type] = flist;
        }
        return flist;
    }

    public static PropertyInfo[] GetProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties
            | DynamicallyAccessedMemberTypes.NonPublicProperties)]
            this Type type, bool nonPublic)
    {
        BindingFlags flag = BindingFlags.Instance | BindingFlags.Public;
        if (nonPublic)
            flag |= BindingFlags.NonPublic;
        return type.GetProperties(flag);
    }

    public static List<MemberInvokeInfo> GetMemberInfos([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
            this Type type, string expression)
    {
        var list = new List<MemberInvokeInfo>(expression.Count('.') + 1);
        MemberInfo? last = null;
        int i, s = 0;
        do
        {
            i = expression.IndexOf('.', s);
            var memberName = expression.Substring(s, (i > 0 ? i : expression.Length) - s);
            last = GetMemberInfo(last == null ? type : GetMemberType(last), memberName, out var index, false);
            if (last == null)
            {
                break;
            }
            list.Add(new MemberInvokeInfo(last, index));
            s = i + 1;
        }
        while (i > 0);
        return list;
    }

    public static MemberInfo? GetMemberInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
            this Type type, string name)
    {
        return GetMemberInfo(type, name, out _, false);
    }

    public static MemberInfo? GetMemberInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
                                                this Type type, string name, out object? index, bool generic, params Type[] types)
    {
        index = null;
        MemberInfo? mi = null;
        string? cacheName = null;
        if (name.IndexOf('[') < 0 || generic)
        {
            cacheName = $"{type.FullName}.{name}{(generic ? "G" : "")}";
            foreach (var t in types)
                cacheName += t.Name;
            if (casheMemberNames.TryGetValue(cacheName, out mi))
                return mi;
        }
        if (type.IsInterface && string.Equals(name, nameof(ToString), StringComparison.Ordinal))
        {
            mi = typeof(object).GetMethod(name, types);
        }
        mi = mi ?? GetPropertyInfo(type, name, out index, types)
         ?? (MemberInfo?)type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
         ?? GetMethodInfo(type, name, generic, types);
        if (cacheName != null && mi != null)
        {
            casheMemberNames[cacheName] = mi;
        }
        return mi;
    }


    public static PropertyInfo? GetPropertyInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
                                                    this Type type, string name)
    {
        if (type == null || name == null)
            return null;
        string cachename = string.Format("{0}.{1}", type.FullName, name);

        if (!casheMemberNames.TryGetValue(cachename, out var minfo)
            && type.GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) is PropertyInfo getInfo)
        {
            casheMemberNames[cachename] = minfo = getInfo;
        }

        return minfo as PropertyInfo;
    }

    public static PropertyInfo? GetPropertyInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
                                                    this Type type, string name, out object? index, params Type[]? types)
    {
        index = null;
        ReadOnlySpan<char> indexName = default;
        var propertyName = name.AsSpan();
        var paramIndexBegin = propertyName.IndexOf('[');
        if (paramIndexBegin > -1)
        {
            var paramIndexEnd = propertyName.IndexOf(']');
            indexName = name.AsSpan().Slice(paramIndexBegin + 1, paramIndexEnd - (paramIndexBegin + 1));
            propertyName = propertyName.Slice(0, paramIndexBegin);

        }
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var isItem = propertyName.Equals("Item", StringComparison.Ordinal);
        foreach (var property in properties)
        {
            var parameters = property.GetIndexParameters();
            if (types != null && types.Length > 0
                    && !CompareParameters(parameters, types))
                continue;
            index = isItem && !indexName.IsEmpty && parameters.Length == 1
                ? Parse(indexName, parameters[0].ParameterType)
                : null;
            if (propertyName.Equals(property.Name, StringComparison.Ordinal)
                || index != null)
            {
                return property;
            }
        }
        return null;
    }

    public static MethodInfo? GetMethodInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
                                                this Type type, string name, bool generic, Type[] types)
    {
        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (string.Equals(method.Name, name, StringComparison.Ordinal) && method.IsGenericMethod == generic)
            {
                if (CompareParameters(method.GetParameters(), types))
                    return method;
            }
        }

        return null;
    }

    private static bool CompareParameters(ParameterInfo[] parameters, Type[] types)
    {
        if (parameters.Length >= types.Length)
        {
            var flag = true;
            for (int j = 0; j < types.Length; j++)
            {
                if (parameters[j].ParameterType != types[j])
                {
                    flag = false;
                    break;
                }
            }
            return flag;
        }
        return false;
    }

    public static bool IsNullable(this Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    public static object? Parse(ReadOnlySpan<char> text, Type toType)
    {
        if (toType == typeof(int))
            return int.TryParse(text, CultureInfo.InvariantCulture, out var intV) ? intV : 0;
        else if (toType == typeof(string))
            return text.ToString();
        else if (toType == typeof(float))
            return float.TryParse(text, CultureInfo.InvariantCulture, out var floatV) ? floatV : 0F;
        return null;
    }

    public static string FormatDisplay(this Type type)
    {
        if (type.IsPointer)
            return type.GetElementType()!.FormatDisplay() + "Pointer";
        if (type.IsArray)
            return type.GetElementType()!.FormatDisplay() + "Array";
        if (type.IsGenericType)
        {
            int tickIndex = type.Name.IndexOf('`');

            string genericName = tickIndex > 0 ? type.Name.Substring(0, tickIndex) : type.Name;

            var typeArguments = type.IsGenericTypeDefinition
                ? type.GetGenericArguments()
                : type.GenericTypeArguments;

            string arguments = string.Join(string.Empty, typeArguments.Select(x => x.FormatDisplay()));

            return genericName + arguments;
        }
        return type.Name;
    }

    public static string FormatShort(this Type type)
    {
        if (type.IsPointer)
            return type.GetElementType()!.FormatShort() + "Pointer";
        if (type.IsArray)
            return type.GetElementType()!.FormatShort() + "Array";
        if (type.IsGenericType)
        {
            int tickIndex = type.Name.IndexOf('`');

            string genericName = tickIndex > 0 ? type.Name.Substring(0, tickIndex) : type.Name;
            if (genericName == "Nullable")
            {
                var typeArguments = type.IsGenericTypeDefinition
                    ? type.GetGenericArguments()
                    : type.GenericTypeArguments;
                return "N" + typeArguments.First().FormatShort();
            }
            return genericName;
        }
        return type.Name;
    }

}
