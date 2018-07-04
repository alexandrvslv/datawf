using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
//using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DataWF.Web.CodeGenerator
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
            return SyntaxFactory.CompilationUnit(
                externs: SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
                usings: SyntaxFactory.List(usings),
                attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                members: SyntaxFactory.List<MemberDeclarationSyntax>(new[] { @namespace }))
                                        .NormalizeWhitespace("    ", true);
        }

        public static IEnumerable<CompilationUnitSyntax> LoadResources(Assembly assembly, string nameSpace, string output = null)
        {
            if (output != null)
            {
                Directory.CreateDirectory(output);
            }

            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (!name.StartsWith(nameSpace))
                    continue;
                using (var manifestStream = assembly.GetManifestResourceStream(name))
                using (var reader = new StreamReader(manifestStream))
                {
                    var text = reader.ReadToEnd();
                    var unit = SyntaxFactory.ParseCompilationUnit(text);
                    if (output != null)
                    {
                        File.WriteAllText(Path.Combine(output, name.Substring(nameSpace.Length)), text);
                    }
                    yield return unit;
                }
            }
        }

        public static PropertyDeclarationSyntax GenProperty(string type, string name, bool setter)
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
                                initializer: null,
                                semicolonToken: SyntaxFactory.Token(SyntaxKind.None));
        }
    }

}