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
            return new ListIndex<T, K>(invoker, GetNullKey<K>(), concurrent);
        }

        public static IListIndex Create(IInvoker invoker, bool concurrent)
        {
            return Create(invoker.TargetType, invoker, concurrent);
        }

        public static IListIndex Create<T>(IInvoker invoker, bool concurrent)
        {
            return Create(typeof(T), invoker, concurrent);
        }

        public static IListIndex Create(Type targetType, IInvoker invoker, bool concurrent)
        {
            Type dataType = invoker.DataType;

            var invokerType = typeof(IValuedInvoker<>).MakeGenericType(dataType);
            var indexType = typeof(ListIndex<,>).MakeGenericType(targetType, dataType);

            return (IListIndex)EmitInvoker.CreateObject(indexType, new[] { invokerType, dataType, typeof(bool) }, new[] { invoker, GetNullKey(dataType), concurrent });
        }
    }
}

