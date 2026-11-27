using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Generator.Records;

public static class TypeRecordExtensions
{
    public static string ToCodeFormat(this TypeTypeEnum typeTypeEnum) => typeTypeEnum switch
    {
        TypeTypeEnum.Interface => "interface",
        TypeTypeEnum.Struct => "struct",
        TypeTypeEnum.RecordStruct => "record struct",
        TypeTypeEnum.Class => "class",
        TypeTypeEnum.Record => "record class",
        _ => throw new ArgumentOutOfRangeException(nameof(typeTypeEnum), typeTypeEnum, null)
    };

    public static string GetAccessibility(this Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public => "public",
        Accessibility.Internal => "internal",
        Accessibility.Private => "private",
        Accessibility.Protected => "protected",
        Accessibility.ProtectedOrInternal => "protected internal",
        Accessibility.ProtectedAndInternal => "private protected",
        _ => string.Empty
    };

    public static string GetContainingTypeOpeners(this TypeRecord type)
    {
        if (type.ContainingType?.Type == null)
            return string.Empty;

        var types = new Stack<TypeRecord>();
        for (var container = type.ContainingType?.Type; container != null; container = container.ContainingType?.Type)
            types.Push(container);

        return string.Join("\n", types.Select(type => $"{type.GetTypeDeclaration()}\n{{"));
    }

    public static string GetContainingTypeClosers(this TypeRecord type)
    {
        if (type.ContainingType?.Type == null)
            return string.Empty;

        var closers = new List<string>();
        for (var container = type.ContainingType?.Type; container != null; container = container.ContainingType?.Type)
            closers.Add("}");
        return string.Join("\n", closers);
    }

    public static string GetTypeDeclaration(this TypeRecord type, string? baseType = null)
    {
        var parts = new List<string>();
        var accessibility = type.Accessibility.GetAccessibility();
        if (!string.IsNullOrEmpty(accessibility))
            parts.Add(accessibility);
        if (type.IsStatic)
            parts.Add("static");
        else
        {
            if (type.IsAbstract && !type.IsInterface)
                parts.Add("abstract");
            if (type.IsSealed && !type.IsValueType)
                parts.Add("sealed");
        }
        if (type.IsReadOnly)
            parts.Add("readonly");
        if (type.IsRefLike)
            parts.Add("ref");
        parts.Add("partial");

        var typeKind = type.TypeType.ToCodeFormat();
        var declaration = $"{string.Join(" ", parts)} {typeKind} {type.OriginName}{type.GenericArgs}";
        return baseType == null ? declaration : $"{declaration}: {baseType}";
    }

    public static string GetGeneratedTypeName(this TypeRecord type)
    {
        if (type.ContainingType?.Type == null)
            return type.OriginName + type.GenericArgsForFileName;

        var types = new Stack<string>();
        for (var container = type; container != null; container = container.ContainingType?.Type)
            types.Push(container.OriginName + container.GenericArgsForFileName);
        return string.Join(".", types);
    }

}
