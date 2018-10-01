using System;
using System.Collections.Generic;

namespace DataWF.Common
{
    public static class ListIndexFabric
    {
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
            if (type == typeof(string))
            {
                return "magic ZeRo String Va!@#;)(*&ue";//, (IEqualityComparer<K>)StringComparer.OrdinalIgnoreCase);
            }

            if (type.IsEnum)
            {
                return int.MinValue;
            }
            if (type == typeof(bool))
            {
                return false;
            }
            if (type == typeof(long))
            {
                return long.MinValue;
            }
            else if (type == typeof(int))
            {
                return int.MinValue;
            }
            else if (type == typeof(short))
            {
                return short.MinValue;
            }
            else if (type == typeof(char))
            {
                return char.MinValue;
            }
            else if (type == typeof(sbyte))
            {
                return sbyte.MinValue;
            }
            else if (type == typeof(ulong))
            {
                return ulong.MinValue;
            }
            else if (type == typeof(uint))
            {
                return uint.MinValue;
            }
            else if (type == typeof(ushort))
            {
                return uint.MinValue;
            }
            else if (type == typeof(byte))
            {
                return byte.MinValue;
            }
            else if (type == typeof(decimal))
            {
                return decimal.MinValue;
            }
            else if (type == typeof(double))
            {
                return double.MinValue;
            }
            else if (type == typeof(float))
            {
                return float.MinValue;
            }
            return EmitInvoker.CreateObject(type);
        }

        public static ListIndex<T, K> Create<T, K>(IInvoker<T, K> accessor)
        {

            IEqualityComparer<K> comparer = null;
            if (accessor.DataType == typeof(string))
            {
                comparer = (IEqualityComparer<K>)StringComparer.OrdinalIgnoreCase;
            }
            return new ListIndex<T, K>(accessor, GetNullKey<K>(), comparer);
        }
    }
}

