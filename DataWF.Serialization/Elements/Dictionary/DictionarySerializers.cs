using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using DataWF.Invokers;

namespace DataWF.Serialization.Elements;

public static class DictionarySerializers<TKey, TValue>
    where TKey : notnull
{
    private static readonly Dictionary<Type, Func<IElementSerializer>> map = new()
    {
        { typeof(Dictionary<TKey, TValue>), static () => typeof(TKey) == typeof(string)
            ? SerializeProvider.Register(DictionaryStringKeySerializer<Dictionary<string, TValue>, TValue>.Instance)
            : SerializeProvider.Register(DictionarySerializer<Dictionary<TKey, TValue>, TKey, TValue>.Instance) },
        { typeof(SortedDictionary<TKey, TValue>), static () => typeof(TKey) == typeof(string)
            ? SerializeProvider.Register(DictionaryStringKeySerializer<SortedDictionary<string, TValue>, TValue>.Instance)
            : SerializeProvider.Register(DictionarySerializer<SortedDictionary<TKey, TValue>, TKey, TValue>.Instance) },
        { typeof(SortedList<TKey, TValue>), static () => typeof(TKey) == typeof(string)
            ? SerializeProvider.Register(DictionaryStringKeySerializer<SortedList<string, TValue>, TValue>.Instance)
            : SerializeProvider.Register(DictionarySerializer<SortedList<TKey, TValue>, TKey, TValue>.Instance) },
        { typeof(ConcurrentDictionary<TKey, TValue>), static () => typeof(TKey) == typeof(string)
            ? SerializeProvider.Register(DictionaryStringKeySerializer<ConcurrentDictionary<string, TValue>, TValue>.Instance)
            : SerializeProvider.Register(DictionarySerializer<ConcurrentDictionary<TKey, TValue>, TKey, TValue>.Instance) },
        { typeof(ImmutableDictionary<TKey, TValue>), static () => typeof(TKey) == typeof(string)
            ? SerializeProvider.Register(ImmutableDictionaryStringKeySerializer<TValue>.Instance)
            : SerializeProvider.Register(ImmutableDictionarySerializer<TKey, TValue>.Instance) },
        { typeof(ImmutableSortedDictionary<TKey, TValue>), static () => typeof(TKey) == typeof(string)
            ? SerializeProvider.Register(ImmutableSortedDictionaryStringKeySerializer<TValue>.Instance)
            : SerializeProvider.Register(ImmutableSortedDictionarySerializer<TKey, TValue>.Instance) },
        { typeof(IDictionary<TKey, TValue>), static () => typeof(TKey) == typeof(string)
            ? SerializeProvider.Register(InterfaceDictionaryStringKeySerializer<TValue>.Instance)
            : SerializeProvider.Register(InterfaceDictionarySerializer<TKey, TValue>.Instance) },
        { typeof(IReadOnlyDictionary<TKey, TValue>), static () => typeof(TKey) == typeof(string)
            ? SerializeProvider.Register(InterfaceReadOnlyDictionaryStringKeySerializer<TValue>.Instance)
            : SerializeProvider.Register(InterfaceReadOnlyDictionarySerializer<TKey, TValue>.Instance) },
        { typeof(IImmutableDictionary<TKey, TValue>), static () => typeof(TKey) == typeof(string)
            ? SerializeProvider.Register(InterfaceImmutableDictionaryStringKeySerializer<TValue>.Instance)
            : SerializeProvider.Register(InterfaceImmutableDictionarySerializer<TKey, TValue>.Instance) },
    };

    public static void Add(IElementSerializer serializer)
    {
        map[serializer.TypeInfo.Type] = () => SerializeProvider.Register(serializer);
    }


    public static IElementSerializer<T> Register<T>()
    // where T : IDictionary<TKey, TValue>
    {
        InvokeProvider.RegisterDictionary<T, TKey, TValue>();
        if (map.TryGetValue(typeof(T), out var func))
            return (IElementSerializer<T>)func();
        var baseType = typeof(T).BaseType;
        while (baseType != null && baseType != typeof(object))
        {
            if (map.TryGetValue(baseType, out func))
            {
                return (IElementSerializer<T>)func();
            }
            baseType = baseType.BaseType;
        }

        throw new NotImplementedException(typeof(T).ToString());
    }
}

