using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DataWF.Common
{
    public static class ListIndexFabric
    {
        private static readonly Dictionary<Type, object> nullKeys = new Dictionary<Type, object>
        {
            { typeof(string), "\u0000" },
            { typeof(bool), false },
            { typeof(long), long.MinValue },
            { typeof(int), int.MinValue },
            { typeof(short), short.MinValue },
            { typeof(char), char.MinValue },
            { typeof(sbyte), sbyte.MinValue },
            { typeof(ulong), ulong.MinValue },
            { typeof(uint), uint.MinValue },
            { typeof(ushort), uint.MinValue },
            { typeof(byte), byte.MinValue },
            { typeof(decimal), decimal.MinValue },
            { typeof(double), double.MinValue },
            { typeof(float), float.MinValue }
        };

        public static N? GetNullableKey<N>() where N : struct
        {
            return (N?)GetNullKey<N>();
        }

        public static N GetNullKey<N>()
        {
            return (N)GetNullKey(typeof(N));
        }

        public static object GetNullKey(Type type)
        {
            type = TypeHelper.CheckNullable(type);
            if (type.IsEnum)
            {
                return Enum.ToObject(type, int.MinValue);
            }
            if (nullKeys.TryGetValue(type, out var value))
            {
                return value;
            }

            return FormatterServices.GetUninitializedObject(type);
        }

        public static ListIndex<T, K> Create<T, K>(IInvoker<T, K> accessor, bool concurrent)
        {
            var comparer = accessor.DataType == typeof(string)
                ? (IEqualityComparer<K>)StringComparer.OrdinalIgnoreCase
                : EqualityComparer<K>.Default;

            return new ListIndex<T, K>(accessor, GetNullKey<K>(), comparer, concurrent);
        }
    }
}

