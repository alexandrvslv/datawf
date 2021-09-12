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
                    Options = (compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
                    Attributes = new AttributesCache(Compilation);
                }
            }
        }

        public CSharpParseOptions Options { get; set; }

        public AttributesCache Attributes { get; set; }
    }
}