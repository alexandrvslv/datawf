using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DataWF.Generator.Records;

public class TypeRecordProvider
{
    ConcurrentDictionary<string, TypeRecordReference> types = new();

    public void Clean()
    {
        types.Clear();
    }

    public TypeRecordReference? GetOrCreate(ITypeSymbol? typeSymbol,
        TakeBaseMode takeBase = TakeBaseMode.Ignore,
        Func<IMethodSymbol, bool>? validateConstructor = null,
        Func<IPropertySymbol, bool>? validateProperty = null,
        Func<IFieldSymbol, bool>? validateField = null,
        Func<IMethodSymbol, bool>? validateMethod = null,
        Func<AttributeData, bool>? validateAttribute = null,
        Func<ITypeSymbol, bool>? validateNestedType = null,
        bool forceRecreate = false,
        bool skipDefaults = false)
    {
        if (typeSymbol == null
            || typeSymbol.SpecialType == SpecialType.System_ValueType)
            return null;

        var isNullable = typeSymbol.IsTypeNullable(out var norigin);
        var isArray = norigin.IsTypeArray(out var aorigin);
        var isPointer = aorigin.IsPointer(out var porigin);
        var name = porigin.GetDisplayName();
        if (!types.TryGetValue(name, out var reference))
        {
            types[name] = reference = new TypeRecordReference(name);
            reference.CreateArgs = new CreateArgs(this,
                name,
                porigin,
                takeBase,
                validateConstructor,
                validateProperty,
                validateField,
                validateMethod,
                validateAttribute,
                validateNestedType,
                skipDefaults);
        }
        else if (forceRecreate)
        {
            reference.Type = Create(new CreateArgs(this,
                name,
                porigin,
                takeBase,
                validateConstructor,
                validateProperty,
                validateField,
                validateMethod,
                validateAttribute,
                validateNestedType,
                skipDefaults));
        }

        if (isPointer)
        {
            var originRef = reference;
            name = $"{name}*";
            if (!types.TryGetValue(name, out reference))
            {
                types[name] = reference = new TypeRecordReference(name)
                {
                    Name = name,
                    Origin = originRef,
                    TypeKeys = TypeRecordKeys.Pointer
                };
            }
        }
        if (isArray)
        {
            var originRef = reference;
            name = $"{name}[]";
            if (!types.TryGetValue(name, out reference))
            {
                types[name] = reference = new TypeRecordReference(name)
                {
                    Name = name,
                    Origin = originRef,
                    TypeKeys = TypeRecordKeys.Array
                        | (isPointer ? TypeRecordKeys.Pointer : TypeRecordKeys.None)
                };
            }
        }
        if (isNullable)
        {
            var originRef = reference;
            name = $"{name}?";
            if (!types.TryGetValue(name, out reference))
            {
                types[name] = reference = new TypeRecordReference(name)
                {
                    Name = name,
                    Origin = originRef,
                    TypeKeys = TypeRecordKeys.Nullable
                        | (isArray ? TypeRecordKeys.Array : TypeRecordKeys.None)
                };
            }
        }
        return reference;
    }

    internal TypeRecord Create(CreateArgs args)
    {
        if (args.skipDefaults)
            return Create(args);
        return args.typeSymbol.SpecialType switch
        {
            SpecialType.System_Object => TypeRecord.Object,
            SpecialType.System_Void => TypeRecord.Void,
            SpecialType.System_Boolean => TypeRecord.Boolean,
            SpecialType.System_Char => TypeRecord.Char,
            SpecialType.System_SByte => TypeRecord.SByte,
            SpecialType.System_Byte => TypeRecord.Byte,
            SpecialType.System_Int16 => TypeRecord.Int16,
            SpecialType.System_UInt16 => TypeRecord.UInt16,
            SpecialType.System_Int32 => TypeRecord.Int32,
            SpecialType.System_UInt32 => TypeRecord.UInt32,
            SpecialType.System_Int64 => TypeRecord.Int64,
            SpecialType.System_UInt64 => TypeRecord.UInt64,
            SpecialType.System_Single => TypeRecord.Single,
            SpecialType.System_Double => TypeRecord.Double,
            SpecialType.System_Decimal => TypeRecord.Decimal,
            SpecialType.System_String => TypeRecord.String,
            SpecialType.System_DateTime => TypeRecord.DateTime,
            _ => TypeRecord.SystemTypes.TryGetValue(args.name, out var systemType) ? systemType
                : Create(args)
        };

        TypeRecord Create(CreateArgs args)
        {
            return new TypeRecord(
                name: args.name,
                typeSymbol: args.typeSymbol,
                provider: this,
                takeBase: args.takeBase,
                validateConstructor: args.validateConstructor,
                validateProperty: args.validateProperty,
                validateField: args.validateField,
                validateMethod: args.validateMethod,
                validateAttribute: args.validateAttribute,
                validateNestedType: args.validateNestedType
            );
        }
    }

    public TypeRecordReference GetOrCreate(Type type)
    {
        if (!types.TryGetValue(type.FullName!, out var reference))
        {
            types[type.FullName!] = reference = new TypeRecordReference(type.FullName!);
            var record = new TypeRecord(type, this);
            reference.Type = record;
        }
        return reference;
    }
}

public record struct CreateArgs(TypeRecordProvider provider,
        string name,
        ITypeSymbol typeSymbol,
        TakeBaseMode takeBase,
        Func<IMethodSymbol, bool>? validateConstructor,
        Func<IPropertySymbol, bool>? validateProperty,
        Func<IFieldSymbol, bool>? validateField,
        Func<IMethodSymbol, bool>? validateMethod,
        Func<AttributeData, bool>? validateAttribute,
        Func<ITypeSymbol, bool>? validateNestedType,
        bool skipDefaults)
{
    public TypeRecord Create() => provider.Create(this);
}