using System.Collections.Generic;
using DataWF.Invokers.Defaults;

namespace DataWF.Serialization.Elements;

public class InterfaceReadOnlySetSerializer<TEntry> : CollectionInterfaceSerializer<IReadOnlySet<TEntry>, TEntry>
{
    public static readonly InterfaceReadOnlySetSerializer<TEntry> Instance = new();

    public static new ISerializeTypeInfo<IReadOnlySet<TEntry>> GetTypeInfo()
    {
        return new SerializeTypeInfo<IReadOnlySet<TEntry>>(
            constructor: null!,
            properties: [
                new SerializePropertyInfo<IReadOnlySet<TEntry>, int>(
                    invoker: IReadOnlySetInvokers<TEntry>.CountInvoker<IReadOnlySet<TEntry>>.Instance!,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance)
            ],
            descendings: [
                HashSetSerializer<TEntry>.Instance,
                SortedSetSerializer<TEntry>.Instance,
                ImmutableHashSetSerializer<TEntry>.Instance,
            ]
        );
    }

    public InterfaceReadOnlySetSerializer()
        : base(GetTypeInfo())
    { }
}
