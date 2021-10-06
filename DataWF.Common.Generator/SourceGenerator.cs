using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DataWF.Common.Generator
{
    [Generator]
    public partial class SourceGenerator : ISourceGenerator
    {
        //https://stackoverflow.com/a/67074009/4682355
        static SourceGenerator()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                var name = new AssemblyName(args.Name);
                Assembly loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().FullName == name.FullName);
                if (loadedAssembly != null)
                {
                    return loadedAssembly;
                }

                string resourceName = $"DataWF.Common.Generator.{name.Name}.dll";
                using Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                if (resourceStream == null)
                {
                    return null;
                }
                var buffer = new byte[resourceStream.Length];
                resourceStream.Read(buffer, 0, buffer.Length);
                return Assembly.Load(buffer);
            };
        }

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
                try { context.ReportDiagnostic(Diagnostic.Create(Helper.DDFailGeneration, Location.None, GetType().Name, "Check Receiver", "Fail", "But why?")); } catch { }
                return;
            }
            try
            {
                var compilationContext = new CompilationContext(ref context);
                var cultures = new List<string>(new[] { "RU", "EN" });//TODO Pass as argument in destination project

                var invokerGenerator = new InvokerGenerator(compilationContext);

                // loop over the candidate fields, and keep the ones that are actually annotated
                foreach (var invokerClass in receiver.IvokerCandidates)
                {
                    if (context.CancellationToken.IsCancellationRequested)
                        return;
                    invokerGenerator.Process(invokerClass);
                }

                if (receiver.TableCandidates.Any())
                {
                    var tableCodeGenerator = new TableGenerator(compilationContext, invokerGenerator)
                    {
                        Cultures = cultures,
                        TableLogGenerator = new TableLogGenerator(compilationContext, invokerGenerator)
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
                    var schemaCodeGenerator = new SchemaGenerator(compilationContext, invokerGenerator)
                    {
                        Cultures = cultures,
                        SchemaLogCodeGenerator = new SchemaLogGenerator(compilationContext, invokerGenerator)
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

                if (receiver.SchemaControllerCandidate.Any())
                {
                    var controllerGenerator = new SchemaControllerGenerator(compilationContext) { };

                    foreach (var serviceClass in receiver.SchemaControllerCandidate)
                    {
                        if (context.CancellationToken.IsCancellationRequested)
                            return;
                        controllerGenerator.Process(serviceClass);
                    }
                }

                if (receiver.ClientProviderCandidate.Any())
                {
                    invokerGenerator.ForceInstance = true;
                    var clientGenerator = new ClientProviderGenerator(compilationContext)
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
            }
            catch (Exception ex)
            {
                try { context.ReportDiagnostic(Diagnostic.Create(Helper.DDFailGeneration, Location.None, GetType().Name, ex.GetType().Name, ex.Message, ex.StackTrace)); } catch { }
            }
        }
    }
}