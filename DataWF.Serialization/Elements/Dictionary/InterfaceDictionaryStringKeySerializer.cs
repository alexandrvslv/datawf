using System.Collections.Concurrent;
using System.Collections.Generic;
using DataWF.Invokers.Defaults;
using MessagePack;

namespace DataWF.Serialization.Elements;

[ExcludeFormatterFromSourceGeneratedResolver]
public class InterfaceDictionaryStringKeySerializer<V> : InterfaceDictionarySerializer<string, V>
{
    public static new readonly InterfaceDictionaryStringKeySerializer<V> Instance = new();

    public static new ISerializeTypeInfo<IDictionary<string, V>> GetTypeInfo()
    {
        return new SerializeTypeInfo<IDictionary<string, V>>(
            constructor: null!,
            properties: [
                new SerializePropertyInfo<IDictionary<string, V>, int>(
                    invoker: IDictionaryInvokers<string, V>.CountInvoker<IDictionary<string, V>>.Instance,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance),
                new SerializePropertyInfo<IDictionary<string, V>, bool>(
                    invoker: IDictionaryInvokers<string, V>.IsReadOnlyInvoker<IDictionary<string, V>>.Instance,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize),
                new SerializePropertyInfo<IDictionary<string, V>, ICollection<string>>(
                    invoker: IDictionaryInvokers<string, V>.KeysInvoker<IDictionary<string, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize),
                new SerializePropertyInfo<IDictionary<string, V>, ICollection<V>>(
                    invoker: IDictionaryInvokers<string, V>.ValuesInvoker<IDictionary<string, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize)
            ],
            descendings: [
                DictionaryStringKeySerializer<Dictionary<string, V>, V>.Instance,
                DictionaryStringKeySerializer<SortedDictionary<string, V>, V>.Instance,
                DictionaryStringKeySerializer<SortedList<string, V>, V>.Instance,
                DictionaryStringKeySerializer<ConcurrentDictionary<string, V>, V>.Instance,
            ]
        );
    }

    public InterfaceDictionaryStringKeySerializer()
        :base(GetTypeInfo())
    { }
}
