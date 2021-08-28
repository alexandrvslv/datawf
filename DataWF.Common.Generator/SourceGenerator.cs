using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DataWF.Common.Generator
{
    [Generator]
    public partial class SourceGenerator : ISourceGenerator
    {

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver(ref context));
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // retreive the populated receiver 
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
            {
                try { context.ReportDiagnostic(Diagnostic.Create(SyntaxHelper.DDFailGeneration, Location.None, GetType().Name, "Check Receiver", "Fail", "But why?")); } catch { }
                return;
            }
            try
            {
                BaseGenerator.Compilation = context.Compilation;
                var cultures = new List<string>(new[] { "RU", "EN" });//TODO Pass as argument in destination project

                var invokerGenerator = new InvokerGenerator(ref context);

                // loop over the candidate fields, and keep the ones that are actually annotated
                foreach (var invokerClass in receiver.IvokerCandidates)
                {
                    if (context.CancellationToken.IsCancellationRequested)
                        return;
                    invokerGenerator.Process(invokerClass);
                }

                if (receiver.TableCandidates.Any())
                {
                    var tableCodeGenerator = new TableGenerator(ref context, invokerGenerator)
                    {
                        Cultures = cultures,
                        TableLogGenerator = new TableLogGenerator(ref context, invokerGenerator)
                        {
                            Cultures = cultures
                        }
                    };

                    foreach (var tableClass in receiver.TableCandidates)
                    {
                        if (context.CancellationToken.IsCancellationRequested)
                            return;
                        tableCodeGenerator.Process(tableClass);
                    }
                }

                if (receiver.SchemaCandidates.Any())
                {
                    var schemaCodeGenerator = new SchemaGenerator(ref context, invokerGenerator)
                    {
                        Cultures = cultures,
                        SchemaLogCodeGenerator = new SchemaLogGenerator(ref context, invokerGenerator)
                        {
                            Cultures = cultures
                        }
                    };

                    foreach (var schemaClass in receiver.SchemaCandidates)
                    {
                        if (context.CancellationToken.IsCancellationRequested)
                            return;
                        schemaCodeGenerator.Process(schemaClass);
                    }
                }

                if (receiver.ClientProviderCandidate.Any())
                {
                    var clientGenerator = new ClientProviderGenerator(ref context)
                    {
                        InvokerGenerator = invokerGenerator
                    };

                    foreach (var clientClass in receiver.ClientProviderCandidate)
                    {
                        if (context.CancellationToken.IsCancellationRequested)
                            return;
                        clientGenerator.Process(clientClass);
                    }
                }

                if (receiver.SchemaControllerCandidate.Any())
                {
                    var controllerGenerator = new SchemaControllerGenerator(ref context) { };

                    foreach (var serviceClass in receiver.SchemaControllerCandidate)
                    {
                        if (context.CancellationToken.IsCancellationRequested)
                            return;
                        controllerGenerator.Process(serviceClass);
                    }
                }
            }
            catch (Exception ex)
            {
                try { context.ReportDiagnostic(Diagnostic.Create(SyntaxHelper.DDFailGeneration, Location.None, GetType().Name, ex.GetType().Name, ex.Message, ex.StackTrace)); } catch { }
            }
        }
    }
}