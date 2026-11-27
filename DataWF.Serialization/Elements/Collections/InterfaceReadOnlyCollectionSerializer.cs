using System.Collections.Generic;
using DataWF.Invokers.Defaults;

namespace DataWF.Serialization.Elements;

public class InterfaceReadOnlyCollectionSerializer<TEntry> : CollectionInterfaceSerializer<IReadOnlyCollection<TEntry>, TEntry>
{
    public static readonly InterfaceReadOnlyCollectionSerializer<TEntry> Instance = new();

    public static new ISerializeTypeInfo<IReadOnlyCollection<TEntry>> GetTypeInfo()
    {
        return new SerializeTypeInfo<IReadOnlyCollection<TEntry>>(
            constructor: null!,
            properties: [
                new SerializePropertyInfo<IReadOnlyCollection<TEntry>, int>(
                    invoker: IReadOnlyCollectionInvokers<TEntry>.CountInvoker<IReadOnlyCollection<TEntry>>.Instance!,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance)
            ],
            descendings: [
                ArraySerializer<TEntry>.ArrInstance,
                ListSerializer<TEntry>.Instance,
                LinkedListSerializer<TEntry>.Instance,
                HashSetSerializer<TEntry>.Instance,
                SortedSetSerializer<TEntry>.Instance,
                QueueSerializer<TEntry>.Instance,
                StackSerializer<TEntry>.Instance,
                ConcurrentBagSerializer<TEntry>.Instance,
                ConcurrentQueueSerializer<TEntry>.Instance,
                ConcurrentStackSerializer<TEntry>.Instance,
                ImmutableListSerializer<TEntry>.Instance,
                ImmutableArraySerializer<TEntry>.Instance,
                ImmutableHashSetSerializer<TEntry>.Instance,
            ]
        );
    }

    public InterfaceReadOnlyCollectionSerializer()
        : base(GetTypeInfo())
    { }
}
