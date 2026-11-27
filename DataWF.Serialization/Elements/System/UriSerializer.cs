using System;
using DataWF.Invokers.Defaults;

namespace DataWF.Serialization.Elements;

public sealed class UriSerializer : StringStoreSerializer<Uri>
{
    public static readonly UriSerializer Instance = new();

    public static new ISerializeTypeInfo<Uri?> GetTypeInfo()
    {
        UriInvokers.RegisterInvokers();
        return new SerializeTypeInfo<Uri>(
            keys: SerializeTypeKeys.Attribute,
            constructor: UriInvokers.UriConstructorString.Instance,
            properties: [
                new SerializePropertyInfo<Uri, string>(
                    invoker: UriInvokers.AbsolutePathInvoker<Uri>.Instance,
                    keys: SerializePropertyKeys.ReadOnlyNonSerializeAttribute
                ),
                new SerializePropertyInfo<Uri, string>(
                    invoker: UriInvokers.AbsoluteUriInvoker<Uri>.Instance,
                    keys: SerializePropertyKeys.ReadOnlyNonSerializeAttribute
                ),
                new SerializePropertyInfo<Uri, string>(
                    invoker: UriInvokers.QueryInvoker<Uri>.Instance,
                    keys: SerializePropertyKeys.ReadOnlyNonSerializeAttribute
                ),
                new SerializePropertyInfo<Uri, string>(
                    invoker: UriInvokers.HostInvoker<Uri>.Instance,
                    keys: SerializePropertyKeys.ReadOnlyNonSerializeAttribute
                ),
                new SerializePropertyInfo<Uri, UriHostNameType>(
                    invoker: UriInvokers.HostNameTypeInvoker<Uri>.Instance,
                    keys: SerializePropertyKeys.ReadOnlyNonSerializeAttribute
                ),
                new SerializePropertyInfo<Uri, string>(
                    invoker: UriInvokers.SchemeInvoker<Uri>.Instance,
                    keys: SerializePropertyKeys.ReadOnlyNonSerializeAttribute
                ),
            ]
        )!;
    }

    public UriSerializer()
        : base(GetTypeInfo())
    { }

    public override bool IsAttributeSerializer => true;

    public override Uri? FromString(ReadOnlySpan<char> value)
        => value.IsEmpty ? default : Uri.TryCreate(value.ToString(), UriKind.RelativeOrAbsolute, out var result) ? result : default!;

    public override Uri? FromString(string value)
        => string.IsNullOrEmpty(value) ? default : Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var result) ? result : default!;

    public override string ToString(Uri? value) => value?.ToString() ?? string.Empty;

    public override string FormatDisplay(Uri? value, string? format = null) => value?.ToString() ?? string.Empty;
}

