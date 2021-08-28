using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DataWF.Common.Generator
{
    /// <summary>
    /// Created on demand before each generation pass
    /// </summary>
    internal class SyntaxReceiver : ISyntaxReceiver
    {
        private readonly GeneratorInitializationContext context;

        public SyntaxReceiver(ref GeneratorInitializationContext context)
        {
            this.context = context;
        }

        public List<ClassDeclarationSyntax> IvokerCandidates { get; } = new List<ClassDeclarationSyntax>();
        public List<ClassDeclarationSyntax> TableCandidates { get; } = new List<ClassDeclarationSyntax>();
        public List<ClassDeclarationSyntax> SchemaCandidates { get; } = new List<ClassDeclarationSyntax>();
        public List<ClassDeclarationSyntax> ClientProviderCandidate { get; } = new List<ClassDeclarationSyntax>();
        public List<ClassDeclarationSyntax> SchemaControllerCandidate { get; } = new List<ClassDeclarationSyntax>();

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
                    var attributesName = classDeclarationSyntax.AttributeLists.SelectMany(p => p.Attributes
                           .Select(p => p.Name.ToString()));

                    foreach (var attribute in attributesName)
                    {
                        if (context.CancellationToken.IsCancellationRequested)
                            return;
                        if (string.Equals(attribute, "InvokerGenerator", StringComparison.Ordinal)
                            || string.Equals(attribute, "InvokerGeneratorAttribute", StringComparison.Ordinal))
                        {
                            IvokerCandidates.Add(classDeclarationSyntax);
                        }

                        if (string.Equals(attribute, "Table", StringComparison.Ordinal)
                            || string.Equals(attribute, "TableAttribute", StringComparison.Ordinal)
                            || string.Equals(attribute, "AbstractTable", StringComparison.Ordinal)
                            || string.Equals(attribute, "AbstractTableAttribute", StringComparison.Ordinal)
                            || string.Equals(attribute, "VirtualTable", StringComparison.Ordinal)
                            || string.Equals(attribute, "VirtualTableAttribute", StringComparison.Ordinal)
                            || string.Equals(attribute, "LogTable", StringComparison.Ordinal)
                            || string.Equals(attribute, "LogTableAttribute", StringComparison.Ordinal)
                            )
                        {
                            TableCandidates.Add(classDeclarationSyntax);
                        }
                        else if (string.Equals(attribute, "Schema", StringComparison.Ordinal)
                            || string.Equals(attribute, "SchemaAttribute", StringComparison.Ordinal)
                        )
                        {
                            SchemaCandidates.Add(classDeclarationSyntax);
                        }
                        else if (string.Equals(attribute, "ClientProvider", StringComparison.Ordinal)
                            || string.Equals(attribute, "ClientProviderAttribute", StringComparison.Ordinal))
                        {
                            ClientProviderCandidate.Add(classDeclarationSyntax);
                        }
                        else if (string.Equals(attribute, "SchemaController", StringComparison.Ordinal)
                           || string.Equals(attribute, "SchemaControllerAttribute", StringComparison.Ordinal))
                        {
                            SchemaControllerCandidate.Add(classDeclarationSyntax);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SyntaxHelper.LaunchDebugger();
                Console.WriteLine($"Generator Fail: {ex.Message} at {ex.StackTrace}");
            }
        }

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            throw new NotImplementedException();
        }
    }


}