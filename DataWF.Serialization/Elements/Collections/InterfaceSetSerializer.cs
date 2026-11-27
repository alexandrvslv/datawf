using System.Collections.Generic;
using DataWF.Invokers.Defaults;

namespace DataWF.Serialization.Elements;

public class InterfaceSetSerializer<TEntry> : CollectionInterfaceSerializer<ISet<TEntry>, TEntry>
{
    public static readonly InterfaceSetSerializer<TEntry> Instance = new();

    public static new ISerializeTypeInfo<ISet<TEntry>> GetTypeInfo()
    {
        return new SerializeTypeInfo<ISet<TEntry>>(
            constructor: null!,
            properties: [
                new SerializePropertyInfo<ISet<TEntry>, int>(
                    invoker: ISetInvokers<TEntry>.CountInvoker<ISet<TEntry>>.Instance!,
                    keys: SerializePropertyKeys.Attribute | SerializePropertyKeys.ReadOnly,
                    serializer: Int32Serializer.Instance)
            ],
            descendings: [
                HashSetSerializer<TEntry>.Instance,
                SortedSetSerializer<TEntry>.Instance,
            ]
        );
    }

    public InterfaceSetSerializer()
        : base(GetTypeInfo())
    { }

    
}
