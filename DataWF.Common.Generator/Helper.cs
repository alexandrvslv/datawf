using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DataWF.Common.Generator
{
    public static class Helper
    {
        public static readonly string cCodeKey = "CodeKey";
        public static readonly string cFileNameKey = "FileNameKey";
        public static readonly string cFileLastWriteKey = "FileLastWriteKey";
        public static readonly string cIWebProvider = "IWebProvider";
        public static readonly string cModelProvider = "ModelProvider";
        public static readonly string cDBProvider = "DBProvider";
        public static readonly string cIDBProvider = "IDBProvider";
        public static readonly string cDBSchema = "DBSchema";
        public static readonly string cDBSchemaLog = "DBSchemaLog";
        public static readonly string cIDBSchema = "IDBSchema";
        public static readonly string cIDBSchemaLog = "IDBSchemaLog";
        public static readonly string cDBTable = "DBTable";
        public static readonly string cIDBTable = "IDBTable";
        public static readonly string cDBTableLog = "DBTableLog";
        public static readonly string cIDBTableLog = "IDBTableLog";
        public static readonly string cDBItem = "DBItem";
        public static readonly string cDBItemLog = "DBItemLog";
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
        public static readonly string cInvokerGenerator = "InvokerGenerator";
        public static readonly string cInvokerGeneratorAttribute = "InvokerGeneratorAttribute";
        public static readonly string cWebSchema = "WebSchema";
        public static readonly string cWebSchemaAttribute = "WebSchemaAttribute";
        public static readonly string cSchemaController = "SchemaController";
        public static readonly string cSchemaControllerAttribute = "SchemaControllerAttribute";
        public static readonly string cProvider = "Provider";
        public static readonly string cProviderAttribute = "ProviderAttribute";
        public static readonly string cGet = "Get";
        public static readonly string cPost = "Post";
        public static readonly string cPut = "Put";
        public static readonly string cDelete = "Delete";

        public static readonly DiagnosticDescriptor DDCommonLibrary = new DiagnosticDescriptor("DWFG001",
            "DataWF.Common references not found",
            "DataWF.Common references not found",
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

        private static readonly Dictionary<Assembly, Dictionary<string, Type>> cacheAssemblyTypes = new Dictionary<Assembly, Dictionary<string, Type>>();

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



        public static Type ParseType(string value, Assembly assembly)
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

        public static IEnumerable<AttributeData> GetAllAttributes(this ITypeSymbol typeSymbol, INamedTypeSymbol type)
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

        public static TypedConstant GetNamedValue(this AttributeData attributeData, string name)
        {
            return attributeData.NamedArguments.FirstOrDefault(kvp => string.Equals(kvp.Key, name, StringComparison.Ordinal)).Value;
        }

        public static AttributeData GetAttribute(this ISymbol symbol, INamedTypeSymbol type)
        {
            if (type == null)
                return null;
            return symbol.GetAttributes().FirstOrDefault(ad => string.Equals(ad.AttributeClass.Name, type.Name, StringComparison.Ordinal));
        }

        public static AttributeData GetAttribute(this ISymbol symbol, IEnumerable<INamedTypeSymbol> types)
        {
            return symbol.GetAttributes().FirstOrDefault(ad => types.Select(p => p.Name).Contains(ad.AttributeClass.Name, StringComparer.Ordinal));
        }

        public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, INamedTypeSymbol type)
        {
            return symbol.GetAttributes().Where(ad => string.Equals(ad.AttributeClass.Name, type.Name, StringComparison.Ordinal));
        }

        public static NamespaceDeclarationSyntax GetNamespace(this ClassDeclarationSyntax classDeclaration)
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

        public static bool IsEnumerable(this ITypeSymbol returnResultType)
        {
            if (string.Equals(returnResultType.Name, "string", StringComparison.OrdinalIgnoreCase))
                return false;
            return returnResultType.AllInterfaces.Any(p => p.Name == "IEnumerable" && p.TypeParameters.Length > 0);
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

        public static IPropertySymbol GetPrimaryKey(this ITypeSymbol type, CompilationContext compilationContext)
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

            IPropertySymbol GetPrimaryKey(IEnumerable<IPropertySymbol> properties)
            {
                foreach (var property in properties)
                {
                    var columnAttribute = property.GetAttribute(compilationContext.Attributes.Column);
                    if (columnAttribute != null)
                    {
                        var keys = columnAttribute.NamedArguments.FirstOrDefault(p => p.Key == "Keys").Value;
                        if (!keys.IsNull && (((int)keys.Value) & (1 << 0)) != 0)
                        {
                            return property;
                        }
                    }
                }
                return null;
            }
        }


        //https://stackoverflow.com/a/36845547
        public static UsingDirectiveSyntax CreateUsingDirective(string usingName)
        {
            NameSyntax qualifiedName = null;

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

            return SF.UsingDirective(qualifiedName);
        }

        public static void AddUsing(INamedTypeSymbol type, HashSet<string> usings)
        {
            AddUsing(type.ContainingNamespace.ToDisplayString(), usings);
            if (type.IsGenericType)
            {
                foreach (var genericArgument in type.TypeArguments)
                    if (genericArgument is INamedTypeSymbol namedType)
                        AddUsing(namedType, usings);
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

        public static IEnumerable<CompilationUnitSyntax> LoadResources(Assembly assembly, string path, string newNameSpace, string output)
        {
            if (output != null)
            {
                Directory.CreateDirectory(output);
            }

            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (!name.StartsWith(path, StringComparison.Ordinal))
                    continue;
                using (var manifestStream = assembly.GetManifestResourceStream(name))
                using (var reader = new StreamReader(manifestStream))
                {
                    var text = reader.ReadToEnd().Replace("NewNameSpace", newNameSpace);
                    var unit = SF.ParseCompilationUnit(text);
                    if (output != null)
                    {
                        File.WriteAllText(Path.Combine(output, name.Substring(path.Length)), text);
                    }
                    yield return unit;
                }
            }
        }

        public static PropertyDeclarationSyntax GenProperty(string type, string name, bool setter, string initializer = null)
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

        public static AttributeListSyntax GenAttributeList(string name, string args = null)
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

        public static AttributeSyntax GenAttribute(string name, string args)
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

        public static void ConsoleWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"Warning: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
        }

        public static void ConsoleInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"Information: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
        }


    }

}