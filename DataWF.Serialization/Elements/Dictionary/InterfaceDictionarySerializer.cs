using System.Collections.Concurrent;
using System.Collections.Generic;
using DataWF.Invokers.Defaults;

namespace DataWF.Serialization.Elements;

public class InterfaceDictionarySerializer<K, V> : DictionaryInterfaceSerializer<IDictionary<K, V>, K, V>
    where K : notnull
{
    public static readonly InterfaceDictionarySerializer<K, V> Instance = new();

    public static new ISerializeTypeInfo<IDictionary<K, V>> GetTypeInfo()
    {
        return new SerializeTypeInfo<IDictionary<K, V>>(
            constructor: null!,
            properties: [
                new SerializePropertyInfo<IDictionary<K, V>, int>(
                    invoker: IDictionaryInvokers<K, V>.CountInvoker<IDictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance),
                new SerializePropertyInfo<IDictionary<K, V>, bool>(
                    invoker: IDictionaryInvokers<K, V>.IsReadOnlyInvoker<IDictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize),
                new SerializePropertyInfo<IDictionary<K, V>, ICollection<K>>(
                    invoker: IDictionaryInvokers<K, V>.KeysInvoker<IDictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize),
                new SerializePropertyInfo<IDictionary<K, V>, ICollection<V>>(
                    invoker: IDictionaryInvokers<K, V>.ValuesInvoker<IDictionary<K, V>>.Instance,
                    keys: SerializePropertyKeys.ReadOnly | SerializePropertyKeys.NonSerialize)
            ],
            descendings: [
                DictionarySerializer<Dictionary<K, V>, K, V>.Instance,
                DictionarySerializer<SortedDictionary<K, V>, K, V>.Instance,
                DictionarySerializer<SortedList<K, V>, K, V>.Instance,
                DictionarySerializer<ConcurrentDictionary<K, V>, K, V>.Instance,
            ]
        );
    }

    public InterfaceDictionarySerializer()
        : base(GetTypeInfo())
    { }

    public InterfaceDictionarySerializer(ISerializeTypeInfo<IDictionary<K, V>> typeInfo)
        : base(typeInfo)
    { }
}
