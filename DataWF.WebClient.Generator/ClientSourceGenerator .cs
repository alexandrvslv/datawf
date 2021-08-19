using DataWF.Common.Generator;
using Microsoft.CodeAnalysis;

namespace DataWF.WebClient.Generator
{
    [Generator]
    public class ClientSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            System.Diagnostics.Debugger.Launch();
            var clientProviderAtributeType = context.Compilation.GetTypeByMetadataName("DataWF.Common.ClientProviderAttribute");
            if (clientProviderAtributeType == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(CodeGenerator.DDCommonLibrary, Location.None));
                return;
            }

            var codeGenerator = new ClientProviderCodeGenerator(ref context, context.Compilation)
            {
                ClientProviderAttributeType = clientProviderAtributeType,
                InvokerCodeGenerator = new InvokerCodeGenerator(ref context, context.Compilation)
            };

            foreach (var classSyntax in receiver.ClientProviderCandidate)
            {
                codeGenerator.Process(classSyntax);
            }
        }
    }
}
