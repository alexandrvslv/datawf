using System;
using System.Collections.Generic;

namespace DataWF.Serialization.Elements;

public static class EnumSerializers<T>
    where T: unmanaged, Enum
{
    private static readonly Dictionary<Type, Func<IElementSerializer>> map = new()
    {
        { typeof(sbyte), static () => SerializeProvider.Register(EnumInt8Serializer<T>.Instance) },
        { typeof(byte), static () => SerializeProvider.Register(EnumUInt8Serializer<T>.Instance) },
        { typeof(Int16), static () => SerializeProvider.Register(EnumInt16Serializer<T>.Instance) },
        { typeof(UInt16), static () => SerializeProvider.Register(EnumUInt16Serializer<T>.Instance) },
        { typeof(Int32), static () => SerializeProvider.Register(EnumInt32Serializer<T>.Instance) },
        { typeof(UInt32), static () => SerializeProvider.Register(EnumUInt32Serializer<T>.Instance) },
        { typeof(Int64), static () => SerializeProvider.Register(EnumInt64Serializer<T>.Instance) },
        { typeof(UInt64), static () => SerializeProvider.Register(EnumUInt64Serializer<T>.Instance) },
    };
    private static readonly Dictionary<Type, Func<IElementSerializer>> mapN = new()
    {
        {typeof(sbyte), static () => SerializeProvider.Register(EnumInt8NSerializer<T>.Instance)},
        {typeof(byte), static () => SerializeProvider.Register(EnumUInt8NSerializer<T>.Instance)},
        {typeof(Int16), static () => SerializeProvider.Register(EnumInt16NSerializer<T>.Instance)},
        {typeof(UInt16), static () => SerializeProvider.Register(EnumUInt16NSerializer<T>.Instance)},
        {typeof(Int32), static () => SerializeProvider.Register(EnumInt32NSerializer<T>.Instance)},
        {typeof(UInt32), static () => SerializeProvider.Register(EnumUInt32NSerializer<T>.Instance)},
        {typeof(Int64), static () => SerializeProvider.Register(EnumInt64NSerializer<T>.Instance)},
        {typeof(UInt64), static () => SerializeProvider.Register(EnumUInt64NSerializer<T>.Instance)},
    };
    
    public static IElementSerializer<T> Register(Type underlineType)
    {
        if (map.TryGetValue(underlineType, out var func))
            return (IElementSerializer<T>)func();
        throw new NotImplementedException(underlineType.ToString());
    }
    
    public static IElementSerializer<T?> RegisterDefaultNullable(Type underlineType)
    {
        if (mapN.TryGetValue(underlineType, out var func))
            return (IElementSerializer<T?>)func();
        throw new NotImplementedException(underlineType.ToString());
    }
}