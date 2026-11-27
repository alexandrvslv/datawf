using System.Collections.Concurrent;
using System.Collections.Generic;
using DataWF.Invokers.Defaults;

namespace DataWF.Serialization.Elements;

public class InterfaceReadOnlyDictionarySerializer<K, V> : DictionaryInterfaceSerializer<IReadOnlyDictionary<K, V>, K, V>
    where K : notnull
{
    public static readonly InterfaceReadOnlyDictionarySerializer<K, V> Instance = new();

    public static new ISerializeTypeInfo<IReadOnlyDictionary<K, V>> GetTypeInfo()
    {
        return new SerializeTypeInfo<IReadOnlyDictionary<K, V>>(
            constructor: null!,
            properties: [
                new SerializePropertyInfo<IReadOnlyDictionary<K, V>, int>(
                    invoker: IReadOnlyDictionaryInvokers<K, V>.CountInvoker<IReadOnlyDictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance),
                new SerializePropertyInfo<IReadOnlyDictionary<K, V>, IEnumerable<K>>(
                    invoker: IReadOnlyDictionaryInvokers<K, V>.KeysInvoker<IReadOnlyDictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize),
                new SerializePropertyInfo<IReadOnlyDictionary<K, V>, IEnumerable<V>>(
                    invoker: IReadOnlyDictionaryInvokers<K, V>.ValuesInvoker<IReadOnlyDictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize)
            ],
            descendings: [
                DictionarySerializer<Dictionary<K, V>, K, V>.Instance,
                DictionarySerializer<SortedDictionary<K, V>, K, V>.Instance,
                DictionarySerializer<SortedList<K, V>, K, V>.Instance,
                DictionarySerializer<ConcurrentDictionary<K, V>, K, V>.Instance,
                ImmutableDictionarySerializer<K, V>.Instance,
                ImmutableSortedDictionarySerializer<K, V>.Instance,
            ]
        );
    }

    public InterfaceReadOnlyDictionarySerializer()
        : base(GetTypeInfo())
    { }

    public InterfaceReadOnlyDictionarySerializer(ISerializeTypeInfo<IReadOnlyDictionary<K, V>> typeInfo)
        : base(typeInfo)
    { }
}
