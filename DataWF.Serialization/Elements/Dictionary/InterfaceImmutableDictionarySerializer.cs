using System.Collections.Generic;
using System.Collections.Immutable;
using DataWF.Invokers.Defaults;

namespace DataWF.Serialization.Elements;

public class InterfaceImmutableDictionarySerializer<K, V> : DictionaryInterfaceSerializer<IImmutableDictionary<K, V>, K, V>
    where K : notnull
{
    public static readonly InterfaceImmutableDictionarySerializer<K, V> Instance = new();

    public static new ISerializeTypeInfo<IImmutableDictionary<K, V>> GetTypeInfo()
    {
        return new SerializeTypeInfo<IImmutableDictionary<K, V>>(
            constructor: null!,
            properties: [
                new SerializePropertyInfo<IImmutableDictionary<K, V>, int>(
                    invoker: IImmutableDictionaryInvokers<K, V>.CountInvoker<IImmutableDictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance),
                new SerializePropertyInfo<IImmutableDictionary<K, V>, IEnumerable<K>>(
                    invoker: IImmutableDictionaryInvokers<K, V>.KeysInvoker<IImmutableDictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize),
                new SerializePropertyInfo<IImmutableDictionary<K, V>, IEnumerable<V>>(
                    invoker: IImmutableDictionaryInvokers<K, V>.ValuesInvoker<IImmutableDictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize)
            ],
            descendings: [
                ImmutableDictionarySerializer<K, V>.Instance,
                ImmutableSortedDictionarySerializer<K,V>.Instance,
            ]
        );
    }

    public InterfaceImmutableDictionarySerializer()
        : base(GetTypeInfo())
    { }

    public InterfaceImmutableDictionarySerializer(ISerializeTypeInfo<IImmutableDictionary<K, V>> typeInfo)
        : base(typeInfo)
    { }
}
