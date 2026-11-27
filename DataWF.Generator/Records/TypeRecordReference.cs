using System;

namespace DataWF.Generator.Records;

public record TypeRecordReference(string Name)
{
    private TypeRecord? type;

    public TypeRecord? Type
    {
        get => type ??= Origin?.Type is { } pType ? pType with
        {
            Name = Name!,
            Keys = (pType.Keys | TypeKeys) & (TypeKeys.HasFlag(TypeRecordKeys.Array)
                ? ~(TypeRecordKeys.ValueType
                    | TypeRecordKeys.Unmanaged
                    | TypeRecordKeys.Enum
                    | TypeRecordKeys.Record
                    | TypeRecordKeys.System) : ~TypeRecordKeys.None),
            OriginType = Origin.Type,
        }
            : CreateArgs.Create();
        set => type = value;
    }

    public TypeRecordReference? Origin { get; internal set; }

    public TypeRecordKeys TypeKeys { get; internal set; }

    public CreateArgs CreateArgs { get; internal set; }

    public override string ToString()
    {
        return Type?.ToString() ?? string.Empty;
    }

    public override int GetHashCode() => Name.GetHashCode();

    public virtual bool Equals(TypeRecordReference? other) => string.Equals(Name, other?.Name, StringComparison.Ordinal);
}
