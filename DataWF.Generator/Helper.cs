using DataWF.Generator.Records;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DataWF.Generator;

public static class Helper
{
    public static readonly string cCodeKey = "CodeKey";
    public static readonly string cFileNameKey = "FileNameKey";
    public static readonly string cFileLastWriteKey = "FileLastWriteKey";
    public static readonly string cIWebProvider = "IWebProvider";
    public static readonly string cModelProvider = "ModelProvider";
    public static readonly string cIModelProvider = "IModelProvider";
    public static readonly string cModelSchema = "ModelSchema";
    public static readonly string cModelLogSchema = "ModelLogSchema";
    public static readonly string cIModelSchema = "IModelSchema";
    public static readonly string cIModelLogSchema = "IModelLogSchema";
    public static readonly string cModelTable = "ModelTable";
    public static readonly string cIModelTable = "IModelTable";
    public static readonly string cModelLogTable = "ModelTableLog";
    public static readonly string cIModelLogTable = "IModelTableLog";
    public static readonly string cDBItem = "DBItem";
    public static readonly string cLogItem = "LogItem";
    public static readonly string cDBGroupItem = "DBGroupItem";
    public static readonly string cDBTransaction = "DBTransaction";
    public static readonly string cType = "Type";
    public static readonly string cObject = "Object";
    public static readonly string cSystem = "System";
    public static readonly string cStream = "Stream";
    public static readonly string cString = "string";
    public static readonly string cLog = "Log";
    public static readonly string cKeys = "Keys";
    public static readonly string cSchema = "Schema";
    public static readonly string cSchemaAttribute = "SchemaAttribute";
    public static readonly string cTable = "Table";
    public static readonly string cTableAttribute = "TableAttribute";
    public static readonly string cLogTable = "LogTable";
    public static readonly string cLogTableAttribute = "LogTableAttribute";
    public static readonly string cAbstractTable = "AbstractTable";
    public static readonly string cAbstractTableAttribute = "AbstractTableAttribute";
    public static readonly string cVirtualTable = "VirtualTable";
    public static readonly string cVirtualTableAttribute = "VirtualTableAttribute";
    public static readonly string cColumn = "Column";
    public static readonly string cColumnAttribute = "ColumnAttribute";
    public static readonly string cReference = "Reference";
    public static readonly string cReferenceAttribute = "ReferenceAttribute";
    public static readonly string cInvoker = "Invoker";
    public static readonly string cIInvoker = "IInvoker";
    public static readonly string cIInvokerRegister = "IInvokerRegister";
    public static readonly string cInvokerAttribute = "InvokerAttribute";
    public const string cElementSerializerAttribute = "ElementSerializerAttribute";
    public const string cElementSerializer = "ElementSerializer";
    public static readonly string cWebSchema = "WebSchema";
    public static readonly string cWebSchemaAttribute = "WebSchemaAttribute";
    public static readonly string cSchemaController = "SchemaController";
    public static readonly string cSchemaControllerAttribute = "SchemaControllerAttribute";
    public static readonly string cISerializerRegister = "ISerializerRegister";
    public static readonly string cProvider = "Provider";
    public static readonly string cProviderAttribute = "ProviderAttribute";
    public static readonly string cGet = "Get";
    public static readonly string cPost = "Post";
    public static readonly string cPut = "Put";
    public static readonly string cDelete = "Delete";

    public static SymbolDisplayFormat NamespaceDisplayFormat = SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);
    public static SymbolDisplayFormat NameDisplayFormat = SymbolDisplayFormat.MinimallyQualifiedFormat;


    public static readonly DiagnosticDescriptor DDCommonLibrary = new DiagnosticDescriptor("DWFG001",
        "DataWF references not found",
        "DataWF references not found",
        "Warning",
        DiagnosticSeverity.Warning,
        true);

    public static readonly DiagnosticDescriptor DDFailGeneration = new DiagnosticDescriptor("DWFG002",
        "Couldn't Generate",
        "Couldn't Generate {0} {1} {2} {3}",
        "Warning",
        DiagnosticSeverity.Warning,
        true);

    public static readonly DiagnosticDescriptor DDSuccessGeneration = new DiagnosticDescriptor("DWFG003",
        "Success Generate",
        "Success Generate {0} {1}",
        "Info",
        DiagnosticSeverity.Info,
        true);

    private static readonly Dictionary<Assembly, Dictionary<string, Type>> cacheAssemblyTypes = new();

    public static void LaunchDebugger()
    {
        if (!System.Diagnostics.Debugger.IsAttached)
            System.Diagnostics.Debugger.Launch();
    }

    public static IEnumerable<ReadOnlyMemory<char>> SpanSplit(this string value, char splitter)
    {
        var word = ReadOnlyMemory<char>.Empty;
        var startIndex = 0;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == splitter)
            {
                if (word.Length > 0)
                    yield return word;
                startIndex = i + 1;
            }
            word = value.AsMemory(startIndex, (i - startIndex) + 1);
        }
        if (word.Length > 0)
            yield return word;
    }

    public static string ToLowerCap(this string str)
    {
        return string.Concat(Char.ToLowerInvariant(str[0]).ToString(), str.Substring(1));
    }

    //https://stackoverflow.com/a/24768641
    public static string ToInitcap(this string str, params char[] separator)
    {
        if (string.IsNullOrEmpty(str))
            return str;
        if (string.Equals(str, cGet, StringComparison.OrdinalIgnoreCase))
            return cGet;
        if (string.Equals(str, cPost, StringComparison.OrdinalIgnoreCase))
            return cPost;
        if (string.Equals(str, cPut, StringComparison.OrdinalIgnoreCase))
            return cPut;
        if (string.Equals(str, cDelete, StringComparison.OrdinalIgnoreCase))
            return cDelete;
        var charArray = new List<char>(str.Length);
        bool newWord = true;
        foreach (Char currentChar in str)
        {
            var newChar = currentChar;
            if (Char.IsLetter(currentChar))
            {
                if (newWord)
                {
                    newWord = false;
                    newChar = Char.ToUpper(currentChar);
                }
                else
                {
                    newChar = Char.ToLower(currentChar);
                }
            }
            else if (separator.Contains(currentChar))
            {
                newWord = true;
                continue;
            }
            charArray.Add(newChar);
        }
        return new string(charArray.ToArray());
    }

    public static Type? ParseType(string value, Assembly assembly)
    {
        var byName = value.IndexOf('.') < 0;
        if (byName)
        {
            if (!cacheAssemblyTypes.TryGetValue(assembly, out var cache))
            {
                var definedTypes = assembly.GetExportedTypes();
                cacheAssemblyTypes[assembly] =
                    cache = new Dictionary<string, Type>(definedTypes.Length, StringComparer.Ordinal);
                foreach (var defined in definedTypes)
                {
                    cache[defined.Name] = defined;
                }
            }

            return cache.TryGetValue(value, out var type) ? type : null;
        }
        else
        {
            return assembly.GetType(value);
        }
    }

    public static bool IsPointer(this ITypeSymbol typeSymbol, out ITypeSymbol origin)
    {
        if (typeSymbol.TypeKind == TypeKind.Pointer && typeSymbol is IPointerTypeSymbol pointerSymbol)
        {
            // Get the type the pointer points to (e.g., int for int*)
            origin = pointerSymbol.PointedAtType;
            return true;
        }

        // if (typeSymbol.TypeKind == TypeKind.FunctionPointer && typeSymbol is IFunctionPointerTypeSymbol fPointerSymbol)
        // {
        //     // Handle function pointers if necessary
        //     origin = fPointerSymbol.Me;
        //     return true;
        // }
        origin = typeSymbol;
        return false;
    }

    public static bool IsTypeArray(this ITypeSymbol typeSymbol, out ITypeSymbol origin)
    {
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            // Get the type of items inside the array (e.g., int for int[])
            origin = arrayType.ElementType;

            // Get the dimensions of the array
            //int rank = arrayType.Rank;
            return true;
        }
        origin = typeSymbol;
        return false;
    }

    public static bool IsTypeNullable(this ITypeSymbol typeSymbol, out ITypeSymbol origin)
    {
        // 1. Check for Nullable Value Types (e.g., int?, bool?)
        if (typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            var type = ((INamedTypeSymbol)typeSymbol).TypeArguments.First();
            bool isGenericDefinition = type.Name == "T";
            origin = isGenericDefinition ? typeSymbol : type;
            return !isGenericDefinition;
        }

        // 2. Check for Nullable Reference Types (e.g., string?, MyClass?)
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
        {
            origin = typeSymbol.WithNullableAnnotation(NullableAnnotation.None);
            return true;
        }
        origin = typeSymbol;
        return false;
    }

    public static string GetGenericArgs(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedType
            && namedType.IsGenericType)
        {
            var genericArgs = namedType.TypeArguments.Select(arg => arg.Name);
            return $"<{string.Join(", ", genericArgs)}>";
        }
        return string.Empty;
    }

    public static IEnumerable<INamedTypeSymbol> GetAllNestedTypes(this INamedTypeSymbol? containingType,
        Func<INamedTypeSymbol, bool>? predicate = null)
    {
        while (containingType != null)
        {
            foreach (var type in containingType.GetMembers().OfType<INamedTypeSymbol>())
            {
                if (predicate == null || predicate(type))
                    yield return type;
            }
            containingType = containingType.BaseType;
        }
    }

    public static IEnumerable<AttributeData> GetAllAttributes(this ITypeSymbol typeSymbol, string type)
    {
        var list = new List<AttributeData>();
        var symbol = typeSymbol;
        while (symbol != null)
        {
            list.AddRange(symbol.GetAttributes(type));
            symbol = symbol.BaseType;
        }
        return list;
    }

    public static EquatableList<T> ToEquatableList<T>(this IEnumerable<T> source)
        where T : IEquatable<T>
         => new EquatableList<T>(source);

    public static TypedConstant GetNamedValue(this AttributeData attributeData, string name)
        => attributeData.NamedArguments.FirstOrDefault(kvp => string.Equals(kvp.Key, name, StringComparison.Ordinal)).Value;

    public static ConstantRecord GetNamedValue(this AttributeRecord attributeData, string name)
        => attributeData.NamedArguments.FirstOrDefault(kvp => string.Equals(kvp.Key, name, StringComparison.Ordinal)).Value;

    public static AttributeData? GetAttribute(this ISymbol symbol, string type)
        => type == null
            ? null
            : symbol.GetAttributes().FirstOrDefault(ad => string.Equals(ad.AttributeClass?.Name, type, StringComparison.Ordinal));

    public static AttributeRecord? GetAttribute(this AttributedRecord symbol, string type)
        => type == null
            ? null
            : symbol.Attributes?.FirstOrDefault(ad => string.Equals(ad.Class?.Name, type, StringComparison.Ordinal));

    public static AttributeData? GetAttribute(this ISymbol symbol, IEnumerable<string> types)
        => symbol.GetAttributes().FirstOrDefault(ad => types.Contains(ad.AttributeClass?.Name, StringComparer.Ordinal));

    public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, string type)
        => symbol.GetAttributes().Where(ad => string.Equals(ad.AttributeClass?.Name, type, StringComparison.Ordinal));

    public static IEnumerable<AttributeRecord> GetAttributes(this PropertyRecord symbol, string type)
        => symbol.Attributes?.Where(ad => string.Equals(ad.Class?.Name, type, StringComparison.Ordinal)) ?? [];

    public static AttributeData? GetInvokerAttribute(this ISymbol symbol,
        bool allowInvoker = true,
        bool allowSerialize = true,
        bool allowTable = true,
        bool allowColumn = true)
    {
        return (allowInvoker ? symbol.GetAttribute(cInvokerAttribute) : null)
            ?? (allowSerialize ? symbol.GetAttribute(cElementSerializerAttribute) : null)
            ?? (allowTable ? symbol.GetAttribute(cTableAttribute) : null)
            ?? (allowColumn ? symbol.GetAttribute(cColumnAttribute) : null);
    }

    public static AttributeRecord? GetInvokerAttribute(this AttributedRecord attributed,
        bool allowInvoker = true,
        bool allowSerializer = true,
        bool allowTable = true,
        bool allowColumn = true)
    {
        return (allowInvoker ? attributed.GetAttribute(cInvokerAttribute) : null)
            ?? (allowSerializer ? attributed.GetAttribute(cElementSerializerAttribute) : null)
            ?? (allowTable ? attributed.GetAttribute(cTableAttribute) : null)
            ?? (allowColumn ? attributed.GetAttribute(cColumnAttribute) : null);
    }

    public static NamespaceDeclarationSyntax? GetNamespace(this ClassDeclarationSyntax classDeclaration)
    {
        var parent = classDeclaration.Parent;
        while (parent != null)
        {
            if (parent is NamespaceDeclarationSyntax namespaceDeclaration)
            {
                return namespaceDeclaration;
            }
            parent = parent.Parent;
        }
        return null;
    }

    public static IEnumerable<INamedTypeSymbol> GetTypes(this INamespaceSymbol nameSpace)
    {
        foreach (var memeber in nameSpace.GetMembers())
        {
            if (memeber is INamespaceSymbol subNamespace)
            {
                foreach (var subType in GetTypes(subNamespace))
                {
                    yield return subType;
                }
            }
            else if (memeber is INamedTypeSymbol type)
            {
                yield return type;
            }
        }
    }

    public static bool IsEnumerable(this ITypeSymbol type)
    {
        if (string.Equals(type.Name, "string", StringComparison.OrdinalIgnoreCase))
            return false;
        return type.AllInterfaces.Any(p => p.Name == "IEnumerable" && p.TypeParameters.Length > 0);
    }

    public static bool IsInvoker(this INamedTypeSymbol type)
    {
        return type.AllInterfaces.Any(p => p.Name.StartsWith(cIInvoker, StringComparison.Ordinal));
    }

    public static bool IsBaseType(this ITypeSymbol type, string v)
    {
        if (type.Name == v)
            return true;

        while (type.BaseType != null)
        {
            if (type.BaseType.Name == v)
                return true;
            type = type.BaseType;
        }
        return false;
    }

    public static bool IsImplementInterface(this TypeRecord type, string iterfaceName)
    {
        if (type.Interfaces != null)
        {
            foreach (var interfaceType in type.Interfaces)
            {
                if (interfaceType.Type?.OriginName == iterfaceName)
                    return true;
                if (interfaceType.Type != null && IsImplementInterface(interfaceType.Type, iterfaceName))
                    return true;
            }
        }
        return false;
    }

    public static bool IsSubclassOf(this TypeRecord type, string baseName)
    {
        if (type.Name == baseName)
            return true;

        while (type.BaseType != null)
        {
            if (type.BaseType.Name == baseName)
                return true;
            type = type.BaseType;
        }
        return false;
    }

    public static IPropertySymbol? GetPrimaryKey(this ITypeSymbol type)
    {
        var property = GetPrimaryKey(type.GetMembers().OfType<IPropertySymbol>());
        if (property != null)
            return property;
        while (type.BaseType != null)
        {
            property = GetPrimaryKey(type.BaseType.GetMembers().OfType<IPropertySymbol>());
            if (property != null)
                return property;
            type = type.BaseType;
        }
        return null;

        IPropertySymbol? GetPrimaryKey(IEnumerable<IPropertySymbol> properties)
        {
            foreach (var property in properties)
            {
                var columnAttribute = property.GetAttribute(Attributes.Column);
                if (columnAttribute != null)
                {
                    var keys = columnAttribute.GetNamedValue("Keys");
                    if (!keys.IsNull && (((int?)keys.Value) & (1 << 0)) != 0)
                    {
                        return property;
                    }
                }
            }
            return null;
        }
    }

    public static TypeRecord GetTupleType(this IEnumerable<PropertyRecord> types)
        => GetTupleType(types.Select(p => p.Type));

    public static TypeRecord GetTupleType(this IEnumerable<TypeRecord> types)
    {
        if (types.Count() == 1)
            return types.First();
        var tupleType = new TypeRecord(
            OriginName: "ValueTuple",
            Name: $"ValueTuple<{string.Join(", ", types.Select(p => p.ToString()))}>",
            Namespace: "System",
            Keys: TypeRecordKeys.GenericType | TypeRecordKeys.ValueType,
            GenericParameters: types.Select(p => new TypeParameterRecord(p)).ToEquatableList());
        return tupleType;
    }

    public static List<PropertyRecord>? GetPrimaryKey(this TypeRecord type)
    {
        var property = GetPrimaryKey(type.Properties ?? Enumerable.Empty<PropertyRecord>());
        if (property.Any())
            return property.ToList();
        while (type.BaseType != null)
        {
            property = GetPrimaryKey(type.BaseType.Properties ?? Enumerable.Empty<PropertyRecord>());
            if (property.Any())
                return property.ToList();
            type = type.BaseType;
        }
        return null;

        IEnumerable<PropertyRecord> GetPrimaryKey(IEnumerable<PropertyRecord> properties)
        {
            foreach (var property in properties)
            {
                var columnAttribute = property.GetAttribute(Attributes.Column);
                if (columnAttribute != null)
                {
                    var keys = columnAttribute.GetNamedValue("Keys");
                    if (!keys.IsNull && (((int?)keys.Value) & (1 << 0)) != 0)
                    {
                        yield return property;
                    }
                }
            }
            yield break;
        }
    }

    //https://stackoverflow.com/a/36845547
    public static UsingDirectiveSyntax CreateUsingDirective(string usingName)
    {
        NameSyntax? qualifiedName = null;

        foreach (var identifier in usingName.Split('.'))
        {
            var name = SF.IdentifierName(identifier);

            if (qualifiedName != null)
            {
                qualifiedName = SF.QualifiedName(qualifiedName, name);
            }
            else
            {
                qualifiedName = name;
            }
        }

        return SF.UsingDirective(qualifiedName!);
    }

    public static void AddUsing(TypeRecord type, HashSet<string> usings)
    {
        AddUsing(type.Namespace, usings);
        if (type.IsGenericType && type.GenericParameters != null)
        {
            foreach (var genericArgument in type.GenericParameters)
                if (genericArgument.Type != null)
                    AddUsing(genericArgument.Type, usings);
        }
    }

    public static void AddUsing(string usingName, HashSet<string> usings)
    {
        if (!usings.Contains(usingName))
        {
            usings.Add(usingName);
        }
    }

    public static void AddUsing(Type type, Dictionary<string, UsingDirectiveSyntax> usings)
    {
        if (type.Namespace != null)
            AddUsing(type.Namespace, usings);
        if (type.IsGenericType)
        {
            foreach (var genericArgument in type.GetGenericArguments())
                AddUsing(genericArgument, usings);
        }
    }

    public static void AddUsing(string usingName, Dictionary<string, UsingDirectiveSyntax> usings)
    {
        if (!usings.TryGetValue(usingName, out var syntax))
        {
            usings.Add(usingName, Helper.CreateUsingDirective(usingName));
        }
    }

    // public static IEnumerable<CompilationUnitSyntax> LoadResources(Assembly assembly, string path, string newNameSpace, string output)
    // {
    //     if (output != null)
    //     {
    //         Directory.CreateDirectory(output);
    //     }

    //     foreach (var name in assembly.GetManifestResourceNames())
    //     {
    //         if (!name.StartsWith(path, StringComparison.Ordinal))
    //             continue;
    //         using var manifestStream = assembly.GetManifestResourceStream(name);
    //         if (manifestStream == null)
    //             continue;
    //         using var reader = new StreamReader(manifestStream);
    //         var text = reader.ReadToEnd().Replace("NewNameSpace", newNameSpace);
    //         var unit = SF.ParseCompilationUnit(text);
    //         if (output != null)
    //         {
    //             File.WriteAllText(Path.Combine(output, name.Substring(path.Length)), text);
    //         }
    //         yield return unit;
    //     }
    // }

    public static PropertyDeclarationSyntax GenProperty(string type, string name, bool setter, string? initializer = null)
    {
        var accessors = setter
            ? new[] {SF.AccessorDeclaration( SyntaxKind.GetAccessorDeclaration )
                        .WithSemicolonToken( SF.Token(SyntaxKind.SemicolonToken )),
                        SF.AccessorDeclaration( SyntaxKind.SetAccessorDeclaration )
                        .WithSemicolonToken( SF.Token(SyntaxKind.SemicolonToken ))}
            : new[] {SF.AccessorDeclaration( SyntaxKind.GetAccessorDeclaration )
                        .WithSemicolonToken( SF.Token(SyntaxKind.SemicolonToken )) };
        return SF.PropertyDeclaration(
                            attributeLists: SF.List<AttributeListSyntax>(),
                            modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                            type: SF.ParseTypeName(type),
                            explicitInterfaceSpecifier: null,
                            identifier: SF.Identifier(name),
                            accessorList: SF.AccessorList(SF.List(accessors)),
                            expressionBody: null,
                            initializer: initializer == null ? null : SF.EqualsValueClause(SF.ParseExpression(initializer)),
                            semicolonToken: initializer == null ? SF.Token(SyntaxKind.None) : SF.Token(SyntaxKind.SemicolonToken));
    }

    public static AttributeListSyntax GenAttributeList(string name, string? args = null)
    {
        return SF.AttributeList(SF.SingletonSeparatedList(GenAttribute(name, args)));
    }

    public static AttributeListSyntax GenAttributeList(KeyValuePair<string, string>[] names)
    {
        var list = new List<AttributeSyntax>();
        foreach (var name in names)
        {
            list.Add(GenAttribute(name.Key, name.Value));
        }
        return SF.AttributeList(SF.SeparatedList(list));
    }

    public static AttributeSyntax GenAttribute(string name, string? args)
    {
        var attribure = SF.Attribute(SF.IdentifierName(name));
        if (args != null)
        {
            attribure = attribure.WithArgumentList(
                    SF.AttributeArgumentList(
                        SF.SingletonSeparatedList(
                            SF.AttributeArgument(
                                SF.ParseExpression(args)))));
        }

        return attribure;
    }

    public static void GetUsings(this TypeRecord type, HashSet<string> usings)
    {
        usings.Add(type.Namespace);
        var baseType = type.BaseType;
        if (baseType != null)
        {
            usings.Add(baseType.Namespace);
            baseType.GetUsings(usings);
        }
        if (type.GenericParameters != null)
        {
            foreach (var typeParameter in type.GenericParameters)
            {
                if (typeParameter.Type != null)
                {
                    usings.Add(typeParameter.Type.Namespace);
                }
            }
        }
        if (type.Interfaces! != null)
        {
            foreach (var interfaceReference in type.Interfaces!)
            {
                if (interfaceReference.Type is { } interfaceType)
                {
                    usings.Add(interfaceType.Namespace);
                    interfaceType.GetUsings(usings);
                }
            }
        }
        if (type.Constructors != null)
        {
            foreach (var constructor in type.Constructors)
            {
                foreach (var parameter in constructor.Parameters)
                {
                    if (parameter.Type != null)
                    {
                        usings.Add(parameter.Type.Namespace);
                    }
                }
            }
        }
        if (type.Properties != null)
        {
            foreach (var property in type.Properties)
            {
                usings.Add(property.Type.Namespace);
            }
        }
        if (type.Fields != null)
        {
            foreach (var field in type.Fields)
            {
                usings.Add(field.Type.Namespace);
            }
        }
        if (type.Methods != null)
        {
            foreach (var method in type.Methods)
            {
                foreach (var parameter in method.Parameters!)
                {
                    if (parameter.Type != null)
                    {
                        usings.Add(parameter.Type.Namespace);
                    }
                }
                usings.Add(method.ReturnType.Namespace);
            }
        }
        usings.Remove(string.Empty);
    }

    public static string GetDisplayName(this ITypeSymbol typeSymbol)
    {
        // if(typeSymbol is INamedTypeSymbol named
        // && named.GetGenericArgs)
        return typeSymbol.ToDisplayString(Helper.NameDisplayFormat);
    }


    public static bool IsUnmanagedCustom(this Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        // 1. Указатели всегда unmanaged
        if (type.IsPointer) return true;

        // 2. Перечисления всегда unmanaged
        if (type.IsEnum) return true;

        // 3. Должен быть значимым типом (ValueType)
        if (!type.IsValueType) return false;

        // 4. Проверка примитивных типов C#
        if (type.IsPrimitive)
        {
            // Исключаем IntPtr и UIntPtr, если работаем в старых версиях .NET Framework, 
            // но в .NET Standard 2.0 / .NET Core они официально unmanaged
            return true;
        }

        // Decimal не является примитивным типом с точки зрения IsPrimitive, но он unmanaged
        if (type == typeof(decimal)) return true;

        // 5. Проверка Nullable<T> — они НЕ являются unmanaged (даже Nullable<int>)
        if (Nullable.GetUnderlyingType(type) != null) return false;

        // 6. Если это пользовательская структура, рекурсивно проверяем все её non-static поля
        // Все поля на любом уровне вложенности должны быть unmanaged
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (!IsUnmanagedCustom(field.FieldType))
            {
                return false;
            }
        }

        return true;
    }

    public static int IndexOf<T>(this List<T> list, Func<T, bool> predicate)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if(predicate(list[i]))
                return i;
        }
        return -1;
    }
}
