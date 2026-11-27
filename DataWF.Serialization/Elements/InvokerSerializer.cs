using DataWF.Invokers;
using DataWF.Serialization.Elements;
using System;

namespace DataWF.Serialization;

public class InvokerSerializer<T, V> : StringStoreSerializer<IInvoker<T, V>>
{
    public static readonly InvokerSerializer<T, V> Instance = new();

    public static new ISerializeTypeInfo<IInvoker<T, V>?> GetTypeInfo() => new SerializeTypeInfo<IInvoker<T, V>?>(
            keys: SerializeTypeKeys.Attribute,
            constructor: null!,
            properties: []
        );

    public InvokerSerializer()
        : base(GetTypeInfo())
    { }

    public override string FormatDisplay(IInvoker<T, V>? value, string? format = null)
        => value?.ToString() ?? stringNull;

    public override IInvoker<T, V>? FromString(ReadOnlySpan<char> value)
        => value.Length == 0 ? null : InvokeProvider.GetInvoker<T, V>(value.ToString());

    public override IInvoker<T, V>? FromString(string value)
        => string.IsNullOrEmpty(value) ? null : InvokeProvider.GetInvoker<T, V>(value);

    public override string ToString(IInvoker<T, V>? value)
        => value?.Name!;

    public override IInvoker<T, V> Parse(object value) => throw new NotImplementedException();



}