using System;
using System.Globalization;
using DataWF.Invokers.Defaults;

namespace DataWF.Serialization.Elements;

public sealed class CultureInfoSerializer : StringStoreSerializer<CultureInfo>
{
    public static readonly CultureInfoSerializer Instance = new();

    public static new ISerializeTypeInfo<CultureInfo?> GetTypeInfo()
    {
        CultureInfoInvokers.RegisterInvokers();
        return new SerializeTypeInfo<CultureInfo>(
            keys: SerializeTypeKeys.Attribute,
            constructor: CultureInfoInvokers.CultureInfoConstructorString.Instance,
            properties: [
                new SerializePropertyInfo<CultureInfo, Calendar>(
                    invoker: CultureInfoInvokers.CalendarInvoker<CultureInfo>.Instance,
                    keys: SerializePropertyKeys.ReadOnlyNonSerializeAttribute
                ),
                new SerializePropertyInfo<CultureInfo, string>(
                    invoker: CultureInfoInvokers.NameInvoker<CultureInfo>.Instance,
                    keys: SerializePropertyKeys.ReadOnlyNonSerializeAttribute
                ),
                //...
            ]
        )!;
    }

    public CultureInfoSerializer()
        : base(GetTypeInfo())
    { }

    public override CultureInfo? FromString(ReadOnlySpan<char> value)
        => value.IsEmpty ? null : CultureInfo.GetCultureInfo(value.ToString());

    public override CultureInfo? FromString(string value)
        => string.IsNullOrEmpty(value) ? null : CultureInfo.GetCultureInfo(value);

    public override string ToString(CultureInfo? value) => value?.Name ?? string.Empty;

    public override string FormatDisplay(CultureInfo? value, string? format = null) => value?.DisplayName ?? string.Empty;
}

public static class CultureInfoExtensions
{
    public static CultureInfo ToCulture(this string cultureName) => CultureInfo.GetCultureInfo(cultureName);
}
