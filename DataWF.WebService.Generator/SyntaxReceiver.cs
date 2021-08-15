using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.WebService.Generator
{
    class SyntaxReceiver : ISyntaxReceiver
    {
        public HashSet<string> NameSpaces { get; set; } = new HashSet<string>();

        /// <summary>
        /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
        /// </summary>
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // any field with at least one attribute is a candidate for property generation
            if (syntaxNode is NamespaceDeclarationSyntax namespaceDeclarationSyntax)
            {
                var nameSpace = namespaceDeclarationSyntax.Name.ToString();

                if (!NameSpaces.Contains(nameSpace, StringComparer.Ordinal))
                    NameSpaces.Add(nameSpace);
            }
        }
    }

}