using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DataWF.Common.Generator
{
    public abstract class CodeGenerator
    {
        protected StringBuilder source;
        protected INamedTypeSymbol classSymbol;
        protected GeneratorExecutionContext context;
        protected ClassDeclarationSyntax classSyntax;
        private Compilation compilation;

        public CodeGenerator(ref GeneratorExecutionContext context, Compilation compilation)
        {
            Context = context;
            Compilation = compilation;
        }

        public readonly GeneratorExecutionContext Context;

        public StringBuilder Source
        {
            get => source;
            set => source = value;
        }

        public ClassDeclarationSyntax ClassSyntax
        {
            get => classSyntax;
            set => classSyntax = value;
        }

        public INamedTypeSymbol ClassSymbol
        {
            get => classSymbol;
            set => classSymbol = value;
        }

        public CSharpParseOptions Options { get; set; }

        public virtual Compilation Compilation
        {
            get => compilation;
            set
            {
                compilation = value;
                Options = (compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            }
        }


        public bool Process(ClassDeclarationSyntax classSyntax)
        {
            ClassSyntax = classSyntax;
            ClassSymbol = Compile();
            return ClassSymbol != null ? Process(ClassSymbol) : false;
        }

        public abstract bool Process(INamedTypeSymbol classSymbol);

        public abstract string Generate();

        protected virtual INamedTypeSymbol Compile()
        {
            return Compile(ClassSyntax);
        }

        protected INamedTypeSymbol Compile(ClassDeclarationSyntax syntax)
        {
            try
            {
                SemanticModel model = Compilation.GetSemanticModel(syntax.SyntaxTree);
                return model.GetDeclaredSymbol(ClassSyntax) as INamedTypeSymbol;
            }
            catch
            { return null; }
        }
    }

}