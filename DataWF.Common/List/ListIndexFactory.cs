using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DataWF.Common
{
    public static class ListIndexFactory
    {
        private static readonly Dictionary<Type, object> nullKeys = new Dictionary<Type, object>
        {
            { typeof(string), "\u0000" },
            { typeof(char), '\u0000' },
            { typeof(byte[]), new byte[] { 0 } },
            { typeof(char[]), new char[] { '\u0000' } },
            { typeof(bool), false },
            { typeof(long), long.MinValue },
            { typeof(ulong), ulong.MaxValue },
            { typeof(int), int.MinValue },
            { typeof(uint), uint.MaxValue },
            { typeof(short), short.MinValue },
            { typeof(ushort), ushort.MaxValue },
            { typeof(sbyte), sbyte.MinValue },
            { typeof(byte), byte.MaxValue },
            { typeof(decimal), decimal.MinValue },
            { typeof(double), double.MinValue },
            { typeof(float), float.MinValue },
            { typeof(DateTime), DateTime.MaxValue },
            { typeof(TimeSpan), TimeSpan.MaxValue },
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
                    value = FormatterServices.GetUninitializedObject(nullable);
                }

                if (nullable != type)
                {
                    value = Activator.CreateInstance(type, value);
                }
                nullKeys[type] = value;
            }
            return value;

        }

        public static ListIndex<T, K> Create<T, K>(IValuedInvoker<K> invoker, bool concurrent)
        {
            var comparer = (IEqualityComparer<K>)EqualityComparer<K>.Default;
            if (invoker.DataType == typeof(string))
                comparer = (IEqualityComparer<K>)StringComparer.OrdinalIgnoreCase;
            if (invoker.DataType == typeof(byte[]))
                comparer = (IEqualityComparer<K>)ByteArrayComparer.Default;
            return new ListIndex<T, K>(invoker, GetNullKey<K>(), comparer, concurrent);
        }
    }
}

