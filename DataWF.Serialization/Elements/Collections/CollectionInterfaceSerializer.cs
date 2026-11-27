using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace DataWF.Serialization.Elements;

public class CollectionInterfaceSerializer<T, TEntry> : AbstractSerializer<T>
    where T : IEnumerable<TEntry>
{

    public CollectionInterfaceSerializer(ISerializeTypeInfo<T> typeInfo) : base(typeInfo)
    { }

    protected override IElementSerializer? FallbackTypeInfo(
        string? shortName = null,
        ushort? union = null)
    {
        return TypeInfo.Type == typeof(IImmutableList<TEntry>)
            ? ImmutableListSerializer<TEntry>.Instance
            : TypeInfo.Type == typeof(IImmutableSet<TEntry>)
                ? ImmutableHashSetSerializer<TEntry>.Instance
                : TypeInfo.Type == typeof(IReadOnlySet<TEntry>) || TypeInfo.Type == typeof(ISet<TEntry>)
                    ? HashSetSerializer<TEntry>.Instance
            : ListSerializer<TEntry>.Instance;
    }
}
