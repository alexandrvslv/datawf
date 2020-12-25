﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DataWF.Common.Generator
{
    [Generator]
    public class InvokerGenerator : ISourceGenerator
    {

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // add the attribute text
            //context.AddSource("AutoNotifyAttribute", SourceText.From(attributeText, Encoding.UTF8));

            // retreive the populated receiver 
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            // we're going to create a new compilation that contains the attribute.
            // TODO: we should allow source generators to provide source during initialize, so that this step isn't required.
            CSharpParseOptions options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            //Compilation compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(attributeText, Encoding.UTF8), options));
            var invokerGeneratorAtribute = context.Compilation.GetTypeByMetadataName("DataWF.Common.InvokerGeneratorAttribute");

            // loop over the candidate fields, and keep the ones that are actually annotated
            foreach (ClassDeclarationSyntax classDeclaration in receiver.CandidateCalsses)
            {
                SemanticModel model = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                INamedTypeSymbol typeSymbol = model.GetDeclaredSymbol(classDeclaration);
                var parameters = typeSymbol.GetMembers()
                    .Where(p => p is IPropertySymbol symbol
                    && !symbol.IsOverride
                    && !symbol.IsIndexer
                    && !symbol.IsStatic
                    && !symbol.Name.Contains('.'))
                    .Select(p => p as IPropertySymbol);
                if (parameters.Count() > 0)
                {
                    var classSource = ProcessClass(typeSymbol, parameters, invokerGeneratorAtribute, context);
                    if (classSource != null)
                    {
                        context.AddSource($"{typeSymbol.Name}Invokers.cs", SourceText.From(classSource, Encoding.UTF8));
                    }
                }
            }
        }

        private string ProcessClass(INamedTypeSymbol classSymbol, IEnumerable<IPropertySymbol> properties, INamedTypeSymbol invokerGeneratorAtribute, GeneratorExecutionContext context)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                return null; //TODO: issue a diagnostic that it must be top level
            }

            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var className = classSymbol.Name;

            var attributeData = classSymbol.GetAttributes().Single(p => p.AttributeClass.Equals(invokerGeneratorAtribute, SymbolEqualityComparer.Default));
            var argInstance = attributeData.NamedArguments.SingleOrDefault(p => p.Key == "Instance").Value;
            var isInstance = !argInstance.IsNull && (bool)argInstance.Value;
            var genericArgs = classSymbol.IsGenericType ? $"<{string.Join(", ", classSymbol.TypeParameters.Select(p => p.Name))}>" : string.Empty;
            // begin building the generated source
            StringBuilder source = new StringBuilder($@"{(namespaceName != "DataWF.Common" ? "using DataWF.Common;" : "")}
using {namespaceName};
");

            foreach (IPropertySymbol propertySymbol in properties)
            {
                ProcessInvokerAttributes(source, propertySymbol, classSymbol);
            }

            source.Append($@"
namespace {namespaceName}
{{
    public partial class {className}{genericArgs}
    {{");

            foreach (IPropertySymbol propertySymbol in properties)
            {
                ProcessInvokerClass(source, propertySymbol, classSymbol, genericArgs, isInstance);
            }
            source.Append(@"}
}");
            return source.ToString();
        }

        private void ProcessInvokerClass(StringBuilder source, IPropertySymbol propertySymbol, INamedTypeSymbol classSymbol, string genericArgs, bool instance)
        {
            // get the name and type of the field
            string propertyName = propertySymbol.Name;
            ITypeSymbol propertyType = propertySymbol.Type;

            var invokerName = $"{propertyName}Invoker";
            var instanceCache = instance ? $@"
            public static readonly {invokerName} Instance = new {invokerName}();" : "";
            source.Append($@"
        public class {invokerName}: Invoker<{classSymbol.Name}{genericArgs}, {propertyType}>
        {{{instanceCache}
            public override string Name => nameof({classSymbol.Name}{genericArgs}.{propertyName});
            public override bool CanWrite => {(!propertySymbol.IsReadOnly ? "true" : "false")};
            public override {propertyType} GetValue({classSymbol.Name}{genericArgs} target) => target.{propertyName};
            public override void SetValue({classSymbol.Name}{genericArgs} target, {propertyType} value){(propertySymbol.IsReadOnly ? "{}" : $" => target.{propertyName} = value;")}
        }}
");

        }

        private void ProcessInvokerAttributes(StringBuilder source, IPropertySymbol propertySymbol, INamedTypeSymbol classSymbol)
        {
            // get the name and type of the field
            string propertyName = propertySymbol.Name;
            ITypeSymbol propertyType = propertySymbol.Type;
            string genericArgs = classSymbol.IsGenericType ? $"<{string.Join("", Enumerable.Repeat(",", classSymbol.TypeParameters.Length - 1))}>" : "";
            string nameGenericArgs = classSymbol.IsGenericType ? "<object>" : "";
            source.Append($@"[assembly: Invoker(typeof({classSymbol.Name}{genericArgs}), ""{propertyName}"", typeof({classSymbol.Name}{genericArgs}.{propertyName}Invoker))]
");
        }


        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        class SyntaxReceiver : ISyntaxReceiver
        {
            public List<ClassDeclarationSyntax> CandidateCalsses { get; } = new List<ClassDeclarationSyntax>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // any field with at least one attribute is a candidate for property generation
                if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax)
                {
                    if (classDeclarationSyntax.AttributeLists.Any(p => p.Attributes.Any(p => p.Name.ToString() == "InvokerGenerator")))
                        CandidateCalsses.Add(classDeclarationSyntax);
                }
            }
        }
    }


}