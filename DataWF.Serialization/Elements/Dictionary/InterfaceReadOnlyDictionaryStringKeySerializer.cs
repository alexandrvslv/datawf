using System.Collections.Concurrent;
using System.Collections.Generic;
using DataWF.Invokers.Defaults;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public class InterfaceReadOnlyDictionaryStringKeySerializer<V> : DictionaryInterfaceSerializer<IReadOnlyDictionary<string, V>, string, V>
{
    public static readonly InterfaceReadOnlyDictionaryStringKeySerializer<V> Instance = new();

    public static new ISerializeTypeInfo<IReadOnlyDictionary<string, V>> GetTypeInfo()
    {
        return new SerializeTypeInfo<IReadOnlyDictionary<string, V>>(
            constructor: null!,
            properties: [
                new SerializePropertyInfo<IReadOnlyDictionary<string, V>, int>(
                    invoker: IReadOnlyDictionaryInvokers<string, V>.CountInvoker<IReadOnlyDictionary<string, V>>.Instance,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance),
                new SerializePropertyInfo<IReadOnlyDictionary<string, V>, IEnumerable<string>>(
                    invoker: IReadOnlyDictionaryInvokers<string, V>.KeysInvoker<IReadOnlyDictionary<string, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize),
                new SerializePropertyInfo<IReadOnlyDictionary<string, V>, IEnumerable<V>>(
                    invoker: IReadOnlyDictionaryInvokers<string, V>.ValuesInvoker<IReadOnlyDictionary<string, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize)
            ],
            descendings: [
                DictionaryStringKeySerializer<Dictionary<string, V>, V>.Instance,
                DictionaryStringKeySerializer<SortedDictionary<string, V>, V>.Instance,
                DictionaryStringKeySerializer<SortedList<string, V>, V>.Instance,
                DictionaryStringKeySerializer<ConcurrentDictionary<string, V>, V>.Instance,
                ImmutableDictionaryStringKeySerializer<V>.Instance,
                ImmutableSortedDictionaryStringKeySerializer<V>.Instance,
            ]
        );
    }

    public InterfaceReadOnlyDictionaryStringKeySerializer()
        : base(GetTypeInfo())
    { }
}
