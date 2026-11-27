using System.Collections.Generic;
using DataWF.Invokers.Defaults;

namespace DataWF.Serialization.Elements;

public class InterfaceReadOnlyListSerializer<TEntry> : CollectionInterfaceSerializer<IReadOnlyList<TEntry>, TEntry>
{
    public static readonly InterfaceReadOnlyListSerializer<TEntry> Instance = new();

    public static new ISerializeTypeInfo<IReadOnlyList<TEntry>> GetTypeInfo()
    {
        return new SerializeTypeInfo<IReadOnlyList<TEntry>>(
            constructor: null!,
            properties: [
                new SerializePropertyInfo<IReadOnlyList<TEntry>, int>(
                    invoker: IReadOnlyListInvokers<TEntry>.CountInvoker<IReadOnlyList<TEntry>>.Instance!,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance)
            ],
            descendings: [
                ArraySerializer<TEntry>.ArrInstance,
                ListSerializer<TEntry>.Instance,
                ImmutableListSerializer<TEntry>.Instance,
                ImmutableArraySerializer<TEntry>.Instance,
            ]
        );
    }

    public InterfaceReadOnlyListSerializer()
        : base(GetTypeInfo())
    { }
}
