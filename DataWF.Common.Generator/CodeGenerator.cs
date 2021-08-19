using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DataWF.Common.Generator
{
    public abstract class CodeGenerator
    {
        public static readonly DiagnosticDescriptor DDCommonLibrary = new DiagnosticDescriptor("DWFG001",
            "DataWF.Common project or reference not in project references",
            "DataWF.Common project or reference not in project references",
            "DataWF.Generator", DiagnosticSeverity.Warning, true);

        protected StringBuilder source;
        protected INamedTypeSymbol typeSymbol;
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

        public INamedTypeSymbol TypeSymbol
        {
            get => typeSymbol;
            set => typeSymbol = value;
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
            TypeSymbol = GetSymbol(classSyntax);
            return TypeSymbol != null ? Process(TypeSymbol) : false;
        }

        public abstract bool Process(INamedTypeSymbol classSymbol);

        public abstract string Generate();

        protected virtual INamedTypeSymbol GetSymbol()
        {
            return GetSymbol(ClassSyntax);
        }

        protected INamedTypeSymbol GetSymbol(BaseTypeDeclarationSyntax syntax)
        {
            try
            {
                SemanticModel model = Compilation.GetSemanticModel(syntax.SyntaxTree);
                return model.GetDeclaredSymbol(syntax) as INamedTypeSymbol;
            }
            catch
            { return null; }
        }
    }

}