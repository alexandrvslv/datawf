using System.Collections.Immutable;
using DataWF.Invokers.Defaults;

namespace DataWF.Serialization.Elements;

public class InterfaceImmutableSetSerializer<TEntry> : CollectionInterfaceSerializer<IImmutableSet<TEntry>, TEntry>
{
    public static readonly InterfaceImmutableSetSerializer<TEntry> Instance = new();

    public static new ISerializeTypeInfo<IImmutableSet<TEntry>> GetTypeInfo()
    {
        return new SerializeTypeInfo<IImmutableSet<TEntry>>(
            constructor: null!,
            properties: [
                new SerializePropertyInfo<IImmutableSet<TEntry>, int>(
                    invoker: IImmutableSetInvokers<TEntry>.CountInvoker<IImmutableSet<TEntry>>.Instance!,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance)
            ],
            descendings: [
                ImmutableHashSetSerializer<TEntry>.Instance,
            ]
        );
    }

    public InterfaceImmutableSetSerializer()
        : base(GetTypeInfo())
    { }
}