using System.Collections.Generic;
using System.Collections.Immutable;
using DataWF.Invokers.Defaults;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public class InterfaceImmutableDictionaryStringKeySerializer<V> : InterfaceImmutableDictionarySerializer<string, V>
{
    public static new readonly InterfaceImmutableDictionaryStringKeySerializer<V> Instance = new();

    public static new ISerializeTypeInfo<IImmutableDictionary<string, V>> GetTypeInfo()
    {
        return new SerializeTypeInfo<IImmutableDictionary<string, V>>(
            constructor: null!,
            properties: [
                new SerializePropertyInfo<IImmutableDictionary<string, V>, int>(
                    invoker: IImmutableDictionaryInvokers<string, V>.CountInvoker<IImmutableDictionary<string, V>>.Instance,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance),
                new SerializePropertyInfo<IImmutableDictionary<string, V>, IEnumerable<string>>(
                    invoker: IImmutableDictionaryInvokers<string, V>.KeysInvoker<IImmutableDictionary<string, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize),
                new SerializePropertyInfo<IImmutableDictionary<string, V>, IEnumerable<V>>(
                    invoker: IImmutableDictionaryInvokers<string, V>.ValuesInvoker<IImmutableDictionary<string, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize)
            ],
            descendings: [
                ImmutableDictionaryStringKeySerializer<V>.Instance,
                ImmutableSortedDictionaryStringKeySerializer<V>.Instance,
            ]
        );
    }

    public InterfaceImmutableDictionaryStringKeySerializer()
        : base(GetTypeInfo())
    { }
}