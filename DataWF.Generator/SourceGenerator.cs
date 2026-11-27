using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using DataWF.Generator.Records;

namespace DataWF.Generator;


[Generator(LanguageNames.CSharp)]
public class SourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = new TypeRecordProvider();
        //IncrementalValueProvider<TypeRecordProvider>.
        var pipelineInvokers = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: Attributes.Invoker,
            predicate: static (syntaxNode, cancellationToken) => syntaxNode is TypeDeclarationSyntax,
            transform: (context, cancellationToken) =>
            {
                return new InvokerGenerator(provider, (INamedTypeSymbol)context.TargetSymbol);
            }
        );
        var pipelineSerializers = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: Attributes.Serializer,
            predicate: static (syntaxNode, cancellationToken) => syntaxNode is TypeDeclarationSyntax,
            transform: (context, cancellationToken) =>
            {
                return new SerializerGenerator(provider, (INamedTypeSymbol)context.TargetSymbol);
            }
        );

        context.RegisterSourceOutput(pipelineInvokers, static (spc, model) =>
        {
            model.Process(ref spc);
        });        
        context.RegisterSourceOutput(pipelineSerializers, static (spc, model) =>
        {
            model.Process(ref spc);
        });
    }
}