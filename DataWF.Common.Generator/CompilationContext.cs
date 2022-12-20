using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DataWF.Common.Generator
{
    public class CompilationContext
    {
        private Compilation compilation;
        public readonly GeneratorExecutionContext Context;

        public CompilationContext(ref GeneratorExecutionContext context)
        {
            Context = context;
            Compilation = context.Compilation;
        }

        public Compilation Compilation
        {
            get => compilation;
            set
            {
                if (compilation != value)
                {
                    compilation = value;
                    
                    Attributes = new AttributesCache(Compilation);
                    if(compilation is CSharpCompilation csCompilation)
                        Options = csCompilation.SyntaxTrees[0].Options as CSharpParseOptions;
                    else
                        try { Context.ReportDiagnostic(Diagnostic.Create(Helper.DDFailGeneration, Location.None, "CompilationContext", $"Mismatch Versions!!!", compilation.GetType().Assembly.FullName, "")); } catch { }
                }
            }
        }

        public CSharpParseOptions Options { get; set; }

        public AttributesCache Attributes { get; set; }
    }
}