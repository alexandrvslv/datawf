using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DataWF.Generator.Records;

public record TypeParameterRecord(
    string Name,
    TypeRecordReference? TypeRef,
    EquatableList<AttributeRecord>? Attributes)
    : AttributedRecord(Name, TypeRef, Attributes)
{
    public string GenericParameter { get; } = string.Empty;

    public TypeParameterRecord(ITypeSymbol x, TypeRecordProvider provider)
        : this(
            Name: x.Name,
            TypeRef: x is INamedTypeSymbol namedType ? provider.GetOrCreate(namedType) : null,
            Attributes: null)
    {
        if (x is ITypeParameterSymbol typeParam)
        {
            var parts = new List<string>(8);

            if (typeParam.HasUnmanagedTypeConstraint)
                parts.Add("unmanaged");
            else if (typeParam.HasValueTypeConstraint)
                parts.Add("struct");
            else if (typeParam.HasReferenceTypeConstraint)
                if (typeParam.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated)
                    parts.Add("class?");
                else
                    parts.Add("class");
            else if (typeParam.HasNotNullConstraint)
                parts.Add("notnull");

            foreach (var constraintType in typeParam.ConstraintTypes)
                parts.Add(constraintType.ToDisplayString());

            if (typeParam.HasConstructorConstraint && !typeParam.HasValueTypeConstraint)
                parts.Add("new()");

            if (typeParam.AllowsRefLikeType)
                parts.Add("allows ref struct");

            GenericParameter = parts.Count == 0 ? string.Empty : string.Join(", ", parts);
        }
        else
        {
            GenericParameter = string.Empty;
        }
    }

    public TypeParameterRecord(TypeRecord x)
        : this(
            Name: x.Name,
            TypeRef: new TypeRecordReference(x.Name) { Type = x },
            Attributes: null)
    {
        GenericParameter = string.Empty;
    }

    public TypeParameterRecord(Type x, TypeRecordProvider provider)
        : this(
            Name: x.Name,
            TypeRef: x.IsGenericParameter ? null : provider.GetOrCreate(x),
            Attributes: !x.IsGenericParameter ? null : x.GetCustomAttributesData().Select(x => new AttributeRecord(x, provider)).ToEquatableList())
    {
        if (x.IsGenericParameter)
        {
            var attributes = x.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;
            var allAttributes = x.GetCustomAttributes();
            var parts = new List<string>(8);
            bool hasNotNullAttribute = allAttributes.Any(attr =>
                attr.GetType().FullName == "System.Runtime.CompilerServices.NotNullAttribute" ||
                attr.GetType().FullName == "System.Runtime.CompilerServices.NullableAttribute");


            if (allAttributes.Any(attr => attr.GetType().FullName == "System.Runtime.CompilerServices.IsUnmanagedAttribute"))
                parts.Add("unmanaged");
            else if (attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
                parts.Add("struct");
            else if (attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
                parts.Add("class");
            else if (hasNotNullAttribute)
                parts.Add("notnull");

            foreach (var constraint in x.GetGenericParameterConstraints())
            {
                if (constraint == typeof(ValueType))
                    continue;

                parts.Add(constraint.Name);
            }

            if (attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint) &&
                !attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
                parts.Add("new()");

            if (allAttributes.Any(attr => attr.GetType().FullName == "System.Runtime.CompilerServices.AllowsRefStructAttribute"))
                parts.Add("allows ref struct");

            GenericParameter = parts.Count == 0 ? string.Empty : string.Join(", ", parts);
        }
        else
        {
            GenericParameter = string.Empty;
        }
    }
}
