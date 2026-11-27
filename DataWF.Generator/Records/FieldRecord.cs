using Microsoft.CodeAnalysis;
using System.Linq;

namespace DataWF.Generator.Records;

public record FieldRecord(
        string Name,
        TypeRecordReference? TypeRef,
        FieldRecordKeys Keys,
        EquatableList<AttributeRecord>? Attributes = null)
        : AttributedRecord(Name, TypeRef, Attributes)
{
    public FieldRecord(IFieldSymbol p, TypeRecordProvider provider)
        : this(
            Name: p.Name, 
            TypeRef: provider.GetOrCreate(p.Type),
            Keys: (p.IsReadOnly ? FieldRecordKeys.InitOnly : FieldRecordKeys.None) |
                   (p.IsRequired ? FieldRecordKeys.Required : FieldRecordKeys.None) |
                   (p.IsStatic ? FieldRecordKeys.Static : FieldRecordKeys.None) |
                   (p.DeclaredAccessibility == Accessibility.Public ? FieldRecordKeys.Public : FieldRecordKeys.None),
            Attributes: p.GetAttributes().Select(x => new AttributeRecord(x, provider)).ToEquatableList())
    { }

    public FieldRecord(System.Reflection.FieldInfo p, TypeRecordProvider provider)
        : this(
            Name: p.Name,
            TypeRef: provider.GetOrCreate(p.FieldType),
            Keys: (p.IsInitOnly ? FieldRecordKeys.InitOnly : FieldRecordKeys.None) |
                   (false ? FieldRecordKeys.Required : FieldRecordKeys.None) |
                   (p.IsStatic ? FieldRecordKeys.Static : FieldRecordKeys.None) |
                   (p.IsPublic ? FieldRecordKeys.Public : FieldRecordKeys.None),
            Attributes: p.GetCustomAttributesData().Select(x => new AttributeRecord(x, provider)).ToEquatableList())
    { }

    public new TypeRecord Type => TypeRef?.Type!;

    public bool IsInitOnly => (Keys & FieldRecordKeys.InitOnly) == FieldRecordKeys.InitOnly;
    public bool IsRequired => (Keys & FieldRecordKeys.Required) == FieldRecordKeys.Required;
    public bool IsStatic => (Keys & FieldRecordKeys.Static) == FieldRecordKeys.Static;
    public bool IsPublic => (Keys & FieldRecordKeys.Public) == FieldRecordKeys.Public;
}

public enum FieldRecordKeys: byte
{
    None = 0,
    InitOnly = 1,
    Required = 2,
    Static = 4,
    Public = 8
}
