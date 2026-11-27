using System;
using System.Collections.Generic;

namespace DataWF.Serialization.Elements;

public static class ArraySerializers
{
    private static readonly Dictionary<Type, Func<IElementSerializer>> map = new()
    {
        {typeof(bool), static () => SerializeProvider.Register(BoolArraySerializer.Instance)},
        {typeof(char), static () => SerializeProvider.Register(CharArraySerializer.Instance)},
        
        {typeof(sbyte), static () => SerializeProvider.Register(SByteArraySerializer.Instance)},
        {typeof(short), static () => SerializeProvider.Register(Int16ArraySerializer.Instance)},
        {typeof(int), static () => SerializeProvider.Register(Int32ArraySerializer.Instance)},
        {typeof(long), static () => SerializeProvider.Register(Int64ArraySerializer.Instance)},
        
        {typeof(byte), static () => SerializeProvider.Register(ByteArraySerializer.Instance)},
        {typeof(ushort), static () => SerializeProvider.Register(UInt16ArraySerializer.Instance)},
        {typeof(uint), static () => SerializeProvider.Register(UInt32ArraySerializer.Instance)},
        {typeof(ulong), static () => SerializeProvider.Register(UInt64ArraySerializer.Instance)},
        
        {typeof(float), static () => SerializeProvider.Register(SingleArraySerializer.Instance)},
        {typeof(double), static () => SerializeProvider.Register(DoubleArraySerializer.Instance)},
        
        {typeof(string), static () => SerializeProvider.Register(StringArraySerializer.Instance)},

    };
    
    public static bool ContainsKey(Type type) => map.ContainsKey(type);
    
    public static IElementSerializer<T[]?> Register<T>()
    {
        if (map.TryGetValue(typeof(T), out var func))
            return (IElementSerializer<T[]?>)func();
        return SerializeProvider.Register(new ArraySerializer<T>());
    }

    public static IElementSerializer<T[]?> RegisterUnmanaged<T>() where T : unmanaged
    {
        if (map.TryGetValue(typeof(T), out var func))
            return (IElementSerializer<T[]?>)func();
        return SerializeProvider.Register(new UnmanagedArraySerializer<T>());
    }
}