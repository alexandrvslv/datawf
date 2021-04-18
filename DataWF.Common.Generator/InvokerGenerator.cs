using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DataWF.Common.Generator
{
    [Generator]
    public partial class InvokerGenerator : ISourceGenerator
    {

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // retreive the populated receiver 
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;
            try
            {

                var codeGenerator = new InvokerCodeGenerator(ref context, context.Compilation);

                // loop over the candidate fields, and keep the ones that are actually annotated
                foreach (ClassDeclarationSyntax classDeclaration in receiver.CandidateCalsses)
                {
                    if (context.CancellationToken.IsCancellationRequested)
                        return;
                    codeGenerator.Process(classDeclaration);
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