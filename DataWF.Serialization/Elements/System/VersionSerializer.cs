using System;
using DataWF.Invokers.Defaults;

namespace DataWF.Serialization.Elements;

public sealed class VersionSerializer : StringStoreSerializer<Version>
{
    public static readonly VersionSerializer Instance = new();

    public static new ISerializeTypeInfo<Version?> GetTypeInfo()
    {
        VersionInvokers.RegisterInvokers();
        return new SerializeTypeInfo<Version>(
            keys: SerializeTypeKeys.Attribute,
            constructor: VersionInvokers.VersionConstructorString.Instance,
            properties: [
                new SerializePropertyInfo<Version, int>(
                    invoker: VersionInvokers.BuildInvoker.Instance,
                    keys: SerializePropertyKeys.ReadOnlyAttribute
                ),
                new SerializePropertyInfo<Version, int>(
                    invoker: VersionInvokers.MajorInvoker.Instance,
                    keys: SerializePropertyKeys.ReadOnlyAttribute
                ),
                new SerializePropertyInfo<Version, short>(
                    invoker: VersionInvokers.MajorRevisionInvoker.Instance,
                    keys: SerializePropertyKeys.ReadOnlyAttribute
                ),
                new SerializePropertyInfo<Version, int>(
                    invoker: VersionInvokers.MinorInvoker.Instance,
                    keys: SerializePropertyKeys.ReadOnlyAttribute
                ),
                new SerializePropertyInfo<Version, short>(
                    invoker: VersionInvokers.MinorRevisionInvoker.Instance,
                    keys: SerializePropertyKeys.ReadOnlyAttribute
                ),
                new SerializePropertyInfo<Version, int>(
                    invoker: VersionInvokers.RevisionInvoker.Instance,
                    keys: SerializePropertyKeys.ReadOnlyAttribute
                ),
            ]
        )!;
    }

    public VersionSerializer()
        : base(GetTypeInfo())
    { }

    public override Version? FromString(ReadOnlySpan<char> value)
        => Version.TryParse(value, out var version) ? version : null;

    public override Version? FromString(string value)
        => Version.TryParse(value, out var version) ? version : null;

    public override string ToString(Version? value)
        => value?.ToString() ?? string.Empty;

    public override string FormatDisplay(Version? value, string? format = null)
        => value?.ToString() ?? string.Empty;
}
