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
        public List<ClassDeclarationSyntax> WebSchemaCandidates { get; } = new List<ClassDeclarationSyntax>();
        public List<ClassDeclarationSyntax> SchemaControllerCandidates { get; } = new List<ClassDeclarationSyntax>();
        public List<ClassDeclarationSyntax> ProviderCandidates { get; } = new List<ClassDeclarationSyntax>();

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
                           .Select(p => GetNameString(p.Name)));

                    foreach (var attribute in attributesName)
                    {
                        if (context.CancellationToken.IsCancellationRequested)
                            return;
                        if (attribute.StartsWith(Helper.cInvokerGenerator, StringComparison.Ordinal))
                        {
                            IvokerCandidates.Add(classDeclarationSyntax);
                        }

                        if (attribute.StartsWith(Helper.cTable, StringComparison.Ordinal)
                            || attribute.StartsWith(Helper.cAbstractTable, StringComparison.Ordinal)
                            || attribute.StartsWith(Helper.cVirtualTable, StringComparison.Ordinal)
                            || attribute.StartsWith(Helper.cLogTable, StringComparison.Ordinal))
                        {
                            TableCandidates.Add(classDeclarationSyntax);
                        }
                        else if (attribute.Equals(Helper.cSchema, StringComparison.Ordinal)
                            || attribute.Equals(Helper.cSchemaAttribute, StringComparison.Ordinal))
                        {
                            SchemaCandidates.Add(classDeclarationSyntax);
                        }
                        else if (attribute.StartsWith(Helper.cWebSchema, StringComparison.Ordinal))
                        {
                            WebSchemaCandidates.Add(classDeclarationSyntax);
                        }
                        else if (attribute.StartsWith(Helper.cSchemaController, StringComparison.Ordinal))
                        {
                            SchemaControllerCandidates.Add(classDeclarationSyntax);
                        }
                        else if (attribute.StartsWith(Helper.cProvider, StringComparison.Ordinal))
                        {
                            ProviderCandidates.Add(classDeclarationSyntax);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LaunchDebugger();
                Console.WriteLine($"Generator Fail: {ex.Message} at {ex.StackTrace}");
            }
        }

        private string GetNameString(NameSyntax name)
        {
            if (name is IdentifierNameSyntax iName)
                return iName.Identifier.ValueText;
            if (name is QualifiedNameSyntax qName)
                return GetNameString(qName.Left);
            return name.ToString();
        }

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            throw new NotImplementedException();
        }
    }


}