using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DataWF.WebService.Generator
{

    public static class SyntaxHelper
    {
        //https://stackoverflow.com/a/36845547
        public static UsingDirectiveSyntax CreateUsingDirective(string usingName)
        {
            NameSyntax qualifiedName = null;

            foreach (var identifier in usingName.Split('.'))
            {
                var name = SyntaxFactory.IdentifierName(identifier);

                if (qualifiedName != null)
                {
                    qualifiedName = SyntaxFactory.QualifiedName(qualifiedName, name);
                }
                else
                {
                    qualifiedName = name;
                }
            }

            return SyntaxFactory.UsingDirective(qualifiedName);
        }

        public static CompilationUnitSyntax GenUnit(MemberDeclarationSyntax @class, string nameSpace, IEnumerable<UsingDirectiveSyntax> usings)
        {
            var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(nameSpace))
                                             .AddMembers(@class);
            return GenUnit(@namespace, usings);
        }

        public static CompilationUnitSyntax GenUnit(NamespaceDeclarationSyntax @namespace, IEnumerable<UsingDirectiveSyntax> usings)
        {
            return SyntaxFactory.CompilationUnit(
                            externs: SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
                            usings: SyntaxFactory.List(usings),
                            attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                            members: SyntaxFactory.List<MemberDeclarationSyntax>(new[] { @namespace }))
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
                    var text = reader.ReadToEnd().Replace("NewNameSpace", newNameSpace, StringComparison.Ordinal);
                    var unit = SyntaxFactory.ParseCompilationUnit(text);
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
                ? new[] {SyntaxFactory.AccessorDeclaration( SyntaxKind.GetAccessorDeclaration )
                        .WithSemicolonToken( SyntaxFactory.Token(SyntaxKind.SemicolonToken )),
                        SyntaxFactory.AccessorDeclaration( SyntaxKind.SetAccessorDeclaration )
                        .WithSemicolonToken( SyntaxFactory.Token(SyntaxKind.SemicolonToken ))}
                : new[] {SyntaxFactory.AccessorDeclaration( SyntaxKind.GetAccessorDeclaration )
                        .WithSemicolonToken( SyntaxFactory.Token(SyntaxKind.SemicolonToken )) };
            return SyntaxFactory.PropertyDeclaration(
                                attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                                modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                                type: SyntaxFactory.ParseTypeName(type),
                                explicitInterfaceSpecifier: null,
                                identifier: SyntaxFactory.Identifier(name),
                                accessorList: SyntaxFactory.AccessorList(SyntaxFactory.List(accessors)),
                                expressionBody: null,
                                initializer: initializer == null ? null : SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(initializer)),
                                semicolonToken: initializer == null ? SyntaxFactory.Token(SyntaxKind.None) : SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        public static AttributeListSyntax GenAttribute(string name)
        {
            return SF.AttributeList(
                SF.SingletonSeparatedList(
                    SF.Attribute(
                        SF.IdentifierName(name))));
        }

        public static AttributeListSyntax GenAttribute(string name, string args)
        {
            return SF.AttributeList(
                SF.SingletonSeparatedList(
                    SF.Attribute(
                        SF.IdentifierName(name)).WithArgumentList(
                        SF.AttributeArgumentList(
                            SF.SingletonSeparatedList(
                                SF.AttributeArgument(
                                    SF.ParseExpression(args)))))));
        }
    }

}