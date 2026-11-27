using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace DataWF.Serialization.Elements;

public static class SystemSerializers
{
    private static readonly Dictionary<Type, Func<IElementSerializer>> map = new()
    {
        {typeof(bool), static () => SerializeProvider.Register(BoolSerializer.Instance)},
        {typeof(bool?), static () => SerializeProvider.Register(BoolNSerializer.Instance)},
        {typeof(char), static () => SerializeProvider.Register(CharSerializer.Instance)},
        {typeof(char?), static () => SerializeProvider.Register(CharNSerializer.Instance)},
        
        {typeof(sbyte), static () => SerializeProvider.Register(Int8Serializer.Instance)},
        {typeof(sbyte?), static () => SerializeProvider.Register(Int8NSerializer.Instance)},
        {typeof(Int16), static () => SerializeProvider.Register(Int16Serializer.Instance)},
        {typeof(Int16?), static () => SerializeProvider.Register(Int16NSerializer.Instance)},
        {typeof(Int32), static () => SerializeProvider.Register(Int32Serializer.Instance)},
        {typeof(Int32?), static () => SerializeProvider.Register(Int32NSerializer.Instance)},
        {typeof(Int64), static () => SerializeProvider.Register(Int64Serializer.Instance)},
        {typeof(Int64?), static () => SerializeProvider.Register(Int64NSerializer.Instance)},
        
        {typeof(byte), static () => SerializeProvider.Register(UInt8Serializer.Instance)},
        {typeof(byte?), static () => SerializeProvider.Register(UInt8NSerializer.Instance)},
        {typeof(UInt16), static () => SerializeProvider.Register(UInt16Serializer.Instance)},
        {typeof(UInt16?), static () => SerializeProvider.Register(UInt16NSerializer.Instance)},
        {typeof(UInt32), static () => SerializeProvider.Register(UInt32Serializer.Instance)},
        {typeof(UInt32?), static () => SerializeProvider.Register(UInt32NSerializer.Instance)},
        {typeof(UInt64), static () => SerializeProvider.Register(UInt64Serializer.Instance)},
        {typeof(UInt64?), static () => SerializeProvider.Register(UInt64NSerializer.Instance)},
        
        {typeof(Half), static () => SerializeProvider.Register(HalfSerializer.Instance)},
        {typeof(Half?), static () => SerializeProvider.Register(HalfNSerializer.Instance)},
        {typeof(Single), static () => SerializeProvider.Register(SingleSerializer.Instance)},
        {typeof(Single?), static () => SerializeProvider.Register(SingleNSerializer.Instance)},
        {typeof(Double), static () => SerializeProvider.Register(DoubleSerializer.Instance)},
        {typeof(Double?), static () => SerializeProvider.Register(DoubleNSerializer.Instance)},
        {typeof(Decimal), static () => SerializeProvider.Register(DecimalSerializer.Instance)},
        {typeof(Decimal?), static () => SerializeProvider.Register(DecimalNSerializer.Instance)},
        
        {typeof(Guid), static () => SerializeProvider.Register(GuidSerializer.Instance)},
        {typeof(Guid?), static () => SerializeProvider.Register(GuidNSerializer.Instance)},
        
        {typeof(DateOnly), static () => SerializeProvider.Register(DateOnlySerializer.Instance)},
        {typeof(DateOnly?), static () => SerializeProvider.Register(DateOnlyNSerializer.Instance)},
        
        {typeof(DateTime), static () => SerializeProvider.Register(DateTimeSerializer.Instance)},
        {typeof(DateTime?), static () => SerializeProvider.Register(DateTimeNSerializer.Instance)},
        
        {typeof(DateTimeOffset), static () => SerializeProvider.Register(DateTimeOffsetSerializer.Instance)},
        {typeof(DateTimeOffset?), static () => SerializeProvider.Register(DateTimeOffsetNSerializer.Instance)},
        
        {typeof(TimeOnly), static () => SerializeProvider.Register(TimeOnlySerializer.Instance)},
        {typeof(TimeOnly?), static () => SerializeProvider.Register(TimeOnlyNSerializer.Instance)},
        
        {typeof(TimeSpan), static () => SerializeProvider.Register(TimeSpanSerializer.Instance)},
        {typeof(TimeSpan?), static () => SerializeProvider.Register(TimeSpanNSerializer.Instance)},
        
        {typeof(CultureInfo), static () => SerializeProvider.Register(CultureInfoSerializer.Instance)},
        {typeof(Version), static () => SerializeProvider.Register(VersionSerializer.Instance)},
        {typeof(Type), static () => SerializeProvider.Register(TypeSerializer.Instance)},
        {typeof(Uri), static () => SerializeProvider.Register(UriSerializer.Instance)},
        {typeof(string), static () => SerializeProvider.Register(StringSerializer.Instance)},
        {typeof(ArrayList), static () => SerializeProvider.Register(ArrayListSerializer.Instance)},

    };
    
    public static bool ContainsKey(Type type) => map.ContainsKey(type);
    
    public static IElementSerializer<T> Register<T>()
    {
        if (map.TryGetValue(typeof(T), out var func))
            return (IElementSerializer<T>)func();
        throw new NotImplementedException(typeof(T).ToString());
    }
}
