using System;

namespace DataWF.Serialization.Elements;

public class SerializeObjectInfo : SerializeTypeInfo<object?>
{
    public SerializeObjectInfo()
        : base(constructor: null!, 
            typeName:  nameof(Object), 
            shortName: nameof(Object),
            properties: Array.Empty<ISerializePropertyInfo<object?>>(), 
            keys: SerializeTypeKeys.None
        )
    { }

    public override IElementSerializer? GetDescending(string name)
    {
        return SerializeProvider.NameIndex.TryGetValue(name, out var serializer)
            ? serializer
            : null;
    }

    public override IElementSerializer? GetDescending(ushort union)
    {
        return SerializeProvider.UnionIndex.TryGetValue(union, out var serializer)
            ? serializer
            : null;
    }

    public override IElementSerializer? GetDescending(Type type)
    {
        return SerializeProvider.GetSerializer(type);
    }
}