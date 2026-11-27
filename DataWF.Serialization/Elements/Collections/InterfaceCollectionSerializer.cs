using System.Collections.Generic;
using DataWF.Invokers.Defaults;

namespace DataWF.Serialization.Elements;

public class InterfaceCollectionSerializer<TEntry> : CollectionInterfaceSerializer<ICollection<TEntry>, TEntry>
{
    public static readonly InterfaceCollectionSerializer<TEntry> Instance = new();

    public static new ISerializeTypeInfo<ICollection<TEntry>> GetTypeInfo()
    {
        return new SerializeTypeInfo<ICollection<TEntry>>(
            constructor: null!,
            properties: [
                new SerializePropertyInfo<ICollection<TEntry>, int>(
                    invoker: ICollectionInvokers<TEntry>.CountInvoker<ICollection<TEntry>>.Instance!,
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
                ImmutableListSerializer<TEntry>.Instance,
                ImmutableArraySerializer<TEntry>.Instance,
                ImmutableHashSetSerializer<TEntry>.Instance,
            ]
        );
    }

    public InterfaceCollectionSerializer()
        : base(GetTypeInfo())
    { }
}