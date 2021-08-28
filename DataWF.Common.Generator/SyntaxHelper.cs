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
    public static class SyntaxHelper
    {
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
        private static readonly Dictionary<IAssemblySymbol, Dictionary<string, INamedTypeSymbol>> cacheAssemblySymbolTypes = new Dictionary<IAssemblySymbol, Dictionary<string, INamedTypeSymbol>>();
        //https://stackoverflow.com/a/24768641
        public static string ToInitcap(this string str, params char[] separator)
        {
            if (string.IsNullOrEmpty(str))
                return str;
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

        public static INamedTypeSymbol ParseType(Type value, IEnumerable<IAssemblySymbol> assemblies)
        {
            return ParseType(value.FullName, assemblies);
        }

        public static INamedTypeSymbol ParseType(string value, IEnumerable<IAssemblySymbol> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                var type = ParseType(value, assembly);
                if (type != null)
                    return type;
            }
            return null;
        }

        public static INamedTypeSymbol ParseType(string value, IAssemblySymbol assembly)
        {
            var byName = value.IndexOf('.') < 0;
            if (byName)
            {
                if (!cacheAssemblySymbolTypes.TryGetValue(assembly, out var cache))
                {
                    var definedTypes = assembly.GlobalNamespace.GetTypes();
                    cacheAssemblySymbolTypes[assembly] =
                        cache = new Dictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);
                    foreach (var defined in definedTypes)
                    {
                        if(defined.DeclaredAccessibility == Accessibility.Public)
                            cache[defined.Name] = defined;
                    }
                }

                return cache.TryGetValue(value, out var type) ? type : null;
            }
            else
            {
                return assembly.GetTypeByMetadataName(value);
            }
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

        public static IPropertySymbol GetPrimaryKey(this ITypeSymbol type)
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
                    var columnAttribute = property.GetAttribute(BaseGenerator.Attributes.Column);
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
                usings.Add(usingName, SyntaxHelper.CreateUsingDirective(usingName));
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