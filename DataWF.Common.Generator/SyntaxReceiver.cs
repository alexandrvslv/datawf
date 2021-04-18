using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DataWF.Common.Generator
{
    public partial class InvokerGenerator
    {
        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        internal class SyntaxReceiver : ISyntaxReceiver
        {
            public List<ClassDeclarationSyntax> CandidateCalsses { get; } = new List<ClassDeclarationSyntax>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                try
                {
                    // any field with at least one attribute is a candidate for property generation
                    if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax)
                    {
                        if (classDeclarationSyntax.AttributeLists.Any(p => p.Attributes.Select(p => p.Name.ToString())
                        .Any(p => string.Equals(p, "InvokerGenerator", StringComparison.Ordinal)
                        || string.Equals(p, "InvokerGeneratorAttribute", StringComparison.Ordinal))))
                            CandidateCalsses.Add(classDeclarationSyntax);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Generator Fail: {ex.Message} at {ex.StackTrace}");
#if DEBUG
                    //System.Diagnostics.Debugger.Launch();
#endif
                }
            }
        }
    }


}