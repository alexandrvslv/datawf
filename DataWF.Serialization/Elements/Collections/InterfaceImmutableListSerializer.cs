using System.Collections.Generic;
using System.Collections.Immutable;
using DataWF.Invokers.Defaults;

namespace DataWF.Serialization.Elements;

public class InterfaceImmutableListSerializer<TEntry> : CollectionInterfaceSerializer<IImmutableList<TEntry>, TEntry>
{
    public static readonly InterfaceImmutableListSerializer<TEntry> Instance = new();

    public static new ISerializeTypeInfo<IImmutableList<TEntry>> GetTypeInfo()
    {
        return new SerializeTypeInfo<IImmutableList<TEntry>>(
            constructor: null!,
            properties: [
                new SerializePropertyInfo<IImmutableList<TEntry>, int>(
                    invoker: IImmutableListInvokers<TEntry>.CountInvoker<IImmutableList<TEntry>>.Instance!,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance)
            ],
            descendings: [
                ImmutableListSerializer<TEntry>.Instance,
                ImmutableArraySerializer<TEntry>.Instance,
            ]
        );
    }

    public InterfaceImmutableListSerializer()
        : base(GetTypeInfo())
    { }
}
