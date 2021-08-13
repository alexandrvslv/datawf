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

                var cultures = new List<string>(new[] { "RU", "EN" });//TODO Pass as argument in destination project
                var compilation = context.Compilation;

                tableCodeGenerator = new TableCodeGenerator(ref context, compilation)
                {
                    Cultures = cultures,
                    TableLogCodeGenerator = new TableLogCodeGenerator(ref context, compilation)
                    {
                        Cultures = cultures
                    }
                };

                foreach (ClassDeclarationSyntax classSyntax in receiver.TableCandidates)
                {
                    if (context.CancellationToken.IsCancellationRequested)
                        return;
                    if (tableCodeGenerator.Process(classSyntax))
                    {
                        compilation = tableCodeGenerator.Compilation;
                    }
                }

                schemaCodeGenerator = new SchemaCodeGenerator(ref context, compilation)
                {
                    Cultures = cultures,
                    SchemaLogCodeGenerator = new SchemaLogCodeGenerator(ref context, compilation)
                    {
                        Cultures = cultures
                    }
                };

                foreach (ClassDeclarationSyntax classSyntax in receiver.SchemaCandidates)
                {
                    if (context.CancellationToken.IsCancellationRequested)
                        return;
                    if (schemaCodeGenerator.Process(classSyntax))
                    {
                        compilation = schemaCodeGenerator.Compilation;
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