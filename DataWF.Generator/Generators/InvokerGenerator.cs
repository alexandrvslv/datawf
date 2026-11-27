using DataWF.Generator.Records;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace DataWF.Generator;

internal class InvokerGenerator : PropertiesGenerator
{
    private const string GenericInvokerParam = "TTarget";
    private const string CInvoker = "Invoker";
    private const string CInstance = "Instance";
    private const string CIgnore = "Ignore";

    public static bool ForceInstance { get; set; }

    private static bool GetInstance(AttributeRecord? attributeData)
    {
        if (attributeData == null)
            return false;

        var instance = attributeData.GetNamedValue(CInstance);
        return instance.IsNull || instance.Value is true;
    }

    public static bool ValidateConstructor(IMethodSymbol ctor)
    {
        if (ctor.GetAttribute("ObsoleteAttribute") != null)
            return false;
        var attributeData = ctor.GetAttribute(Helper.cInvokerAttribute);
        var argIgnore = attributeData?.GetNamedValue(CIgnore) ?? default;
        var isIgnore = !argIgnore.IsNull && argIgnore.Value is true;
        return attributeData != null
            && !isIgnore
            || ctor.DeclaredAccessibility == Accessibility.Public;
    }

    public static bool ValidateField(IFieldSymbol field)
    {
        var attributeData = field.GetAttribute(Helper.cInvokerAttribute);
        var argIgnore = attributeData?.GetNamedValue(CIgnore) ?? default;
        var isIgnore = !argIgnore.IsNull && argIgnore.Value is true;
        return !isIgnore
            && !field.IsStatic
            && (field.DeclaredAccessibility == Accessibility.Public
            || attributeData != null);
    }

    public static bool ValidateField(FieldRecord field)
    {
        var attributeData = field.GetAttribute(Helper.cInvokerAttribute);
        var argIgnore = attributeData?.GetNamedValue(CIgnore) ?? default;
        var isIgnore = !argIgnore.IsNull && argIgnore.Value is true;
        return !isIgnore
            && !field.IsStatic
            && (field.IsPublic
            || attributeData != null);
    }

    public static bool ValidateMethod(IMethodSymbol method)
    {
        var attributeData = method.GetAttribute(Helper.cInvokerAttribute);
        var argIgnore = attributeData?.GetNamedValue(CIgnore) ?? default;
        var isIgnore = !argIgnore.IsNull && argIgnore.Value is true;
        return attributeData != null
            && !isIgnore
            && !method.IsStatic;
    }

    public static bool ValidateProperty(IPropertySymbol property)
    {
        var attributeData = property.GetAttribute(Helper.cInvokerAttribute);
        var argIgnore = attributeData?.GetNamedValue(CIgnore) ?? default;
        var isIgnore = !argIgnore.IsNull && argIgnore.Value is true;
        return !isIgnore
            && !property.IsStatic
            && (property.DeclaredAccessibility == Accessibility.Public
            || attributeData != null);
    }

    public static bool ValidateProperty(PropertyRecord property)
    {
        var attributeData = property.GetAttribute(Helper.cInvokerAttribute);
        var argIgnore = attributeData?.GetNamedValue(CIgnore) ?? default;
        var isIgnore = !argIgnore.IsNull && argIgnore.Value is true;
        return !isIgnore
            && !property.IsStatic
            && (property.IsPublic || attributeData != null);
    }

    public static bool ValidateNestedType(ITypeSymbol symbol)
    {
        return symbol.Name.EndsWith(CInvoker)
            && symbol.DeclaredAccessibility == Accessibility.Public;
    }

    protected Dictionary<string, InvokerInfo> existInvokers = new();
    protected Dictionary<string, InvokerInfo> newInvokers = new();
    protected Dictionary<string, CtorInfo> ctors = new();
    protected HashSet<string> usings = new(StringComparer.Ordinal)
    {
        "DataWF.Invokers",
        "DataWF.Comparers",
        "System.Collections",
        "System.ComponentModel",
        "System"
    };
    protected List<TypeInvokerInfo> typeInvokers = new();

    public InvokerGenerator(TypeRecordProvider provider, TypeRecord type)
        : base(provider, type)
    {
        IsInstance = ForceInstance || GetInstance(type.GetInvokerAttribute());
    }

    public InvokerGenerator(TypeRecordProvider provider, INamedTypeSymbol typeSymbol)
        : this(provider, GetOrCreateTypeRecord(provider, typeSymbol))
    { }

    private static TypeRecord GetOrCreateTypeRecord(TypeRecordProvider provider, INamedTypeSymbol typeSymbol)
    {
        return provider.GetOrCreate(
            typeSymbol: typeSymbol,
            forceRecreate: true,
            skipDefaults: true,
            takeBase: TakeBaseMode.Recursive
                | TakeBaseMode.Attribute | TakeBaseMode.AttributeInherited
                | TakeBaseMode.Property | TakeBaseMode.PropertyInherited
                | TakeBaseMode.Field | TakeBaseMode.FieldInherited
                | TakeBaseMode.Constructor | TakeBaseMode.ConstructorInherited
                | TakeBaseMode.Method | TakeBaseMode.MethodInherited
                | TakeBaseMode.NestedType | TakeBaseMode.NestedTypeInherited,
            validateConstructor: ValidateConstructor,
            validateProperty: ValidateProperty,
            validateField: ValidateField,
            validateMethod: ValidateMethod,
            validateAttribute: TypeRecord.AllAttributes,
            validateNestedType: ValidateNestedType
            )!.Type!;
    }

    public bool IsInstance { get; set; }

    public override string GetFileName() => GetInvokerFileName();

    public string GetInvokerFileName() => $"{Namespace}.{Type.GetGeneratedTypeName()}Invokers.g.cs";

    protected bool IsNestedType => Type.ContainingType?.Type != null;

    private void FillBaseInvokers(TypeRecord type)
    {
        typeInvokers.Clear();
        existInvokers.Clear();
        newInvokers.Clear();
        ctors.Clear();

        var baseType = type;
        while (baseType != null && baseType != TypeRecord.Object)
        {
            var toGenerate = baseType == type
                || baseType.GetInvokerAttribute() == null;
            typeInvokers.Add(new(
                Type: baseType,
                Invokers: GetInvokers(baseType, toGenerate),
                ToGenerate: toGenerate));
            if (baseType.IsInterface)
            {
                GetInterfaceInvokers(baseType);
                break;
            }
            else
            {
                baseType = baseType.BaseType;
            }
        }

        for (var i = 0; i < typeInvokers.Count; i++)
        {
            var typeInfo = typeInvokers[i];
            for (var j = i + 1; j < typeInvokers.Count; j++)
            {
                var baseTypeInfo = typeInvokers[j];
                if (!typeInfo.Type.IsInterface && baseTypeInfo.Type.IsInterface)
                    continue;
                var k = 0;
                foreach (var baseInvoker in baseTypeInfo.Invokers)
                {
                    if (!typeInfo.Invokers.Any(invoker => invoker.Name == baseInvoker.Name))
                        typeInfo.Invokers.Insert(k++, baseInvoker with { Inherit = true });
                }
            }
        }
    }

    private void GetInterfaceInvokers(TypeRecord iType)
    {
        var interfaceTypes = iType.Interfaces!.Select(i => i.Type);
        while (interfaceTypes.Any())
        {
            foreach (var interfaceType in interfaceTypes)
            {
                if (interfaceType == null)
                    continue;
                int index = -1;
                if ((index = typeInvokers.IndexOf(x => x.Type == interfaceType)) > -1)
                {
                    var temp = typeInvokers[index];
                    typeInvokers.RemoveAt(index);
                    typeInvokers.Add(temp);
                    continue;
                }
                var toGenerate = interfaceType.GetInvokerAttribute() == null;
                var invokers = GetInvokers(interfaceType, toGenerate);
                typeInvokers.Add(new(
                        Type: interfaceType,
                        Invokers: invokers,
                        ToGenerate: toGenerate));
            }
            interfaceTypes = interfaceTypes.SelectMany(t => t?.Interfaces?.Select(i => i.Type));
        }
    }

    private List<InvokerInfo> GetInvokers(TypeRecord type, bool toGenerate)
    {
        var isInstance = IsInstance || GetInstance(type.GetInvokerAttribute());
        var nestedInvoker = GetExistNestedInvokers(type);
        var list = new List<InvokerInfo>();
        foreach (var field in type.Fields ?? Enumerable.Empty<FieldRecord>())
        {
            var invokerName = field.Name + CInvoker;
            if (!ValidateField(field))
                continue;
            if (!nestedInvoker.TryGetValue(invokerName, out var invoker))
            {
                invoker = new InvokerInfo(invokerName, type, isInstance, field, !toGenerate);
            }
            list.Add(invoker);
            if (!toGenerate && !existInvokers.ContainsKey(invokerName))
            {
                existInvokers[invokerName] = invoker;
            }
        }

        foreach (var property in type.Properties ?? Enumerable.Empty<PropertyRecord>())
        {
            var invokerName = (property.Parameters?.Count > 0 ? "Item" : property.Name) + CInvoker;
            if (!ValidateProperty(property) || list.Any(x => x.Name == invokerName))
                continue;
            InvokerInfo invoker = default;
            if (existInvokers.TryGetValue(invokerName, out var exist))
            {
                invoker = exist with { DeclareType = type, Inherit = property.IsOverride };
            }
            else if (!nestedInvoker.TryGetValue(invokerName, out invoker))
            {
                invoker = new InvokerInfo(invokerName, type, isInstance, property, !toGenerate)
                {
                    Inherit = property.IsOverride
                };
            }
            list.Add(invoker);
            if (!toGenerate && !existInvokers.ContainsKey(invokerName))
            {
                existInvokers[invokerName] = invoker;
            }
        }

        return list;
    }

    private Dictionary<string, InvokerInfo> GetExistNestedInvokers(TypeRecord type)
    {
        var list = new Dictionary<string, InvokerInfo>(StringComparer.Ordinal);
        var nestedTypes = type.NestedTypes?.Select(x => x.Type!) ?? [];
        foreach (var nestedType in nestedTypes)
        {
            if (CheckNestedInvoker(type, nestedType) is { } invoker)
            {
                list[invoker.Name] = invoker;
                if (!existInvokers.ContainsKey(nestedType.OriginName))
                    existInvokers[nestedType.OriginName] = invoker;
            }
        }
        return list;
    }

    private static InvokerInfo? CheckNestedInvoker(TypeRecord type, TypeRecord nestedType)
    {
        var entryName = nestedType.OriginName.Substring(0, nestedType.OriginName.Length - CInvoker.Length);

        var hasInstance = nestedType.Properties.Any(p => p.Name == CInstance);
        InvokerInfo? invoker = Find(type, nestedType, entryName, hasInstance);
        var baseType = type.BaseType;
        while (invoker == null && baseType != null && baseType != TypeRecord.Object)
        {
            invoker = Find(baseType, nestedType, entryName, hasInstance);
            baseType = baseType.BaseType;
        }
        var interfaceTypes = type.Interfaces.Select(i => i.Type!);
        while (invoker == null && interfaceTypes != null && interfaceTypes.Any())
        {
            foreach (var interfaceType in interfaceTypes)
            {
                invoker = Find(interfaceType, nestedType, entryName, hasInstance);
                if (invoker != null)
                    break;
            }
            interfaceTypes = interfaceTypes.SelectMany(t => t.Interfaces?.Select(i => i.Type!));
        }

        return invoker;

        static InvokerInfo? Find(TypeRecord type, TypeRecord nestedType, string entryName, bool hasInstance)
        {
            if (entryName == "Item"
            && type.Properties.FirstOrDefault(p => p.Parameters?.Count > 0) is { } itemProperty)
            {
                return new InvokerInfo(nestedType.OriginName, type, hasInstance, itemProperty, true);
            }
            else if (type.Properties.FirstOrDefault(p => p.Name == entryName) is { } property)
            {
                return new InvokerInfo(nestedType.OriginName, type, hasInstance, property, true);
            }
            else if (type.Fields?.FirstOrDefault(f => f.Name == entryName) is { } field)
            {
                return new InvokerInfo(nestedType.OriginName, type, hasInstance, field, true);
            }

            return null;
        }
    }

    public override bool ProcessInternal(ref SourceProductionContext context)
    {
        var source = Generate();
        if (source != null)
        {
            context.AddSource(GetInvokerFileName(), source);
            return true;
        }

        return false;
    }

    public string? Generate()
    {
        Type.GetUsings(usings);
        var type = Type;
        if (TryGetToGenerate(out var toGenerateType) && toGenerateType != null)
        {
            type = toGenerateType;
            toGenerateType.GetUsings(usings);
        }

        usings.Remove(Type.Namespace);

        source.Clear();
        source.Append($$"""
            #nullable enable

            {{string.Join("\n", usings.OrderBy(u => u, StringComparer.Ordinal)
                .Select(u => $"using {u};"))}}

            namespace {{Namespace}};

            {{(IsNestedType ? Type.GetContainingTypeOpeners() + "\n" : string.Empty)}}{{Type.GetTypeDeclaration("IInvokerRegister")}}
            {
            """);

        FillBaseInvokers(type);
        foreach (var typeInvokers in typeInvokers)
        {
            if (typeInvokers.ToGenerate)
                GenerateType(typeInvokers);
        }

        if (newInvokers.Count == 0
            && ctors.Count == 0
            && existInvokers.Count == 0)
        {
            return null;
        }

        Register();
        source.Append($$"""

        }
        {{(IsNestedType ? "\n" + Type.GetContainingTypeClosers() : string.Empty)}}
        """);
        return source.ToString();
    }

    private bool TryGetToGenerate(out TypeRecord? toGenerateType)
    {
        toGenerateType = null;
        var attributeData = Type.GetInvokerAttribute();
        if (attributeData?.GetNamedValue("ToGenerate").Value as INamedTypeSymbol is { } toGenerateTypeSymbol)
        {
            if (toGenerateTypeSymbol.IsUnboundGenericType)
            {
                toGenerateTypeSymbol = toGenerateTypeSymbol.OriginalDefinition;
            }
            toGenerateType = GetOrCreateTypeRecord(provider, toGenerateTypeSymbol);
            if (toGenerateType != null)
            {
                FillBaseInvokers(toGenerateType);
                return true;
            }
        }
        return false;
    }

    private void GenerateType(TypeInvokerInfo typeInfo)
    {
        var type = typeInfo.Type;

        if (type.Constructors != null)
            foreach (var constructor in type.Constructors)
            {
                var ctorName = ProcessRuntimeConstructor(constructor, type);
                if (!ctors.ContainsKey(ctorName))
                    ctors.Add(ctorName, new CtorInfo(ctorName, type, IsInstance, constructor));
            }
        foreach (var invokerInfo in typeInfo.Invokers)
        {
            var invokerName = invokerInfo.Name;
            if (invokerInfo.Member == null
                || invokerInfo.Inherit
                || invokerInfo.Exist)
                continue;
            switch (invokerInfo.Member)
            {
                case FieldRecord field:
                    ProcessFieldInvoker(invokerName, field, type, invokerInfo.IsGeneric);
                    break;
                case PropertyRecord property:
                    if (property.Parameters?.Count > 0)
                        ProcessIndexPropertyInvoker(invokerName, property, type, invokerInfo.IsGeneric);
                    else
                        ProcessPropertyInvoker(invokerName, property, type, invokerInfo.IsGeneric);
                    break;
            }
            if (!newInvokers.ContainsKey(invokerName))
                newInvokers.Add(invokerName, invokerInfo);
        }
    }

    private string ProcessRuntimeConstructor(ConstructorRecord constructor, TypeRecord type)
    {
        var parametersTypes = string.Join(string.Empty, constructor.Parameters.Select(p => $"{p.Type?.GetOriginName()}"));
        var typeName = $"{type.OriginName}Constructor{parametersTypes}";
        var isObjectUnsupported = constructor.Parameters.Any(x => x.Type!.IsRefLike);

        var isUnsafe = constructor.Parameters.Any(x => x.Type!.IsPointer);
        var multiParam = constructor.Parameters.Count is > 0 and <= 8
            && !isUnsafe
            ? $", IRuntimeConstructor<{type}, {string.Join(", ", constructor.Parameters.Select(p => p.Type))}>"
            : string.Empty;
        source.Append($$"""

            public{{(isUnsafe ? " unsafe" : string.Empty)}} class {{typeName}}: RuntimeConstructor<{{type}}>{{multiParam}}
            {
        """);
        if (IsInstance)
        {
            source.Append($"""

                    public static readonly {typeName} Instance = new();
            """);
        }
        source.Append($$"""

                public override string Types => "{{parametersTypes}}";
                public override {{type}} Create(params object?[]? parameters)
        """);
        if (isObjectUnsupported)
            source.Append($$"""

                        => throw new ArgumentOutOfRangeException();
            """);
        else
            source.Append($$"""

                        => new {{type}}({{string.Join(",\n            ", constructor.Parameters.Select((p, i) => CastParameter(p, i)))}});
            """);
        if (constructor.Parameters.Count > 0)
        {
            source.Append($$"""

                    public override {{type}} Create()
                        => new {{type}}({{string.Join(", ", constructor.Parameters.Select((p, i) => $"default({p.Type})!"))}});
                    public {{type}} Create({{string.Join(", ", constructor.Parameters.Select(p => $"{p.Type} {p.Name}"))}})
                        => new {{type}}({{string.Join(", ", constructor.Parameters.Select(p => p.Name))}});
            """);
        }
        else
        {
            source.Append($$"""

                    public override {{type}} Create()
                        => new {{type}}();
            """);
        }

        source.Append($$"""

            }
        """);

        return typeName;
    }

    private static string CastParameter(ParameterRecord p, int i)
    {
        if (p.Type!.IsPointer)
            return $"(parameters?[{i}] is IntPtr {p.Name}) ? ({p.Type}){p.Name}.ToPointer() : default!";
        return $"(parameters?[{i}] is {p.Type!.Name.TrimEnd('?')} {p.Name}) ? {p.Name} : default!";
    }

    private void ProcessPropertyInvoker(string invokerName, PropertyRecord propertySymbol, TypeRecord type, bool isGeneric)
    {
        var propertyName = propertySymbol.Name;
        var isNew = existInvokers.ContainsKey(invokerName);

        var propertyType = propertySymbol.Type;
        var typeName = isGeneric ? GenericInvokerParam : type.ToString();

        //ProcessInvokerAttributes(propertySymbol);
        var invokerBaseType = "Invoker";
        var invokerFullName = $"{invokerName}{(isGeneric ? $"<{typeName}>" : string.Empty)}";
        source.Append($$"""

            public sealed{{(isNew ? " new" : string.Empty)}} class {{invokerFullName}}
                : {{invokerBaseType}}<{{typeName}}, {{propertyType}}>, IInvoker{{GetValueTypeInvoker(type, propertyType, typeName)}}
                {{(isGeneric ? $" where {typeName} : {type}" : string.Empty)}}
            {
        """);
        if (IsInstance)
            source.Append($"""

                    public static readonly {invokerName}{(isGeneric ? $"<{typeName}>" : string.Empty)} Instance = new();
            """);
        source.Append($$"""

                public override string Name => nameof({{type}}.{{propertyName}});
                public override bool CanWrite => {{(!propertySymbol.IsReadOnly ? "true" : "false")}};
                public override {{propertyType}} GetValue(scoped in {{typeName}} target) => target.{{propertyName}};
                public {{propertyType}} GetValue({{typeName}} target) => target.{{propertyName}};
                public override void SetValue(scoped ref {{typeName}} target, {{propertyType}} value){{(propertySymbol.IsReadOnly ? "{}"
                    : propertySymbol.IsInitOnly
                        ? $" => target = target with {{ {propertyName} = value }};"
                    : $" => target.{propertyName} = value;")}}
        """);
        GenerateComparer(type, "." + propertyName, propertyType, typeName, invokerFullName);
        source.Append($$"""

            }
        """);
    }

    private void ProcessIndexPropertyInvoker(string invokerName, PropertyRecord propertySymbol, TypeRecord type, bool isGeneric)
    {
        var propertyName = "Item";
        var isNew = existInvokers.ContainsKey(invokerName);

        var propertyType = propertySymbol.Type;
        var parameterType = propertySymbol.Parameters.First().Type!;
        var typeName = isGeneric ? GenericInvokerParam : type.ToString();
        var invokerBaseType = "IndexInvoker";
        var invokerFullName = $"{invokerName}{(isGeneric ? $"<{typeName}>" : string.Empty)}";
        source.Append($$"""

                public sealed{{(isNew ? " new" : string.Empty)}} class {{invokerFullName}}
                    : {{invokerBaseType}}<{{typeName}}, {{propertyType}}, {{parameterType}}>, IInvoker{{GetValueTypeIndexInvoker(type, propertyType, parameterType, typeName)}}
                    {{(isGeneric ? $" where {typeName} : {type}" : string.Empty)}}
                {
            """);
        if (IsInstance)
            source.Append($"""

                    public static readonly {invokerName}{(isGeneric ? $"<{typeName}>" : string.Empty)} Instance = new();
            """);
        source.Append($$"""    
                    
                    public override string Name => "{{propertyName}}";
                    public override bool CanWrite => {{(!propertySymbol.IsReadOnly ? "true" : "false")}};
                    public override {{propertyType}} GetValue(scoped in {{typeName}} target, {{parameterType}} index) => target[index];
                    public {{propertyType}} GetValue({{typeName}} target) => target[Index];
                    public override void SetValue(scoped ref {{typeName}} target, {{parameterType}} index, {{propertyType}} value){{(propertySymbol.IsReadOnly ? "{}" : $" => target[index] = value;")}}
            """);
        GenerateComparer(type, "[Invoker.Index]", propertyType, typeName, invokerFullName);
        source.Append($$"""

            }
        """);
    }

    private void ProcessFieldInvoker(string invokerName, FieldRecord fieldSymbol, TypeRecord type, bool isGeneric)
    {
        var fieldName = fieldSymbol.Name;
        var isNew = existInvokers.ContainsKey(invokerName);
        var fieldType = fieldSymbol.Type;
        var typeName = isGeneric ? GenericInvokerParam : type.ToString();
        var invokerBaseType = "Invoker";
        var invokerFullName = $"{invokerName}{(isGeneric ? $"<{typeName}>" : string.Empty)}";
        source.Append($$"""

                public sealed{{(isNew ? " new" : string.Empty)}} class {{invokerFullName}}
                    : {{invokerBaseType}}<{{typeName}}, {{fieldType}}>, IInvoker{{GetValueTypeInvoker(type, fieldType, typeName)}}
                    {{(isGeneric ? $" where {typeName} : {type}" : string.Empty)}}
                {
            """);
        if (IsInstance)
            source.Append($"""

                    public static readonly {invokerName}{(isGeneric ? $"<{typeName}>" : string.Empty)} Instance = new();
            """);
        source.Append($$"""

                    public override string Name => nameof({{type}}.{{fieldName}});
                    public override bool CanWrite => true;
                    public override {{fieldType}} GetValue(scoped in {{typeName}} target) => target.{{fieldName}};
                    public {{fieldType}} GetValue({{typeName}} target) => target.{{fieldName}};
                    public override void SetValue(scoped ref {{typeName}} target, {{fieldType}} value) => {{(fieldSymbol.IsInitOnly
                        ? $" => target = target with {{ {fieldName} = value }};"
                        : $"target.{fieldName} = value")}};
            """);
        GenerateComparer(type, "." + fieldName, fieldType, typeName, invokerFullName);
        source.Append($$"""

            }
        """);
    }

    private static string GetValueTypeInvoker(TypeRecord type, TypeRecord propertyType, string typeName)
    {
        return type.IsValueType && type.OriginName != "Nullable" ? $", IValueTypeInvoker<{typeName}, {propertyType}>" : string.Empty;
    }

    private static string GetValueTypeIndexInvoker(TypeRecord type, TypeRecord propertyType, TypeRecord parameterType, string typeName)
    {
        return type.IsValueType && type.OriginName != "Nullable" ? $", IValueTypeIndexInvoker<{typeName}, {propertyType}, {parameterType}>" : string.Empty;
    }

    private void GenerateComparer(TypeRecord type, string memberAccess, TypeRecord memberType, string typeName, string invokerFullName)
    {
        if (memberType.IsTypeParameter)
            return;
        var nonNullable = type.IsValueType && !type.IsNullable;
        source.Append($$"""

                public IInvokerComparer<{{typeName}}, {{memberType}}> CreateComparer(ListSortDirection direction = ListSortDirection.Ascending)
                        => new PropertyValueComparer(this, direction);
                IComparer IInvoker.CreateComparer(ListSortDirection direction)
                        => CreateComparer(direction);
                public IInvokerComparer<{{typeName}}, {{memberType}}> CreateComparer(UniversalComparer<{{memberType}}> comparer, ListSortDirection direction = ListSortDirection.Ascending)
                        => new PropertyValueComparer(this, comparer, direction);
                
                private sealed class PropertyValueComparer 
                    : InvokerComparer<{{typeName}}, {{memberType}}, {{invokerFullName}}, UniversalComparer<{{memberType}}>>
                {
                    public PropertyValueComparer({{invokerFullName}} invoker,
                        ListSortDirection direction = ListSortDirection.Ascending)
                        : this(invoker, UniversalComparer<{{memberType}}>.Default, direction)
                    { }

                    public PropertyValueComparer({{invokerFullName}} invoker,
                        UniversalComparer<{{memberType}}> comparer,
                        ListSortDirection direction = ListSortDirection.Ascending)
                        : base(invoker, comparer, direction)
                    { }
                    
                    public override int Compare({{typeName}}{{(nonNullable ? string.Empty : "?")}} x, {{typeName}}{{(nonNullable ? string.Empty : "?")}} y)
                    {
                        {{(nonNullable ? string.Empty
                            : type.IsValueType && type.IsNullable
                            ? """

                        if (!x.HasValue) return !y.HasValue ? 0 : multiplier * -1;
                        if (!y.HasValue) return multiplier;
        """
                            : """

                        if (ReferenceEquals(x, y)) return 0;
                        if (x == null) return multiplier * -1;
                        if (y == null) return multiplier;
        """)}}

                        int result = {{(memberType.IsValueType && (memberType.IsSystemType || memberType.OriginType?.IsSystemType is true)
                                ? memberType.IsNullable
                                    ? $"x{memberAccess}.GetValueOrDefault().CompareTo(y{memberAccess}.GetValueOrDefault())"
                                    : $"x{memberAccess}.CompareTo(y{memberAccess})"
                                : memberType == TypeRecord.String || memberType.OriginType == TypeRecord.String
                                    ? $"string.Compare(x{memberAccess}, y{memberAccess}, StringComparison.Ordinal)"
                                    : $"Comparer.Compare(x{memberAccess}, y{memberAccess})")}};
                        return multiplier * result;
                    }

                    public override bool Equals({{typeName}}{{(nonNullable ? string.Empty : "?")}} x, {{typeName}}{{(nonNullable ? string.Empty : "?")}} y)
                    {
                        {{(nonNullable ? string.Empty
                            : type.IsValueType && type.IsNullable
                            ? """

                        if (!x.HasValue) return !y.HasValue;
                        if (!y.HasValue) return false;
        """
                            : """

                        if (ReferenceEquals(x, y)) return true;
                        if (x == null || y == null) return false;
        """)}}

                        return {{(memberType.IsValueType && memberType.IsSystemType
                                ? $"x{memberAccess} == y{memberAccess}"
                                : memberType == TypeRecord.String || memberType.OriginType == TypeRecord.String
                                    ? $"string.Equals(x{memberAccess}, y{memberAccess}, StringComparison.Ordinal)"
                                    : $"Comparer.Equals(x{memberAccess}, y{memberAccess})")}};
                    }
                }
        """);
    }

    private void Register()
    {
        source.Append($$"""

            public {{(existInvokers.Count > 0 || Type.IsInterface ? "new" : string.Empty)}} static void RegisterInvokers()
            {
        """);
        foreach (var item in typeInvokers)
        {
            if (item.ToGenerate)
            {
                foreach (var invoker in item.Invokers)
                {
                    RegisterInvoker(invoker, item.Type);
                }
            }
        }

        foreach (var entry in ctors.Values)
        {
            source.Append($$"""

                    InvokeProvider.RegisterConstructor({{(entry.HasInstance ? $"{entry.Name}.Instance" : $"new {entry.Name}()")}});
            """);
        }
        source.Append($$"""

            }
        """);

        void RegisterInvoker(InvokerInfo invoker, TypeRecord type)
        {
            var invokerFullName = invoker.IsGeneric ? $"{invoker.Name}<{type}>" : invoker.Name;
            source.Append($$"""

                    InvokeProvider.RegisterInvoker<{{type}}>({{(invoker.HasInstance ? $"{invokerFullName}.Instance" : $"new {invokerFullName}()")}});
            """);
        }
    }
}


