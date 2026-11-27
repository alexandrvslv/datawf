using System;
using MessagePack;

namespace DataWF.Serialization.Elements;

public sealed class TypeSerializer : StringStoreSerializer<Type>
{
    public static readonly TypeSerializer Instance = new();

    public static new ISerializeTypeInfo<Type?> GetTypeInfo()
    {
        return new SerializeTypeInfo<Type?>(
            keys: SerializeTypeKeys.Attribute,
            typeName: nameof(Type),
            shortName: nameof(Type),
            constructor: null!,
            properties: []
        );
    }

    public TypeSerializer()
        : base(GetTypeInfo())
    { }

    public override Type? FromString(ReadOnlySpan<char> value)
        => value.Length == 0 ? null : TypeHelper.ParseType(value.ToString());

    public override Type? FromString(string value)
        => string.IsNullOrEmpty(value) ? null : TypeHelper.ParseType(value);

    public override string ToString(Type? value) => value == null ? string.Empty : TypeHelper.FormatBinary(value);

    public override string FormatDisplay(Type? value, string? format = null) => value?.Name ?? string.Empty;
}

[ExcludeFormatterFromSourceGeneratedResolver]
public sealed class TypeShortSerializer : StringStoreSerializer<Type>
{
    public static readonly TypeShortSerializer Instance = new TypeShortSerializer();

    public TypeShortSerializer()
        : base(TypeSerializer.GetTypeInfo())
    { }

    public override Type? FromString(ReadOnlySpan<char> value) => TypeHelper.ParseType(value.ToString());

    public override string ToString(Type? value) => value == null ? string.Empty : TypeHelper.FormatBinary(value, true);

    public override string FormatDisplay(Type? value, string? format = null) => value?.Name ?? string.Empty;
}
