using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DataWF.Generator.Records;

public record class TypeRecord(
    string OriginName,
    string Name,
    string Namespace,
    TypeRecordKeys Keys,
    Accessibility Accessibility = Accessibility.Public,
    TypeRecordReference? TypeRef = null,
    TypeRecord? OriginType = null,
    TypeRecordReference? ContainingType = null,
    EquatableList<TypeParameterRecord>? GenericParameters = null,
    EquatableList<TypeRecordReference>? Interfaces = null,
    EquatableList<ConstructorRecord>? Constructors = null,
    EquatableList<PropertyRecord>? Properties = null,
    EquatableList<FieldRecord>? Fields = null,
    EquatableList<MethodRecord>? Methods = null,
    EquatableList<AttributeRecord>? Attributes = null,
    EquatableList<TypeRecordReference>? NestedTypes = null)
    : AttributedRecord(
        Name: Name,
        TypeRef: TypeRef,
        Attributes: Attributes)
{
    private static TypeRecordKeys GetKeys(Type type)
    {
        TypeRecordKeys keys = TypeRecordKeys.None;
        if (type.IsSealed) keys |= TypeRecordKeys.Sealed;
        if (type.IsAbstract) keys |= TypeRecordKeys.Abstract;
        if (type.IsGenericType) keys |= TypeRecordKeys.GenericType;
        if (type.IsValueType) keys |= TypeRecordKeys.ValueType;
        if (type.IsEnum) keys |= TypeRecordKeys.Enum;
        if (type.IsInterface) keys |= TypeRecordKeys.Interface;
        if (type.IsUnmanagedCustom()) keys |= TypeRecordKeys.Unmanaged;
        if (type.GetProperty("EqualityContract",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) != null) keys |= TypeRecordKeys.Record;
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type)) keys |= TypeRecordKeys.Enumerable;
        if (type.IsValueType
            && type.GetCustomAttributes(true).Any(attr => attr.GetType().Name == "IsByRefLikeAttribute")) keys |= TypeRecordKeys.RefStruct;
        if (SystemTypes.ContainsKey(type.Name)) keys |= TypeRecordKeys.System;
        if (type.IsAbstract && type.IsSealed) keys |= TypeRecordKeys.Static;
        if (type.IsValueType
            && type.GetCustomAttributes(true).Any(attr => attr.GetType().Name == "IsReadOnlyAttribute")) keys |= TypeRecordKeys.Static;
        return keys;
    }

    private static TypeRecordKeys GetKeys(ITypeSymbol typeSymbol)
    {
        TypeRecordKeys keys = TypeRecordKeys.None;
        var namedType = typeSymbol as INamedTypeSymbol;
        if (typeSymbol.IsSealed) keys |= TypeRecordKeys.Sealed;
        if (typeSymbol.IsAbstract) keys |= TypeRecordKeys.Abstract;
        if (typeSymbol.IsValueType) keys |= TypeRecordKeys.ValueType;
        if (typeSymbol.IsRecord) keys |= TypeRecordKeys.Record;
        if (typeSymbol.TypeKind == TypeKind.Enum) keys |= TypeRecordKeys.Enum;
        if (typeSymbol.TypeKind == TypeKind.Interface) keys |= TypeRecordKeys.Interface;
        if (namedType?.IsUnmanagedType is true) keys |= TypeRecordKeys.Unmanaged;
        if (typeSymbol.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)) keys |= TypeRecordKeys.Enumerable;
        if (namedType?.IsGenericType is true) keys |= TypeRecordKeys.GenericType;
        if (namedType?.IsRefLikeType is true) keys |= TypeRecordKeys.RefStruct;
        if (SystemTypes.ContainsKey(typeSymbol.Name)) keys |= TypeRecordKeys.System;
        if (typeSymbol.IsStatic) keys |= TypeRecordKeys.Static;
        if (typeSymbol.IsReadOnly) keys |= TypeRecordKeys.ReadOnly;
        if (typeSymbol.TypeKind == TypeKind.TypeParameter) keys |= TypeRecordKeys.TypeParameter;
        return keys;
    }

    public static Func<IMethodSymbol, bool> PublicConstructors = static (p)
        => p.DeclaredAccessibility == Accessibility.Public;
    public static Func<IPropertySymbol, bool> AllProperties = static (p)
        => true;
    public static Func<IPropertySymbol, bool> PublicProperties = static (p)
        => p.DeclaredAccessibility == Accessibility.Public;
    public static Func<IFieldSymbol, bool> AllFields = static (p)
        => true;
    public static Func<IFieldSymbol, bool> PublicFields = static (p)
        => p.DeclaredAccessibility == Accessibility.Public;
    public static Func<IMethodSymbol, bool> AllMethods = static (p)
        => !(p.MethodKind is MethodKind.UserDefinedOperator
            or MethodKind.Conversion);
    public static Func<IMethodSymbol, bool> PublicMethods = static (p)
        => p.DeclaredAccessibility == Accessibility.Public
            && !(p.MethodKind is MethodKind.UserDefinedOperator
                or MethodKind.Conversion);
    public static Func<AttributeData, bool> AllAttributes = static (p)
        => true;
    public static Func<ITypeSymbol, bool> AllNestedTypes = static (p)
        => true;
    public static Func<ITypeSymbol, bool> PublicNestedTypes = static (p)
        => p.DeclaredAccessibility == Accessibility.Public;
    private string? genericArgs;

    public TypeRecord(string name,
        ITypeSymbol typeSymbol,
        TypeRecordProvider provider,
        TakeBaseMode takeBase = TakeBaseMode.Ignore,
        Func<IMethodSymbol, bool>? validateConstructor = null,
        Func<IPropertySymbol, bool>? validateProperty = null,
        Func<IFieldSymbol, bool>? validateField = null,
        Func<IMethodSymbol, bool>? validateMethod = null,
        Func<AttributeData, bool>? validateAttribute = null,
        Func<ITypeSymbol, bool>? validateNestedType = null)
        : this(
            OriginName: typeSymbol.Name,
            Name: name,
            Namespace: typeSymbol.ContainingNamespace.ToDisplayString(Helper.NamespaceDisplayFormat),
            Keys: GetKeys(typeSymbol),
            Accessibility: typeSymbol.DeclaredAccessibility,
            GenericParameters: typeSymbol is INamedTypeSymbol namedType2 && namedType2.TypeArguments.Any()
                ? namedType2.TypeArguments.Select(x => new TypeParameterRecord(x, provider)).ToEquatableList()
                : null,
            Interfaces: typeSymbol.Interfaces.Select(i => provider.GetOrCreate(i,
                    takeBase: TakeBaseMode.Recursive,
                    validateProperty: takeBase.HasFlag(TakeBaseMode.Property)
                            ? takeBase.HasFlag(TakeBaseMode.PropertyInherited)
                                ? validateProperty
                                : PublicProperties
                            : null,
                    validateField: null,
                    validateMethod: takeBase.HasFlag(TakeBaseMode.Method)
                            ? takeBase.HasFlag(TakeBaseMode.MethodInherited)
                                ? validateMethod
                                : PublicMethods
                            : null,
                    validateAttribute: takeBase.HasFlag(TakeBaseMode.Attribute)
                            ? takeBase.HasFlag(TakeBaseMode.AttributeInherited)
                                ? validateAttribute
                                : AllAttributes
                            : null,
                    validateNestedType: null)!).ToEquatableList(),
            ContainingType: provider.GetOrCreate(typeSymbol.ContainingType),
            TypeRef: (takeBase.HasFlag(TakeBaseMode.Take) || takeBase.HasFlag(TakeBaseMode.Recursive))
                    ? provider.GetOrCreate(typeSymbol.BaseType,
                        takeBase: takeBase.HasFlag(TakeBaseMode.Recursive) ? takeBase : TakeBaseMode.Ignore,
                        validateConstructor: takeBase.HasFlag(TakeBaseMode.Constructor)
                            ? takeBase.HasFlag(TakeBaseMode.ConstructorInherited)
                                ? validateConstructor
                                : PublicConstructors
                            : null,
                        validateProperty: takeBase.HasFlag(TakeBaseMode.Property)
                            ? takeBase.HasFlag(TakeBaseMode.PropertyInherited)
                                ? validateProperty
                                : PublicProperties
                            : null,
                        validateField: takeBase.HasFlag(TakeBaseMode.Field)
                            ? takeBase.HasFlag(TakeBaseMode.FieldInherited)
                                ? validateField
                                : PublicFields
                            : null,
                        validateMethod: takeBase.HasFlag(TakeBaseMode.Method)
                            ? takeBase.HasFlag(TakeBaseMode.MethodInherited)
                                ? validateMethod
                                : PublicMethods
                            : null,
                        validateAttribute: takeBase.HasFlag(TakeBaseMode.Attribute)
                            ? takeBase.HasFlag(TakeBaseMode.AttributeInherited)
                                ? validateAttribute
                                : AllAttributes
                            : null,
                        validateNestedType: takeBase.HasFlag(TakeBaseMode.NestedType)
                            ? takeBase.HasFlag(TakeBaseMode.NestedTypeInherited)
                                ? validateNestedType
                                : PublicNestedTypes
                            : null)
                    : null,
            Constructors: validateConstructor == null ? null : typeSymbol is INamedTypeSymbol namedType3
                ? namedType3.InstanceConstructors
                    .Where(validateConstructor.Invoke)
                    .Select(x => new ConstructorRecord(x, provider))
                    .ToEquatableList()
                : null,
            Properties: validateProperty == null ? null : typeSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(validateProperty.Invoke)
                .Select(x => new PropertyRecord(x, provider))
                .ToEquatableList(),
            Fields: validateField == null ? null : typeSymbol.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(validateField.Invoke)
                .Select(x => new FieldRecord(x, provider))
                .ToEquatableList(),
            Methods: validateMethod == null ? null : typeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(validateMethod.Invoke)
                .Select(x => new MethodRecord(x, provider))
                .ToEquatableList(),
            Attributes: validateAttribute == null ? null : typeSymbol.GetAttributes()
                .Where(validateAttribute.Invoke)
                .Select(x => new AttributeRecord(x, provider))
                .ToEquatableList(),
            NestedTypes: validateNestedType == null ? null : typeSymbol.GetMembers()
                .OfType<INamedTypeSymbol>()
                .Where(validateNestedType.Invoke)
                .Select(x => provider.GetOrCreate(
                    typeSymbol: x,
                    takeBase: takeBase.HasFlag(TakeBaseMode.Recursive) ? takeBase : TakeBaseMode.Take,
                    validateProperty: takeBase.HasFlag(TakeBaseMode.Property)
                        ? takeBase.HasFlag(TakeBaseMode.PropertyInherited)
                            ? validateProperty
                            : AllProperties
                        : null,
                    validateField: takeBase.HasFlag(TakeBaseMode.Field)
                        ? takeBase.HasFlag(TakeBaseMode.FieldInherited)
                            ? validateField
                            : AllFields
                        : null,
                    validateMethod: takeBase.HasFlag(TakeBaseMode.Method)
                        ? takeBase.HasFlag(TakeBaseMode.MethodInherited)
                            ? validateMethod
                            : AllMethods
                        : null,
                    validateAttribute: takeBase.HasFlag(TakeBaseMode.Attribute)
                        ? takeBase.HasFlag(TakeBaseMode.AttributeInherited)
                            ? validateAttribute
                            : AllAttributes
                        : null,
                    validateNestedType: takeBase.HasFlag(TakeBaseMode.NestedType)
                            ? takeBase.HasFlag(TakeBaseMode.NestedTypeInherited)
                                ? validateNestedType
                                : AllNestedTypes
                            : null))
                .OfType<TypeRecordReference>()
                .ToEquatableList()
        )
    { }

    public TypeRecord(
        Type type,
        TypeRecordProvider provider)
        : this(
            OriginName: type.Name,
            Name: type.Name,
            Namespace: type.Namespace ?? string.Empty,
            Keys: GetKeys(type),
            GenericParameters: type.IsGenericType
                ? type.GetGenericArguments().Select(x => new TypeParameterRecord(x, provider)).ToEquatableList()
                : null,
            TypeRef: type.BaseType != null && type.BaseType != typeof(object) ? provider.GetOrCreate(type.BaseType) : null,
            Constructors: type.GetConstructors().Select(x => new ConstructorRecord(x, provider)).ToEquatableList(),
            Properties: type.GetProperties().Select(x => new PropertyRecord(x, provider)).ToEquatableList(),
            Fields: type.GetFields().Select(x => new FieldRecord(x, provider)).ToEquatableList(),
            Methods: type.GetMethods().Select(x => new MethodRecord(x, provider)).ToEquatableList(),
            Attributes: type.GetCustomAttributesData().Select(x => new AttributeRecord(x, provider)).ToEquatableList(),
            NestedTypes: type.GetNestedTypes().Select(x => provider.GetOrCreate(x)).ToEquatableList()
        )
    { }

    public bool IsSealed => Keys.HasFlag(TypeRecordKeys.Sealed);
    public bool IsAbstract => Keys.HasFlag(TypeRecordKeys.Abstract);
    public bool IsGenericType => Keys.HasFlag(TypeRecordKeys.GenericType);
    public bool IsValueType => Keys.HasFlag(TypeRecordKeys.ValueType);
    public bool IsInterface => Keys.HasFlag(TypeRecordKeys.Interface);
    public bool IsRecord => Keys.HasFlag(TypeRecordKeys.Record);
    public bool IsEnum => Keys.HasFlag(TypeRecordKeys.Enum);
    public bool IsEnumerable => Keys.HasFlag(TypeRecordKeys.Enumerable);
    public bool IsNullable => Keys.HasFlag(TypeRecordKeys.Nullable);
    public bool IsArray => Keys.HasFlag(TypeRecordKeys.Array);
    public bool IsPointer => Keys.HasFlag(TypeRecordKeys.Pointer);
    public bool IsUnmanaged => Keys.HasFlag(TypeRecordKeys.Unmanaged);
    public bool IsRefLike => Keys.HasFlag(TypeRecordKeys.RefStruct);
    public bool IsSystemType => Keys.HasFlag(TypeRecordKeys.System);
    public bool IsStatic => Keys.HasFlag(TypeRecordKeys.Static);
    public bool IsReadOnly => Keys.HasFlag(TypeRecordKeys.ReadOnly);
    public bool IsTypeParameter => Keys.HasFlag(TypeRecordKeys.TypeParameter);

    public TypeRecord? BaseType => TypeRef?.Type;

    public int Arity => GenericParameters != null ? GenericParameters.Count : 0;

    public string GenericArgs => genericArgs ??= GenericParameters != null
        ? $"<{string.Join(", ", GenericParameters.Select(x => x.Name))}>" // x.Type?.ToString() ?? 
        : string.Empty;
    public string GenericArgsForFileName => Arity > 0
        ? $"_{Arity}"
        : string.Empty;

    public TypeTypeEnum TypeType
        => IsInterface ? TypeTypeEnum.Interface
            : IsValueType ? IsRecord ? TypeTypeEnum.RecordStruct : TypeTypeEnum.Struct
            : IsRecord ? TypeTypeEnum.Record : TypeTypeEnum.Class;

    public bool IsGenericInvoker
        => !IsSealed && (IsInterface || !IsValueType);

    public static readonly TypeRecord SystemType = new("Type", "Type", "System", TypeRecordKeys.System);
    public static readonly TypeRecord Object = new("Object", "object", string.Empty, TypeRecordKeys.System);
    public static readonly TypeRecord String = new("String", "string", string.Empty, TypeRecordKeys.System);
    public static readonly TypeRecord Void = new(string.Empty, "void", string.Empty, TypeRecordKeys.System);
    public static readonly TypeRecord Boolean = new("Boolean", "bool", string.Empty, TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord Char = new("Char", "char", string.Empty, TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord SByte = new("SByte", "sbyte", string.Empty, TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord Byte = new("Byte", "byte", string.Empty, TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord Int16 = new("Int16", "short", string.Empty, TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord UInt16 = new("UInt16", "ushort", string.Empty, TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord Int32 = new("Int32", "int", string.Empty, TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord UInt32 = new("UInt32", "uint", string.Empty, TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord Int64 = new("Int64", "long", string.Empty, TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord UInt64 = new("UInt64", "ulong", string.Empty, TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord Int128 = new("Int128", "Int128", "System", TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord UInt128 = new("UInt128", "UInt128", "System", TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord Half = new("Half", "Half", "System", TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord Single = new("Single", "float", string.Empty, TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord Double = new("Double", "double", string.Empty, TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord Decimal = new("Decimal", "decimal", string.Empty, TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord DateTime = new("DateTime", "DateTime", "System", TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord DateTimeOffset = new("DateTimeOffset", "DateTimeOffset", "System", TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord DateOnly = new("DateOnly", "DateOnly", "System", TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord TimeOnly = new("TimeOnly", "TimeOnly", "System", TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord TimeSpan = new("TimeSpan", "TimeSpan", "System", TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static readonly TypeRecord Guid = new("Guid", "Guid", "System", TypeRecordKeys.ValueType | TypeRecordKeys.Unmanaged | TypeRecordKeys.System);
    public static Dictionary<string, TypeRecord> SystemTypes { get; } = new(StringComparer.Ordinal)
    {
        //{ nameof(Void), Void },
        //{ nameof(Object), Object },
        { nameof(Boolean), Boolean },
        { nameof(Char), Char },
        { nameof(SByte), SByte },
        { nameof(Byte), Byte },
        { nameof(Int16), Int16 },
        { nameof(UInt16), UInt16 },
        { nameof(Int32), Int32 },
        { nameof(UInt32), UInt32 },
        { nameof(Int64), Int64 },
        { nameof(UInt64), UInt64 },
        { nameof(Int128), Int128 },
        { nameof(UInt128), UInt128 },
        { nameof(Half), Half },
        { nameof(Single), Single },
        { nameof(Double), Double },
        { nameof(Decimal), Decimal },
        { nameof(String), String },
        { nameof(DateTime), DateTime },
        { nameof(DateTimeOffset), DateTimeOffset },
        { nameof(DateOnly), DateOnly },
        { nameof(TimeOnly), TimeOnly },
        { nameof(TimeSpan), TimeSpan },
        { nameof(Guid), Guid },
        { nameof(Type), SystemType },
    };

    public string GetOriginName() => IsArray ? OriginName + "Array"
        : IsPointer ? OriginName + "Pointer"
        : OriginName;

    public override string ToString() => Name;

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Name);
        hashCode.Add(Namespace);
        hashCode.Add(Keys);
        hashCode.Add(TypeRef);
        hashCode.Add(GenericParameters);
        hashCode.Add(Constructors);
        hashCode.Add(Properties);
        hashCode.Add(Fields);
        hashCode.Add(Methods);
        hashCode.Add(Attributes);
        hashCode.Add(NestedTypes);
        hashCode.Add(Interfaces);
        return hashCode.ToHashCode();
    }

    public virtual bool Equals(TypeRecord? other) => other != null
        && string.Equals(Name, other.Name, StringComparison.Ordinal)
        && string.Equals(Namespace, other.Namespace, StringComparison.Ordinal)
        && Keys == other.Keys
        && TypeRef == other.TypeRef
        && GenericParameters == other.GenericParameters
        && Constructors == other.Constructors
        && Properties == other.Properties
        && Fields == other.Fields
        && Methods == other.Methods
        && Attributes == other.Attributes
        && NestedTypes == other.NestedTypes
        && Interfaces == other.Interfaces;

}

public enum TypeTypeEnum
{
    Interface,
    Struct,
    RecordStruct,
    Class,
    Record
}
