using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace DataWF.Serialization.Elements;

public abstract class DictionaryInterfaceSerializer<T, K, V> : AbstractSerializer<T>
    where K : notnull
{
    protected DictionaryInterfaceSerializer(ISerializeTypeInfo<T> typeInfo) : base(typeInfo)
    { }

    protected override IElementSerializer? FallbackTypeInfo(
        string? shortName = null,
        ushort? union = null)
    {
        return TypeInfo.Type == typeof(IImmutableDictionary<K, V>)
            ? typeof(K) == typeof(string)
                ? ImmutableDictionaryStringKeySerializer<V>.Instance
                : ImmutableDictionarySerializer<K, V>.Instance
            : typeof(K) == typeof(string)
                ? DictionaryStringKeySerializer<Dictionary<string, V>, V>.Instance
                : DictionarySerializer<Dictionary<K, V>, K, V>.Instance;
    }
}
