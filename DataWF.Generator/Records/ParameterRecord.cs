using Microsoft.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace DataWF.Generator.Records;

public record ParameterRecord(
    string Name,
    TypeRecordReference? TypeRef,
    EquatableList<AttributeRecord>? Attributes = null)
    : AttributedRecord(Name, TypeRef, Attributes)
{
    public ParameterRecord(IParameterSymbol p, TypeRecordProvider provider)
        : this(
            Name: p.Name,
            TypeRef: provider.GetOrCreate(p.Type),
            Attributes: p.GetAttributes().Select(x => new AttributeRecord(x, provider)).ToEquatableList())
    { }

    public ParameterRecord(ParameterInfo p, TypeRecordProvider provider)
        : this(
            Name: p.Name,
            TypeRef: provider.GetOrCreate(p.ParameterType),
            Attributes: p.GetCustomAttributesData().Select(x => new AttributeRecord(x, provider)).ToEquatableList())
    { }

    public new TypeRecord? Type => TypeRef?.Type;
}

