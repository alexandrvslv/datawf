using System;
using System.Collections.Generic;
using MessagePack;
using System.Text.Json;

namespace DataWF.Serialization.Elements;

public abstract class GenericCollectionSerializer<T, TElement> : ElementSerializer<T>
    where T : IEnumerable<TElement>
{
    IElementSerializer<TElement> elementSerializer;

    protected GenericCollectionSerializer(ISerializeTypeInfo<T> typeInfo)
        : base(typeInfo)
    {
        elementSerializer = SerializeProvider.GetSerializer<TElement>();
    }

    protected IElementSerializer<TElement> ElementSerializer => elementSerializer;

    public override bool IsAttributeSerializer => false;

    public override string FormatDisplay(T? value, string? format = null)
    {
        if (value == null)
            return stringNull;
        var stringBuilder = new System.Text.StringBuilder();
        stringBuilder.Append('[');
        var elementSerializer = ElementSerializer;
        foreach (var entry in value)
        {
            stringBuilder.Append(elementSerializer.FormatDisplay(entry));
            stringBuilder.Append(',').Append('\n');
        }
        stringBuilder.Append(']');
        return stringBuilder.ToString();
    }

    public override T FromString(ReadOnlySpan<char> value)
        => JsonSerializer.Deserialize<T>(value, options: new JsonSerializerOptions { Converters = { this } })!;

    public override string ToString(T? value)
        => JsonSerializer.Serialize(value, options: new JsonSerializerOptions { Converters = { this } });

    public override T Parse(object value)
       => value switch
       {
           T same => same,
           null => default!,
           _ => FromString(value.ToString())
       };
}
