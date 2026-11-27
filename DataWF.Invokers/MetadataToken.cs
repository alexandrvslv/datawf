using System;
using System.Reflection;
using System.Linq;

namespace DataWF.Invokers;

public readonly struct MetadataToken : IComparable<MetadataToken>, IEquatable<MetadataToken>
{
    public static MetadataToken GetToken(MemberInfo info)
        => GetToken(info.ReflectedType ?? info.DeclaringType ?? typeof(object), GetName(info));

    private static string GetName(MemberInfo info)
    {
        if (info is ConstructorInfo ctor)
        {
            var parameters = ctor.GetParameters();
            return string.Join(string.Empty, parameters.Select(p => p.ParameterType.FormatDisplay()));
        }
        if (info is MethodBase method)
        {
            var parameters = method.GetParameters();
            return $"{info.Name}({string.Join(string.Empty, parameters.Select(p => p.ParameterType.FormatDisplay()))})";
        }
        return info.Name;
    }

    public static MetadataToken GetToken<T>(string name)
        => GetToken(typeof(T), name);

    public static MetadataToken GetToken(Type type, string name)
        => new MetadataToken(type.Module.GetHashCode(),
            type.GetHashCode(),
            name);

    public readonly int Module;
    public readonly int Type;
    public readonly string Name;

    public MetadataToken(int module, int type, string name)
    {
        Module = module;
        Type = type;
        Name = name;
    }

    public int CompareTo(MetadataToken other)
    {
        var rezult = Module.CompareTo(other.Module);
        rezult = rezult == 0 ? Type.CompareTo(other.Type) : rezult;
        return rezult == 0 ? string.Compare(Name, other.Name, StringComparison.Ordinal) : rezult;
    }

    public override bool Equals(object? obj)
    {
        return obj is MetadataToken token && Equals(token);
    }

    public bool Equals(MetadataToken other) => Module == other.Module
        && Type == other.Type
        && string.Equals(Name, other.Name, StringComparison.Ordinal);

    public override int GetHashCode() => HashCode.Combine(Module, Type, Name);
}
