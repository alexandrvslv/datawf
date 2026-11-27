using System.Collections.Generic;

namespace DataWF.Serialization.Elements;

public class InterfaceEnumerableSerializer<TEntry> : CollectionInterfaceSerializer<IEnumerable<TEntry>, TEntry>
{
    public static readonly InterfaceEnumerableSerializer<TEntry> Instance = new();

    public static new ISerializeTypeInfo<IEnumerable<TEntry>> GetTypeInfo()
    {
        return new SerializeTypeInfo<IEnumerable<TEntry>>(
            constructor: null!,
            properties: [],
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

    public InterfaceEnumerableSerializer()
        : base(GetTypeInfo())
    { }
}
