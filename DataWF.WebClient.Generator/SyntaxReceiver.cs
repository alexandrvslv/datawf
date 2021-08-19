using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.WebClient.Generator
{
    class SyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> ClientProviderCandidate { get; } = new List<ClassDeclarationSyntax>();

        /// <summary>
        /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
        /// </summary>
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // any field with at least one attribute is a candidate for property generation
            if (syntaxNode is ClassDeclarationSyntax classDeclaration)
            {
                var attributesName = classDeclaration.AttributeLists.SelectMany(p => p.Attributes
                            .Select(p => p.Name.ToString()));
                if (attributesName.Any(p =>
                       string.Equals(p, "ClientProvider", StringComparison.Ordinal)
                       || string.Equals(p, "ClientProviderAttribute", StringComparison.Ordinal)))
                {
                    ClientProviderCandidate.Add(classDeclaration);
                }
                
            }
        }
    }
}