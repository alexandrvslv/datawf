using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DataWF.Serialization;

namespace DataWF.Collections.Indexes;

public static class IndexFactory
{
    private static readonly Dictionary<Type, object> nullKeys = new()
        {
            { typeof(string), "\u0000" },
            { typeof(char), '\u0000' },
            { typeof(byte[]), new byte[] { 0 } },
            { typeof(char[]), new char[] { '\u0000' } },
            { typeof(bool), default(bool) },
            { typeof(bool?), default(bool?)! },
            { typeof(long), default(long) },
            { typeof(long?), default(long?)! },
            { typeof(ulong), default(ulong) },
            { typeof(ulong?), default(ulong?)! },
            { typeof(int), default(int) },
            { typeof(int?), default(int?)! },
            { typeof(uint), default(uint) },
            { typeof(uint?) , default(uint?)! },
            { typeof(short), default(short) },
            { typeof(short?), default(short?)! },
            { typeof(ushort), default(ushort) },
            { typeof(ushort?), default(ushort?)! },
            { typeof(sbyte), default(sbyte) },
            { typeof(sbyte?), default(sbyte?)! },
            { typeof(byte), default(byte) },
            { typeof(byte?), default(byte?)! },
            { typeof(decimal), default(decimal) },
            { typeof(decimal?), default(decimal?)! },
            { typeof(double), default(double) },
            { typeof(double?), default(double?)! },
            { typeof(float), default(float) },
            { typeof(float?), default(float?)! },
            { typeof(Guid), default(Guid) },
            { typeof(Guid?), default(Guid?)! },
            { typeof(DateTime), default(DateTime) },
            { typeof(DateTime?), default(DateTime?)! },
            { typeof(DateTimeOffset), default(DateTimeOffset) },
            { typeof(DateTimeOffset?), default(DateTimeOffset?)! },
            { typeof(DateOnly), default(DateOnly) },
            { typeof(DateOnly?), default(DateOnly?)! },
            { typeof(TimeOnly), default(TimeOnly) },
            { typeof(TimeOnly?), default(TimeOnly?)! },
            { typeof(TimeSpan), default(TimeSpan) },
            { typeof(TimeSpan?), default(TimeSpan?)! },
        };

    public static N GetNullKey<N>()
    {
        return (N)GetNullKey(typeof(N));
    }

    public static object GetNullKey(Type type)
    {
        if (!nullKeys.TryGetValue(type, out var value))
        {
            var nullable = TypeHelper.CheckNullable(type);
            if (nullable != type
                && nullKeys.TryGetValue(nullable, out value))
            {
                return nullKeys[type] = Activator.CreateInstance(type, value);
            }
            if (nullable.IsEnum)
            {
                var undelineType = Enum.GetUnderlyingType(nullable);
                value = Enum.ToObject(nullable, nullKeys[undelineType]);
            }
            else
            {
                value = RuntimeHelpers.GetUninitializedObject(nullable);
            }

            if (nullable != type)
            {
                value = Activator.CreateInstance(type, value);
            }
            nullKeys[type] = value;
        }
        return value;

    }

}

