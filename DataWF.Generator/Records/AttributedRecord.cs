using Microsoft.CodeAnalysis;
using System.Linq;

namespace DataWF.Generator.Records;

public abstract record AttributedRecord(
    string Name,
    TypeRecordReference? TypeRef = null,
    EquatableList<AttributeRecord>? Attributes = null)
{
    public AttributedRecord(ISymbol p, TypeRecordProvider provider)
        : this(
            Name: p.Name, 
            Attributes: p.GetAttributes().Select(x => new AttributeRecord(x, provider)).ToEquatableList())
    { }

    public TypeRecord? Type => TypeRef?.Type;
}
