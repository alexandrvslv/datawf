using MemoryPack;
using System;
using System.Data.Common;
using System.Text.Json;
using MessagePack;

namespace DataWF.Serialization.Elements;

public abstract class StringStoreSerializer<T> : ElementSerializer<T?>
{
    protected StringStoreSerializer(ISerializeTypeInfo<T?> typeInfo) : base(typeInfo)
    { }

    public override bool IsAttributeSerializer => true;

    public override T? Read(DbDataReader reader, int i)
        => reader.IsDBNull(i) ? default(T) : FromString(reader.GetString(i));

    public override void Read(XmlInvokeReader reader, scoped ref T? value)
        => value = FromString(reader.ReadContentAsString());

    public override void Write(XmlInvokeWriter writer, scoped ref T? value)
    {
        if (value is T notNullValue)
            writer.WriteValue(ToString(notNullValue));
        else
            writer.WriteNullValue();
    }

    public override void Write(XmlInvokeWriter writer, scoped ref T? value, ISerializePropertyInfo propertyInfo)
    {
        if (value is T notNullValue)
            writer.WriteAttributeString(propertyInfo.Name, ToString(notNullValue));
        else
            writer.WriteAttributeString(propertyInfo.Name, stringXmlNil);
    }

    public override void Read(ref Utf8JsonReader reader, scoped ref T? value, JsonSerializerOptions options)
        => value = reader.TokenType == JsonTokenType.Null ? default : FromString(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, scoped ref T? value, JsonSerializerOptions options)
    {
        if (value is T notNullValue)
            writer.WriteStringValue(ToString(notNullValue));
        else
            writer.WriteNullValue();
    }

    public override void Write(Utf8JsonWriter writer, scoped ref T? value, JsonSerializerOptions options, ISerializePropertyInfo propertyInfo)
    {
        if (value is T notNullValue)
            writer.WriteString(propertyInfo.JsonName, ToString(notNullValue));
        else
            writer.WriteNull(propertyInfo.JsonName);
    }

    public override void Read(ref MemoryPackReader reader, scoped ref T? value)
        => value = FromString(reader.ReadString()!);

    public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T? value)
        => writer.WriteString(ToString(value!));

    public override void Read(ref MessagePackReader reader, scoped ref T? value, MessagePackSerializerOptions options)
    {
        var text = reader.ReadString();
        value = text == null ? default : FromString(text.AsSpan());
    }

    public override void Write(ref MessagePackWriter writer, scoped ref T? value, MessagePackSerializerOptions options)
    {
        if (value is T notNullValue)
            writer.Write(ToString(notNullValue));
        else
            writer.WriteNil();
    }

    public override T? Parse(object value)
    {
        if (value is T typedValue)
            return typedValue;
        if (value == null || value == DBNull.Value)
            return default(T);
        return FromString(value.ToString()!);
    }
}
