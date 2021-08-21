using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DataWF.Common.Generator
{
    internal abstract class BaseGenerator
    {

        public static Compilation compilation;
        public static Compilation Compilation
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
        public static CSharpParseOptions Options { get; set; }
        public static AttributesCache Attributes { get; set; }

        public readonly GeneratorExecutionContext Context;

        protected StringBuilder source;
        private INamedTypeSymbol typeSymbol;
        private ClassDeclarationSyntax classSyntax;

        public BaseGenerator(ref GeneratorExecutionContext context)
        {
            Context = context;
        }

        public StringBuilder Source
        {
            get => source;
            set => source = value;
        }

        public INamedTypeSymbol TypeSymbol
        {
            get => typeSymbol;
            set => typeSymbol = value;
        }

        public ClassDeclarationSyntax ClassSyntax
        {
            get => classSyntax;
            set => classSyntax = value;
        }

        public bool Process(ClassDeclarationSyntax classSyntax)
        {
            ClassSyntax = classSyntax;
            return Process(GetSymbol());
        }

        public bool Process(INamedTypeSymbol typeSymbol)
        {
            try
            {
                TypeSymbol = typeSymbol;
                if (TypeSymbol == null)
                {
                    return false; //TODO: issue a diagnostic get symbol fail
                }
                if (!TypeSymbol.ContainingSymbol.Equals(TypeSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
                {
                    return false; //TODO: issue a diagnostic that it must be top level
                }

                var result = Process();
                if (result)
                {
                    try { Context.ReportDiagnostic(Diagnostic.Create(SyntaxHelper.DDSuccessGeneration, Location.None, GetType().Name, TypeSymbol.Name)); } catch { }
                }
                return result;
            }
            catch (Exception ex)
            {
                try { Context.ReportDiagnostic(Diagnostic.Create(SyntaxHelper.DDFailGeneration, Location.None, GetType().Name, TypeSymbol.Name, ex.Message, ex.StackTrace)); } catch { }
                return false;
            }
        }

        public abstract bool Process();

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