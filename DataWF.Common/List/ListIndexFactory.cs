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
            { typeof(bool), false },
            { typeof(long), long.MaxValue },
            { typeof(ulong), ulong.MaxValue },
            { typeof(int), int.MaxValue },
            { typeof(uint), uint.MaxValue },
            { typeof(short), short.MaxValue },
            { typeof(ushort), uint.MaxValue },
            { typeof(char), char.MaxValue },
            { typeof(sbyte), sbyte.MaxValue },
            { typeof(byte), byte.MaxValue },
            { typeof(decimal), decimal.MaxValue },
            { typeof(double), double.MaxValue },
            { typeof(float), float.MaxValue },
            { typeof(DateTime), DateTime.MaxValue },
            { typeof(TimeSpan), TimeSpan.MaxValue }
        };

        public static N? GetNullableKey<N>() where N : struct
        {
            return (N?)null;
        }

        public static N GetNullKey<N>()
        {
            return (N)GetNullKey(typeof(N));
        }

        public static object GetNullKey(Type type)
        {
            if (TypeHelper.IsNullable(type))
            {
                return Activator.CreateInstance(type);
            }
            if (type.IsEnum)
            {
                return Enum.ToObject(type, int.MinValue);
            }
            if (nullKeys.TryGetValue(type, out var value))
            {
                return value;
            }
            else
            {
                return nullKeys[type] = FormatterServices.GetUninitializedObject(type);
            }
        }

        public static ListIndex<T, K> Create<T, K>(IValuedInvoker<K> invoker, bool concurrent)
        {
            var comparer = invoker.DataType == typeof(string)
                ? (IEqualityComparer<K>)StringComparer.OrdinalIgnoreCase
                : EqualityComparer<K>.Default;

            return new ListIndex<T, K>(invoker, GetNullKey<K>(), comparer, concurrent);
        }
    }
}

