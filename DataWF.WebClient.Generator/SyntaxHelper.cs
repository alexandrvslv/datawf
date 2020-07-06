using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
//using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DataWF.WebClient.Generator
{

    public static class SyntaxHelper
    {
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

        public static CompilationUnitSyntax GenUnit(MemberDeclarationSyntax @class, string nameSpace, IEnumerable<UsingDirectiveSyntax> usings)
        {
            var @namespace = SF.NamespaceDeclaration(SF.ParseName(nameSpace))
                                             .AddMembers(@class);
            return SF.CompilationUnit(
                externs: SF.List<ExternAliasDirectiveSyntax>(),
                usings: SF.List(usings),
                attributeLists: SF.List<AttributeListSyntax>(),
                members: SF.List<MemberDeclarationSyntax>(new[] { @namespace }))
                                        .NormalizeWhitespace("    ", true);
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