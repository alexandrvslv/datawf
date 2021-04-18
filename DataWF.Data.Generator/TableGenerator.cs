using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using DataWF.Common.Generator;

namespace DataWF.Data.Generator
{
    [Generator]
    public class TableGenerator : ISourceGenerator
    {
        private TableCodeGenerator tableCodeGenerator;
        private SchemaCodeGenerator schemaCodeGenerator;

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

                var cultures = new List<string>(new[] { "RU", "EN" });//TODO Pass as argument
                tableCodeGenerator = new TableCodeGenerator(ref context, context.Compilation)
                {
                    Cultures = cultures,
                    LogItemCodeGenerator = new LogItemCodeGenerator(ref context, context.Compilation)
                    {
                        Cultures = cultures
                    }
                };


                foreach (ClassDeclarationSyntax classSyntax in receiver.TableCandidates)
                {
                    if (context.CancellationToken.IsCancellationRequested)
                        return;
                    tableCodeGenerator.Process(classSyntax);
                }

                schemaCodeGenerator = new SchemaCodeGenerator(ref context, context.Compilation);

                foreach (ClassDeclarationSyntax classSyntax in receiver.SchemaCandidates)
                {
                    if (context.CancellationToken.IsCancellationRequested)
                        return;
                    schemaCodeGenerator.Process(classSyntax);
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