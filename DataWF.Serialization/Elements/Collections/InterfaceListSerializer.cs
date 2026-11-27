using System.Collections.Generic;
using DataWF.Invokers.Defaults;

namespace DataWF.Serialization.Elements;

public class InterfaceListSerializer<TEntry> : CollectionInterfaceSerializer<IList<TEntry>, TEntry>
{
    public static readonly InterfaceListSerializer<TEntry> Instance = new();

    public static new ISerializeTypeInfo<IList<TEntry>> GetTypeInfo()
    {
        return new SerializeTypeInfo<IList<TEntry>>(
            constructor: null!,
            properties: [
                new SerializePropertyInfo<IList<TEntry>, int>(
                    invoker: IListInvokers<TEntry>.CountInvoker<IList<TEntry>>.Instance!,
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

    public InterfaceListSerializer()
        : base(GetTypeInfo())
    { }
}
