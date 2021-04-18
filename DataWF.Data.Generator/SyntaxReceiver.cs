using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DataWF.Data.Generator
{
    internal class SyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> TableCandidates { get; } = new List<ClassDeclarationSyntax>();
        public List<ClassDeclarationSyntax> SchemaCandidates { get; } = new List<ClassDeclarationSyntax>();

        /// <summary>
        /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
        /// </summary>
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            try
            {
                // any field with at least one attribute is a candidate for property generation
                if (syntaxNode is ClassDeclarationSyntax classDeclaration)
                {
                    var attributesName = classDeclaration.AttributeLists.SelectMany(p => p.Attributes
                            .Select(p => p.Name.ToString()));
                    if (attributesName.Any(p => string.Equals(p, "Table", StringComparison.Ordinal)
                    || string.Equals(p, "TableAttribute", StringComparison.Ordinal)
                    || string.Equals(p, "AbstractTable", StringComparison.Ordinal)
                    || string.Equals(p, "AbstractTableAttribute", StringComparison.Ordinal)
                    || string.Equals(p, "VirtualTable", StringComparison.Ordinal)
                    || string.Equals(p, "VirtualTableAttribute", StringComparison.Ordinal)
                    || string.Equals(p, "ItemType", StringComparison.Ordinal)
                    || string.Equals(p, "ItemTypeAttribute", StringComparison.Ordinal)
                    || string.Equals(p, "LogTable", StringComparison.Ordinal)
                    || string.Equals(p, "LogTableAttribute", StringComparison.Ordinal)
                    ))
                    {
                        TableCandidates.Add(classDeclaration);
                    }
                    else if (attributesName.Any(p => string.Equals(p, "Schema", StringComparison.Ordinal)
                     || string.Equals(p, "SchemaAttribute", StringComparison.Ordinal)
                    ))
                    {
                        SchemaCandidates.Add(classDeclaration);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Generator Fail: {ex.Message} at {ex.StackTrace}");
#if DEBUG
                if (!System.Diagnostics.Debugger.IsAttached)
                {
                    //System.Diagnostics.Debugger.Launch();
                }
#endif
            }
        }
    }


}