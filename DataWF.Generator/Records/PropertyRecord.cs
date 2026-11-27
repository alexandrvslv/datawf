using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Reflection;

namespace DataWF.Generator.Records;

public record PropertyRecord(
    string Name,
    TypeRecordReference? TypeRef,
    PropertyRecordKeys Keys = PropertyRecordKeys.Public,
    EquatableList<ParameterRecord>? Parameters = null,
    EquatableList<AttributeRecord>? Attributes = null)
    : AttributedRecord(Name, TypeRef, Attributes)
{
    public PropertyRecord(IPropertySymbol p, TypeRecordProvider provider)
        : this(
            Name: p.Name,
            TypeRef: provider.GetOrCreate(
                typeSymbol: p.Type,
                takeBase: TakeBaseMode.Take | TakeBaseMode.Property | TakeBaseMode.Method | TakeBaseMode.Attribute,
                validateProperty: TypeRecord.AllProperties,
                validateMethod: TypeRecord.AllMethods,
                validateAttribute: TypeRecord.AllAttributes),
            Keys: (p.IsReadOnly ? PropertyRecordKeys.ReadOnly : PropertyRecordKeys.None) |
                   (p.IsStatic ? PropertyRecordKeys.Static : PropertyRecordKeys.None) |
                   (p.IsPartialDefinition ? PropertyRecordKeys.Partial : PropertyRecordKeys.None) |
                   (p.IsOverride ? PropertyRecordKeys.Override : PropertyRecordKeys.None) |
                   (p.DeclaredAccessibility == Accessibility.Public ? PropertyRecordKeys.Public : PropertyRecordKeys.None) |
                   (!p.IsReadOnly && p.SetMethod!.IsInitOnly is true ? PropertyRecordKeys.InitOnly : PropertyRecordKeys.None),
            Parameters: p.Parameters.Select(x => new ParameterRecord(x, provider)).ToEquatableList(),
            Attributes: p.GetAttributes().Select(x => new AttributeRecord(x, provider)).ToEquatableList())
    { }

    public PropertyRecord(PropertyInfo x, TypeRecordProvider provider)
        : this(
            Name: x.Name,
            TypeRef: provider.GetOrCreate(x.PropertyType),
            Keys: (x.CanWrite is false ? PropertyRecordKeys.ReadOnly : PropertyRecordKeys.None) |
                   (x.GetMethod?.IsStatic ?? false ? PropertyRecordKeys.Static : PropertyRecordKeys.None) |
                   (false ? PropertyRecordKeys.Partial : PropertyRecordKeys.None) |
                   (x.GetGetMethod()?.GetBaseDefinition().DeclaringType != x.DeclaringType ? PropertyRecordKeys.Override : PropertyRecordKeys.None) |
                   (x.GetMethod?.IsPublic ?? false ? PropertyRecordKeys.Public : PropertyRecordKeys.None) |
                   (false ? PropertyRecordKeys.InitOnly : PropertyRecordKeys.None),
            Parameters: x.GetIndexParameters().Select(x => new ParameterRecord(x, provider)).ToEquatableList(),
            Attributes: x.GetCustomAttributesData().Select(x => new AttributeRecord(x, provider)).ToEquatableList())
    { }

    new public TypeRecord Type => TypeRef!.Type!;

    public bool IsReadOnly => (Keys & PropertyRecordKeys.ReadOnly) == PropertyRecordKeys.ReadOnly;
    public bool IsStatic => (Keys & PropertyRecordKeys.Static) == PropertyRecordKeys.Static;
    public bool IsPartial => (Keys & PropertyRecordKeys.Partial) == PropertyRecordKeys.Partial;
    public bool IsOverride => (Keys & PropertyRecordKeys.Override) == PropertyRecordKeys.Override;
    public bool IsPublic => (Keys & PropertyRecordKeys.Public) == PropertyRecordKeys.Public;
    public bool IsInitOnly => (Keys & PropertyRecordKeys.InitOnly) == PropertyRecordKeys.InitOnly;

    internal bool IsIndex => Parameters != null && Parameters.Count > 0;
}

[Flags]
public enum PropertyRecordKeys: byte
{
    None = 0,
    ReadOnly = 1,
    Static = 2,
    Partial = 4,
    Override = 8,
    Public = 16,
    InitOnly = 32,
}
